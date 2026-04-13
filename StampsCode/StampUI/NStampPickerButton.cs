using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.StampUI;

public partial class NStampPickerButton : NButton
{
    private static readonly StringName _v = new("v");
    private static readonly StringName _s = new("s");
    private static readonly StringName _h = new("h");
    
    private const float _unhoverS  = 0.8f;
    private const float _unhoverV  = 0.9f;
    private const float _selectedS = 1.5f;  
    private const float _selectedV = 1.1f;
    
    private TextureRect _image = null!;
    private ShaderMaterial _hsv = null!;
    private NStampPreviewControl _preview = null!;
    private Tween? _tween;
    private Tween? _pulseTween;
    private float _borderAlpha;
    private bool _isSelected;
    private NMapScreen _screen;
    private NStampPickerScreen _stampPickerScreen;
    private HoverTip? _hoverTip;
    private NButton _deleteButton;

    
    static readonly AccessTools.FieldRef<NMapScreen, NMapDrawingInput?> _drawingInputRef =
        AccessTools.FieldRefAccess<NMapScreen, NMapDrawingInput?>("_drawingInput");
    
    public void Initialize(NMapScreen screen, NStampPickerScreen  stampPickerScreen)
    {
        _screen = screen;
        _stampPickerScreen = stampPickerScreen;
    }

    public NMapScreen Screen => _screen;

    public StampDefinition Stamp
    {
        get => _stamp;
        set
        {
            _stamp = value;
            _preview?.SetStamp(value);
        }
    }
    private StampDefinition _stamp = null!;

    [Signal]
    public delegate void StampSelectedEventHandler(NStampPickerButton button);
    [Signal]
    public delegate void StampUnSelectedEventHandler(NStampPickerButton button);
    [Signal]
    public delegate void StampDeletedEventHandler(NStampPickerButton button);

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(128, 128);
        ConnectSignals();
        _hoverTip = new HoverTip(new LocString("static_hover_tips", "STAMP_PICKER.title"), _stamp.Name);

        _hsv = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://shaders/hsv.gdshader"),
        };
        _hsv.SetShaderParameter(_s, _unhoverS);
        _hsv.SetShaderParameter(_v, _unhoverV);
        _hsv.SetShaderParameter(_h, 1.0f);

        Name = _stamp.Name;

        _image = new TextureRect
        {
            Name = "ButtonImage",
            Texture = GD.Load<Texture2D>(Path.Join(MainFile.ModId, "button.png")),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = 0f, OffsetTop = 0f, OffsetRight = 0f, OffsetBottom = 0f,
            Scale = Vector2.One * 0.9f,
            PivotOffset = new Vector2(64, 64),
            Material = _hsv,
        };
        AddChild(_image);

        _preview = new NStampPreviewControl
        {
            Name = "StampPreview",
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = 0f, OffsetTop = 0f, OffsetRight = 0f, OffsetBottom = 0f,
            MouseFilter = MouseFilterEnum.Ignore,
            Scale = Vector2.One * 0.9f,
            PivotOffset = new Vector2(64, 64),
        };
        AddChild(_preview);
        _preview.SetStamp(_stamp);

        var deleteButton = new NDeleteButton();
        deleteButton.Initialize(this, _stampPickerScreen);
        deleteButton.Visible = Config.EnableDeleteButton;
        AddChild(deleteButton);

    }
    
    protected override void OnFocus()
    {
        NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
        nHoverTipSet.GlobalPosition = GlobalPosition + new Vector2(Size.X - nHoverTipSet.Size.X / 2, -2 * nHoverTipSet.Size.Y / 3);
        base.OnFocus();
        if (_isSelected) return;
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_image, "scale", Vector2.One * 0.95f, 0.05);
        _tween.TweenProperty(_preview, "scale", Vector2.One * 0.95f, 0.05);
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
        base.OnUnfocus();
        if (_isSelected) return;
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_image, "scale", Vector2.One * 0.9f, 0.5)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenProperty(_preview, "scale", Vector2.One * 0.9f, 0.5)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), _unhoverS, 0.5)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), _unhoverV, 0.5)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    }

    protected override void OnPress()
    {
        base.OnPress();
        var drawingInput = _drawingInputRef(Screen);
        _isSelected = !_isSelected;

        _tween?.Kill();
        _tween = CreateTween().SetParallel();

        float targetS = _isSelected ? _selectedS : _unhoverS;
        float targetV = _isSelected ? _selectedV : _unhoverV;
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), targetS, 0.15)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), targetV, 0.15)
              .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenProperty(_image, "scale",
            Vector2.One * (_isSelected ? 1.0f : 0.9f), 0.15)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenProperty(_preview, "scale",
            Vector2.One * (_isSelected ? 1.0f : 0.9f), 0.15)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);

        drawingInput?.StopDrawing();

        if (_isSelected)
        {
            StampRegistry.SetActive(Stamp);
            EmitSignal(SignalName.StampSelected, this);
        }
        else
        {
            StampRegistry.SetActive(null);
            EmitSignal(SignalName.StampUnSelected, this);
        }
    }

    public void Deselect()
    {
        if (!_isSelected) return;
        _isSelected = false;
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), _unhoverS, 0.3);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), _unhoverV, 0.3);
        _tween.TweenProperty(_image, "scale", Vector2.One * 0.9f, 0.3);
        _tween.TweenProperty(_preview, "scale", Vector2.One * 0.9f, 0.3);
    }
    
    public void DisplayAsSelected()
    {
        _isSelected = true;
        _tween?.Kill();
        _tween = null;

        _hsv.SetShaderParameter(_s, _selectedS);
        _hsv.SetShaderParameter(_v, _selectedV);
        _image.Scale = Vector2.One;
        _preview.Scale = Vector2.One;

    }

    public bool IsSelected => _isSelected;
    
    private void SetS(float value) => _hsv.SetShaderParameter(_s, value);
    private void SetV(float value) => _hsv.SetShaderParameter(_v, value);
}
