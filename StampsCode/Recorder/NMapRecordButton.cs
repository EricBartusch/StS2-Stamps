using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Stamps.StampsCode.Recorder;

public partial class NMapRecordButton : NButton
{
    private static readonly StringName _imagePath = Path.Join(MainFile.ModId, "record_button.png");
    private static readonly StringName _glowImagePath = Path.Join(MainFile.ModId, "record_button_glow.png");


    private static readonly Color _recordingColor = new Color("FFA500");
    private static readonly Color _inactiveColor  = new Color("FFFFFF80");

    private TextureRect _icon = null!;
    private Tween? _tween;
    
    private Control _drawingToolHolder;
    private HoverTip _hoverTip;
    private HoverTip _hoverTipActive;
    private NHoverTipSet nHoverTipSet;

    private bool _stampActive;
    private bool _isRecording;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(60, 60);
        ConnectSignals();

        _icon = new TextureRect
        {
            Name               = "RecordIcon",
            Texture            = GD.Load<Texture2D>(_imagePath),
            LayoutMode         = 1,
            ExpandMode         = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode        = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical  = SizeFlags.ExpandFill,
            SelfModulate       = _inactiveColor,
            AnchorLeft = 0, AnchorTop = 0, AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 0, OffsetTop = 0, OffsetRight = 0, OffsetBottom = 0,
            Scale       = new Vector2(1.1f, 1.1f),
            PivotOffset = new Vector2(30, 30),
            FocusMode   = FocusModeEnum.All,
            Material    = GD.Load<Material>("res://themes/canvas_item_material_additive_shared.tres"),
        };
        AddChild(_icon);
        
        _drawingToolHolder = (Control)GetParent();
        var title = new LocString("static_hover_tips", "RECORD_BUTTON.title");
        _hoverTip       = new HoverTip(title, new LocString("static_hover_tips", "RECORD_BUTTON.description"));
        _hoverTipActive = new HoverTip(title, new LocString("static_hover_tips", "RECORD_BUTTON.description.active"));
        
        StampRecorder.StampRecorded += OnStampRecorded;
    }

    public override void _ExitTree()
    {
        StampRecorder.StampRecorded -= OnStampRecorded;
    }

    private void OnStampRecorded() => SetRecording(false);

    public void SetRecording(bool recording)
    {
        _isRecording = recording;
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_icon, "self_modulate",
            recording ? _recordingColor : _inactiveColor, 0.1);
        _icon.Texture = PreloadManager.Cache.GetTexture2D(recording ? _glowImagePath : _imagePath);
        _icon.SelfModulate = recording ? _recordingColor : _inactiveColor;
        if(IsFocused)
        {
            var tip = _isRecording ? _hoverTipActive : _hoverTip;
            NHoverTipSet.Remove(_drawingToolHolder);
            nHoverTipSet = NHoverTipSet.CreateAndShow(_drawingToolHolder, tip);
            nHoverTipSet.GlobalPosition = _drawingToolHolder.GlobalPosition + new Vector2(10f, -132f);
        }
    }
    
    protected override void OnFocus()
    {
        base.OnFocus();
        var tip = _isRecording ? _hoverTipActive : _hoverTip;
        nHoverTipSet = NHoverTipSet.CreateAndShow(_drawingToolHolder, tip);
        nHoverTipSet.GlobalPosition = _drawingToolHolder.GlobalPosition + new Vector2(10f, -132f);
        if (!_isRecording)
        {
            _icon.Texture = PreloadManager.Cache.GetTexture2D(_glowImagePath);
            _icon.SelfModulate = _recordingColor;
        }
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_icon, "scale", Vector2.One * 1.2f, 0.05f);
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        NHoverTipSet.Remove(_drawingToolHolder);
        if (!_isRecording)
        {
            _icon.Texture = PreloadManager.Cache.GetTexture2D(_imagePath);
            _icon.SelfModulate = _inactiveColor;
        }
        _tween?.Kill();
        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_icon, "scale", Vector2.One * 1.1f, 0.05f);
    }
}
