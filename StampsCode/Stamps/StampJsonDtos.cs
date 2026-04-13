using System.Text.Json.Serialization;

namespace Stamps.StampsCode.Stamps;

public class StampDto
{
    [JsonPropertyName("strokes")]
    public List<StrokeDto> Strokes { get; set; } = new();
}

public class StrokeDto
{
    [JsonPropertyName("points")]
    public PointDto[]? Points { get; set; }

    [JsonPropertyName("erase")]
    public bool Erase { get; set; }
}

public class PointDto
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }
}
