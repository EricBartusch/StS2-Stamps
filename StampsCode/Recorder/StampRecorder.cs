using System.Text.Json;
using Godot;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.Recorder;

public static class StampRecorder
{ 
    public static event Action? StampRecorded;

    public static bool IsRecording { get; private set; }

    public static Vector2 PendingClickPosition { get; private set; }

    private static bool _suppressNextStroke;

    public static void StartRecording() => IsRecording = true;

    public static void CancelRecording() => IsRecording = false;

    public static void DiscardRecording()
    {
        IsRecording = false;
        _pendingStrokes.Clear();
    }

    public static void SuppressNextStroke() => _suppressNextStroke = true;

    public static void RecordClickPosition(Vector2 position) => PendingClickPosition = position;

    // Per-stroke: raw game-space offsets + the click position they were relative to + erase flag.
    private static readonly List<(Vector2 ClickPos, PointDto[] RawOffsets, bool Erase)> _pendingStrokes = new();
    
    public static void OnLineCompleted(Line2D line, Vector2 clickPosition, bool erase = false)
    {
        if (_suppressNextStroke)
        {
            _suppressNextStroke = false;
            return;
        }
        if (!IsRecording) return;

        int count = line.GetPointCount();
        if (count < 2) return;

        var pts = new PointDto[count];
        for (int i = 0; i < count; i++)
        {
            // Reverse the BeginLine formula: offset = viewportPt * 2 - clickPos
            var offset = line.GetPointPosition(i) * 2f - clickPosition;
            pts[i] = new PointDto { X = offset.X, Y = offset.Y };
        }

        _pendingStrokes.Add((clickPosition, pts, erase));
    }

    public static void StampCompleted(string name)
    {
        IsRecording = false;

        if (_pendingStrokes.Count == 0)
        {
            _pendingStrokes.Clear();
            return;
        }

        var strokes = BuildNormalizedStrokes();
        var stamp = new StampDto();
        foreach (var s in strokes)
            stamp.Strokes.Add(s);

        string folder = Config.CustomStampDir;
        Directory.CreateDirectory(folder);
        string safeName = GetUniqueName(folder, name);
        string json = JsonSerializer.Serialize(stamp, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(folder, safeName + ".json"), json);

        CustomStampLoader.LoadStamps(folder);

        StampRecorded?.Invoke();
        _pendingStrokes.Clear();
    }
    
    public static StampDefinition? BuildPreview()
    {
        if (_pendingStrokes.Count == 0) return null;

        var strokes = BuildNormalizedStrokes();
        var stampStrokes = strokes.Select(s =>
        {
            var points = s.Points!.Select(p => new Vector2(
                (p.X - 0.5f) * CustomStampLoader.NormalizedScale,
                (p.Y - 0.5f) * CustomStampLoader.NormalizedScale)).ToList();
            return new StampStroke(points, s.Erase);
        }).ToList();
        return new StampDefinition("Preview", stampStrokes);
    }

    private static List<StrokeDto> BuildNormalizedStrokes()
    {
        float sumX = 0, sumY = 0;
        foreach (var (cp, _, _) in _pendingStrokes) { sumX += cp.X; sumY += cp.Y; }
        var qRef = new Vector2(sumX / _pendingStrokes.Count, sumY / _pendingStrokes.Count);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        var corrected = new List<(Vector2 Corr, PointDto[] Pts, bool Erase)>(_pendingStrokes.Count);
        foreach (var (clickPos, pts, erase) in _pendingStrokes)
        {
            var corr = clickPos - qRef;
            corrected.Add((corr, pts, erase));
            foreach (var p in pts)
            {
                float x = p.X + corr.X;
                float y = p.Y + corr.Y;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
        }

        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;

        var result = new List<StrokeDto>();
        foreach (var (corr, pts, erase) in corrected)
        {
            var normalized = new PointDto[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                normalized[i] = new PointDto
                {
                    X = (pts[i].X + corr.X - cx) / CustomStampLoader.NormalizedScale + 0.5f,
                    Y = (pts[i].Y + corr.Y - cy) / CustomStampLoader.NormalizedScale + 0.5f,
                };
            }
            result.Add(new StrokeDto { Points = normalized, Erase = erase });
        }
        return result;
    }

    private static string GetUniqueName(string folder, string baseName)
    {
        if (!File.Exists(Path.Combine(folder, baseName + ".json")))
            return baseName;
        for (int i = 2; ; i++)
        {
            string candidate = $"{baseName}_{i}";
            if (!File.Exists(Path.Combine(folder, candidate + ".json")))
                return candidate;
        }
    }
}