using Godot;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace Stamps.StampsCode.Stamps;

public class StampDefinition
{
    public string Name { get; set; }
    public List<StampStroke> Strokes { get; set; }
    private ulong _playerId { get; set; }
    
    public StampDefinition(string name, List<StampStroke> strokes, ulong playerId = 0)
    {
        Name    = name;
        Strokes = strokes;
        _playerId = playerId;
    }

    public bool OnlyEraseStrokes()
    {
        foreach (var stroke in Strokes)
        {
            if (!stroke.Erase)
                return false;
        }
        return true;
    }
    
    public string GetPlayerName()
    {
        return _playerId > 0 ? PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, _playerId) : "";
    }
    
    public StampDto ToDto()
    {
        return new StampDto
        {
            Strokes = Strokes.Select(s => new StrokeDto
            {
                Erase = s.Erase,
                Points = s.Points.Select(p => new PointDto
                {
                    X = p.X / CustomStampLoader.NormalizedScale + 0.5f,
                    Y = p.Y / CustomStampLoader.NormalizedScale + 0.5f,
                }).ToArray()
            }).ToList()
        };
    }
}

public class StampStroke
{
    public List<Vector2> Points { get; set; }
    public bool Erase { get; set; }

    public StampStroke(List<Vector2> points, bool erase = false)
    {
        Points = points;
        Erase  = erase;
    }
}
