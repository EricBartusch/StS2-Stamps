# StS2-Stamps

Adds stamps to the map screen in Slay the Spire 2. Click the stamp button on the map toolbar to open the stamp picker, select a stamp, then click anywhere on the map to place it.

## Features

- **Built-in stamps** - Arrow, Circle, Cross, Exclamation, Heart, Question, Star
- **Custom stamps** - Record your own stamps using the record button on the map toolbar
- **Multiplayer** - Works in multiplayer, but each player needs to have the mod installed

## Custom Stamp Folder

On first launch, the default stamps are written as JSON files to:

```
%APPDATA%\Godot\app_userdata\Slay the Spire 2\mod_configs\CustomStamps\
```

You can add, edit, or delete `.json` files in this folder and click **Reload Stamps** in the mod config to pick up the changes without restarting.

## JSON Stamp Format

Each stamp is a `.json` file. Points use a **0–1 normalized coordinate space** where `(0.5, 0.5)` is the center of the stamp.

```json
{
  "strokes": [
    {
      "points": [
        { "x": 0.5, "y": 0.25 },
        { "x": 0.75, "y": 0.75 },
        { "x": 0.25, "y": 0.75 },
        { "x": 0.5, "y": 0.25 }
      ],
      "erase": false
    }
  ]
}
```

- A stamp can have multiple strokes drawn as separate line segments.
- Set `"erase": true` on a stroke to use it as an eraser stroke (subtracts from previous strokes).
- The filename (without `.json`) is used as the stamp's display name.

## Contributing New Default Stamps

Default stamps are defined in code in [StampsCode/Stamps/DefaultStamps.cs](StampsCode/Stamps/DefaultStamps.cs). Points use an internal coordinate space where the center is `(0, 0)` and the scale is `±50` units (i.e. multiply your normalized 0–1 values by 100 and subtract 50).

To add a new default stamp:

1. **Design the stamp.** The easiest way is to use the in-game recorder, then find the saved `.json` in your custom stamps folder.

2. **Convert the points.** For each point in the JSON, apply:
   ```
   x_code = (x_json - 0.5) * 100
   y_code = (y_json - 0.5) * 100
   ```

3. **Add a `static readonly` field** to `DefaultStamps.cs`:
   ```csharp
   public static readonly StampDefinition MyStamp = new("MyStamp",
   [
       new StampStroke(
       [
           new Vector2(-25f, -25f),
           new Vector2( 25f,  25f),
           // ...
       ]),
   ]);
   ```

4. **Add it to the `All` property:**
   ```csharp
   public static IEnumerable<StampDefinition> All =>
   [
       Arrow,
       Circle,
       // ...
       MyStamp,
   ];
   ```

5. Test it out in game to see if it looks as expected.

6. Open a pull request. Include a screenshot or description of what the stamp looks like.

## Credits

Thank you Blizz for the stamp icon!

## TODO
- Option to animate stamps being drawn instead of instant
