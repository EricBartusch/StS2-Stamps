using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Stamps.StampsCode.Patches;
using Stamps.StampsCode.Recorder;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.StampUI;

public partial class NStampPickerScreen : Panel
{
    private const float ButtonSize    = 128f;
    private static readonly int Columns = (int)Config.ButtonColumns;
    private const float Padding       = 12f;
    private const float HeaderHeight  = 40f;
    private const float ToolbarHeight = 108f;
    private const float GapAbove      = 8f;
    private const float LeftAnchorPx  = 56f;

    private GridContainer _grid = null!;
    
    private NMapScreen _screen;
    private NMapStampButton? _button;
    private bool _stampActive;

    private static readonly Shader BlurShader = GD.Load<Shader>(
        "res://shaders/dark_blur.gdshader"
    );
    
    public void Initialize(NMapScreen screen, NMapStampButton button)
    {
        _screen = screen;
        _button = button;
    }

    public NMapScreen Screen => _screen;

    static readonly AccessTools.FieldRef<NMapScreen, NMapDrawingInput> _drawingInputRef =
        AccessTools.FieldRefAccess<NMapScreen, NMapDrawingInput>("_drawingInput");
    
    static readonly Action<NMapScreen> updateDrawingButtonStates =
        AccessTools.MethodDelegate<Action<NMapScreen>>(
            AccessTools.Method(typeof(NMapScreen), "UpdateDrawingButtonStates")
        );

    public override void _Ready()
    {
        var style = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,

        };
        AddThemeStyleboxOverride("panel", style);
        
        AnchorLeft = AnchorRight = 0f;
        AnchorTop  = AnchorBottom = 1f;
        MouseFilter = MouseFilterEnum.Stop;

        var britishBroadcastingCompany = new BackBufferCopy
        {
            CopyMode   = BackBufferCopy.CopyModeEnum.Viewport,
        };
        AddChild(britishBroadcastingCompany);

        var blurMat = new ShaderMaterial { Shader = BlurShader };
        blurMat.SetShaderParameter("lod", 3.0f);
        blurMat.SetShaderParameter("mix_percentage", 0.5f);
        var blurRect = new ColorRect
        {
            LayoutMode  = 1,
            AnchorLeft  = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft  = 0f, OffsetTop  = 0f, OffsetRight  = 0f, OffsetBottom  = 0f,
            Color       = Colors.White,
            Material    = blurMat,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(blurRect);

        var title = new Label
        {
            Text = new LocString("static_hover_tips", "STAMP_PICKER.label").GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 0f,
            OffsetLeft = Padding, OffsetTop = Padding, OffsetRight = -Padding, OffsetBottom = HeaderHeight,
            LabelSettings = new LabelSettings {
                FontSize = 24,
                Font = PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_glyph_space_one.tres"),
                FontColor = Colors.White,
                ShadowSize = 2,
                ShadowColor = new Color(0f, 0f, 0f, 0.8f)
            }
            
        };
        AddChild(title);

        _grid = new GridContainer
        {
            Columns = Columns,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = Padding, OffsetTop = HeaderHeight, OffsetRight = -Padding, OffsetBottom = -Padding,
        };
        AddChild(_grid);

        Visible = false;
        Rebuild();

        StampRecorder.StampRecorded += Rebuild;
    }

    public override void _ExitTree()
    {
        StampRecorder.StampRecorded -= Rebuild;
    }

    public void Rebuild()
    {
        CustomStampLoader.LoadStamps(Config.CustomStampDir);

        foreach (Node child in _grid.GetChildren())
            child.QueueFree();

        var allStamps = StampRegistry.All;
        var buttons = new List<NStampPickerButton>(allStamps.Count);

        foreach (var stamp in allStamps)
        {
            var btn = new NStampPickerButton { Stamp = stamp };
            btn.Initialize(Screen, this);
            btn.Connect(NStampPickerButton.SignalName.StampSelected,
                Callable.From<NStampPickerButton>(pressed => OnStampSelected(pressed, buttons)));
            btn.Connect(NStampPickerButton.SignalName.StampUnSelected,
                Callable.From<NStampPickerButton>(_ => OnStampUnSelected()));
            btn.Connect(NStampPickerButton.SignalName.StampDeleted,
                Callable.From<NStampPickerButton>(_ => Rebuild()));
            _grid.AddChild(btn);
            buttons.Add(btn);
            if (StampRegistry.ActiveStamp?.Name == stamp.Name)
            {
                btn.DisplayAsSelected();
            }
        }
        ApplySize(Math.Max(allStamps.Count, 1));
    }

    private void OnStampSelected(NStampPickerButton pressed, List<NStampPickerButton> siblings)
    {
        foreach (var other in siblings)
            if (other != pressed) other.Deselect();

        var newInput = NMapDrawingInput.Create(Screen.Drawings, NMapDrawingsPatches.Stamp);
        newInput.Connect(NMapDrawingInput.SignalName.Finished, Callable.From(OnStampDrawingFinished));

        _stampActive = true;
        _drawingInputRef(Screen) = newInput;
        Screen.AddChildSafely(newInput);
        updateDrawingButtonStates(Screen);
    }

    private void OnStampDrawingFinished()
    {
        _drawingInputRef(Screen) = null;
        updateDrawingButtonStates(Screen);
    }

    private void OnStampUnSelected()
    {
        _stampActive = false;
        _drawingInputRef(Screen)?.StopDrawing();
        _drawingInputRef(Screen) = null;
        updateDrawingButtonStates(Screen);
    }

    private void ApplySize(int stampCount)
    {
        int rows = (int)Math.Ceiling(stampCount / (float)Columns);
        float panelWidth  = Columns * ButtonSize + Padding * 2;
        float panelHeight = HeaderHeight + rows * ButtonSize + Padding * 2;

        OffsetLeft   = LeftAnchorPx;
        OffsetRight  = LeftAnchorPx + panelWidth;
        OffsetTop    = -(ToolbarHeight + panelHeight + GapAbove);
        OffsetBottom = -(ToolbarHeight + GapAbove);
    }

    public static bool IsAnyOpen { get; private set; }

    public void Open()
    {
        Rebuild();
        Visible = true;
        IsAnyOpen = true;
        _button?.SetPickerOpen(true);
    }

    public void Close()
    {
        Visible = false;
        IsAnyOpen = false;
        _button?.SetPickerOpen(false);
    }

    public void Toggle()
    {
        if (Visible) Close(); else Open();
    }

    // Close on any click outside the panel (and outside the button that opens it)
    // Not marked as handled so the stamp will actually stamp on the map if the screen is open
    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        if (@event is InputEventMouseButton { Pressed: true } mb)
        {
            var pos = mb.GlobalPosition;
            if (!GetGlobalRect().HasPoint(pos) && !(_button?.GetGlobalRect().HasPoint(pos) ?? false))
                Close();
        }
    }
}
