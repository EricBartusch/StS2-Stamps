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
