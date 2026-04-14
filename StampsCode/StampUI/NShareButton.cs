using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using Stamps.StampsCode.Networking;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.StampUI;

public partial class NShareButton : NButton
{
    private NStampPickerButton _stampPickerButton;
    private StampDisplayTile _stampDisplayTile;
    private NStampPickerScreen _stampPickerScreen;
    private ConfigStampPreviewControl _configStampPreviewControl;
    private ShaderMaterial _hsv = null!;
    private Tween? _tween;
    private TextureRect _buttonTex = null!;
    private Label _deleteLabel = null!;
    private static readonly StringName _v = new("v");
    private static readonly StringName _s = new("s");
    private static readonly StringName _h = new("h");
    
    private const float _unhoverS  = 0.8f;
    private const float _unhoverV  = 0.9f;
    private const float _hoverS    = 1.2f;
    private const float _hoverV    = 1.3f;
    private const float _pressS    = 0.6f;
    private const float _pressV    = 0.6f;


    public override void _Ready()
    {
        ConnectSignals();
        Name = "ShareButton";
        CustomMinimumSize = new Vector2(24, 24);
        AnchorBottom = 0.0f;
        AnchorLeft = 0.0f;
        AnchorRight = 0.0f;
        AnchorTop = 0.0f;
        OffsetBottom = 28;
        OffsetLeft = 2;
        OffsetTop = 4;
        OffsetRight = 26;
        
        _hsv = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://shaders/hsv.gdshader"),
        };
        _hsv.SetShaderParameter(_s, _unhoverS);
        _hsv.SetShaderParameter(_v, _unhoverV);
        _hsv.SetShaderParameter(_h, 1.0f);

        _buttonTex = new TextureRect()
        {
            Name = "ShareButtonImage",
            Texture = GD.Load<Texture2D>(Path.Join(MainFile.ModId, "share_button.png")),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            LayoutMode = 1,
            AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = 0f, OffsetTop = 0f, OffsetRight = 0f, OffsetBottom = 0f,
            Scale = Vector2.One * 0.9f,
            PivotOffset = new Vector2(12, 12),
            Material = _hsv,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_buttonTex);
    }

    public void Initialize(NStampPickerButton stampPickerButton, NStampPickerScreen stampPickerScreen)
    {
        _stampPickerButton = stampPickerButton;
        _stampPickerScreen = stampPickerScreen;
    }
    
    public void Initialize(StampDisplayTile stampDisplayTile, ConfigStampPreviewControl configStampPreviewControl)
    {
        _stampDisplayTile = stampDisplayTile;
        _configStampPreviewControl = configStampPreviewControl;
    }
    
    protected override void OnFocus()
    {
        base.OnFocus();
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_buttonTex, "scale", Vector2.One * 1.0f, 0.05);
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), _hoverS, 0.05);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), _hoverV, 0.05);
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_buttonTex, "scale", Vector2.One * 0.9f, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), _unhoverS, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), _unhoverV, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    
    }

    protected override void OnPress()
    {
        base.OnPress();
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenMethod(Callable.From<float>(SetS), (float)_hsv.GetShaderParameter(_s), _pressS, 0.05);
        _tween.TweenMethod(Callable.From<float>(SetV), (float)_hsv.GetShaderParameter(_v), _pressV, 0.05);
    }

    protected override void OnRelease()
    {
        if (_stampPickerButton is not null)
        {
            StampDefinition stamp = StampRegistry.GetStampByName(_stampPickerButton.Stamp.Name);
            if (stamp is not null)
            {
                MultiplayerManager.BroadcastStamp(stamp);
            }
        }
    }
    
    private void SetS(float value) => _hsv.SetShaderParameter(_s, value);
    private void SetV(float value) => _hsv.SetShaderParameter(_v, value);
}