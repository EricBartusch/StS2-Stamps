using Godot;

namespace Stamps.StampsCode.Stamps;

public class StampDefinition
{
    public string Name { get; set; }
    public List<StampStroke> Strokes { get; set; }
    
    public StampDefinition(string name, List<StampStroke> strokes)
    {
        Name    = name;
        Strokes = strokes;
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
