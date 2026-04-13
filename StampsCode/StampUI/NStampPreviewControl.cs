using Godot;
using Stamps.StampsCode.Patches;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.StampUI;

// This one is basically all vibe coded
public partial class NStampPreviewControl : Control
{
    private static readonly Color DefaultLineColor = NMapDrawingsPatches.PlayerColor;
    private const float GameDrawWidth  = 4f; 
    private const float GameEraseWidth = 20f; // normally the erase width is 12f, but it needs to be way bigger here for whatever reason
    private const float Padding        = 12f;

    public Color? LineColor { get; set; }

    private CanvasGroup _group = null!;
    private StampDefinition? _stamp;

    public override void _Ready()
    {

        _group = new CanvasGroup { Name = "StrokeGroup", Visible = false };
        AddChild(_group);
        CallDeferred(nameof(Rebuild));
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            Rebuild();
    }

    public void SetStamp(StampDefinition stamp)
    {
        _stamp = stamp;
        if (IsInsideTree())
            CallDeferred(nameof(Rebuild));
    }

    private void Rebuild()
    {
        if (_stamp == null || _group == null) return;

        var size = Size;
        if (size == Vector2.Zero) return;

        foreach (var child in _group.GetChildren())
            child.Free();

        var (normalized, scale) = Normalize(_stamp.Strokes);
        foreach (var (points, erase) in normalized)
        {
            if (points.Count < 2) continue;

            var line = new Line2D
            {
                Width        = (erase ? GameEraseWidth : GameDrawWidth) * scale,
                DefaultColor = erase ? Colors.White : (LineColor ?? DefaultLineColor),
                Antialiased  = true,
            };

            if (erase)
            {
                line.Material = new CanvasItemMaterial
                {
                    BlendMode = CanvasItemMaterial.BlendModeEnum.Sub,
                };
            }

            foreach (var p in points)
                line.AddPoint(p);

            _group.AddChild(line);
        }

        _group.Visible = true;
    }
    
    private (List<(List<Vector2> Points, bool Erase)> Strokes, float Scale) Normalize(List<StampStroke> strokes)
    {
        if (strokes.Count == 0) return ([], 1f);
        
        // Trying to find the total size of the stamp
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var stroke in strokes)
        {
            foreach (var p in stroke.Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
        }

        float stampW = maxX - minX;
        float stampH = maxY - minY;
        if (stampW == 0) stampW = 1;
        if (stampH == 0) stampH = 1;

        Vector2 size = Size;
        float availW = size.X - Padding * 2;
        float availH = size.Y - Padding * 2;

        float scale = Mathf.Min(availW / stampW, availH / stampH) * 0.7f;

        float offsetX = Padding + (availW - stampW * scale) * 0.5f - minX * scale;
        float offsetY = Padding + (availH - stampH * scale) * 0.5f - minY * scale;

        var result = new List<(List<Vector2>, bool)>();
        foreach (var stroke in strokes)
        {
            var newStroke = new List<Vector2>(stroke.Points.Count);

            foreach (var p in stroke.Points)
            {
                newStroke.Add(new Vector2(
                    p.X * scale + offsetX,
                    p.Y * scale + offsetY
                ));
            }

            result.Add((newStroke, stroke.Erase));
        }
        return (result, scale);

    }
}
