using System.Text.Json;
using Godot;

namespace Stamps.StampsCode.Stamps;

public class CustomStampLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    
    public const float NormalizedScale = 100f;

    public static void LoadStamps(string folder)
    {
        StampRegistry.NukeStamps();
        if (!Directory.Exists(folder)) return;

        foreach (string file in Directory.GetFiles(folder, "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string json = File.ReadAllText(file);

            StampDto? dto = JsonSerializer.Deserialize<StampDto>(json, _options);
            if (dto?.Strokes is null || dto.Strokes.Count == 0) continue;

            List<StampStroke> strokes = new();

            foreach (var stroke in dto.Strokes)
            {
                if (stroke.Points == null) continue;

                var converted = Array.ConvertAll(stroke.Points, p =>
                    new Vector2(
                        (p.X - 0.5f) * NormalizedScale,
                        (p.Y - 0.5f) * NormalizedScale
                    )
                );

                strokes.Add(new StampStroke(converted.ToList(), stroke.Erase));
            }

            StampRegistry.Register(new StampDefinition(name, strokes));
        }
    }
}
