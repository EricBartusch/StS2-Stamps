using Godot;
using Stamps.StampsCode.Stamps;
using Stamps.StampsCode.StampUI;

namespace Stamps.StampsCode;

public partial class ConfigStampPreviewControl : Panel
{
    private const float ButtonSize   = 128f;
    private static int Columns = (int)Config.ButtonColumns;
    private const float Padding      = 12f;
    private const float HeaderHeight = 40f;
    private GridContainer _grid;

    public override void _Ready()
    {
        Config.EnableDeleteButtonChanged += Reload;
        Config.ButtonColumnsChanged += Reload;

        var style = new StyleBoxFlat
        {
            BgColor                 = new Color(0f, 0f, 0f, 0.0f),
            CornerRadiusTopLeft     = 8,
            CornerRadiusTopRight    = 8,
            CornerRadiusBottomLeft  = 8,
            CornerRadiusBottomRight = 8,
        };
        AddThemeStyleboxOverride("panel", style);

        MouseFilter          = MouseFilterEnum.Stop;
        SizeFlagsVertical    = SizeFlags.ShrinkBegin;
        SizeFlagsHorizontal  = SizeFlags.ShrinkBegin;

        _grid = new GridContainer
        {
            Columns    = Columns,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = Padding, OffsetTop = HeaderHeight, OffsetRight = -Padding, OffsetBottom = -Padding,
        };
        AddChild(_grid);

        var allStamps = StampRegistry.All;
        foreach (var stamp in allStamps)
        {
            var tile = new StampDisplayTile { Stamp = stamp };
            tile.Initialize(this);
            _grid.AddChild(tile);
        }

        int rows = (int)Math.Ceiling(Math.Max(allStamps.Count, 1) / (float)Columns);
        float panelWidth  = Columns * ButtonSize + Padding * 2;
        float panelHeight = HeaderHeight + rows * ButtonSize + Padding * 2;
        CustomMinimumSize = new Vector2(panelWidth, panelHeight);
    }

    public void Reload()
    {
        Columns = (int)Config.ButtonColumns;
        RemoveChild(_grid);
        
        _grid = new GridContainer
        {
            Columns    = Columns,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = Padding, OffsetTop = HeaderHeight, OffsetRight = -Padding, OffsetBottom = -Padding,
        };
        AddChild(_grid);

        var allStamps = StampRegistry.All;
        foreach (var stamp in allStamps)
        {
            var tile = new StampDisplayTile { Stamp = stamp };
            tile.Initialize(this);
            _grid.AddChild(tile);
        }

        int rows = (int)Math.Ceiling(Math.Max(allStamps.Count, 1) / (float)Columns);
        float panelWidth  = Columns * ButtonSize + Padding * 2;
        float panelHeight = HeaderHeight + rows * ButtonSize + Padding * 2;
        CustomMinimumSize = new Vector2(panelWidth, panelHeight);
    }

    public override void _ExitTree()
    {
        Config.EnableDeleteButtonChanged -= Reload;
        Config.ButtonColumnsChanged -= Reload;
    }
}