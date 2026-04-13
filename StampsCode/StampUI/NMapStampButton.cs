using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Stamps.StampsCode.StampUI;

public partial class NMapStampButton : NButton
{

	private static readonly StringName _imagePath = Path.Join(MainFile.ModId, "stamp.png");

	private static readonly StringName _glowImagePath = Path.Join(MainFile.ModId, "stamp_glow.png");

	private Control _drawingToolHolder;

	private TextureRect _icon;

	private HoverTip _hoverTip;
	private NHoverTipSet nHoverTipSet;


	private Tween? _tween;

	private bool _isActive;
	private bool _pickerOpen;

	private bool ShouldGlow => _isActive || _pickerOpen;

	private static readonly Color _activeColor = new Color("7cfc00");

	private static readonly Color _inactiveColor = new Color("FFFFFF80");

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(60, 60);
		ConnectSignals();
		_icon = new TextureRect
		{
			Name = "StampIcon",
			Texture = GD.Load<Texture2D>(_imagePath),
			LayoutMode = 1,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SelfModulate = new Color("FFFFFF80"),
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetLeft = 0,
			OffsetTop = 0,
			OffsetRight = 0,
			OffsetBottom = 0,
			Scale = new Vector2(1.1f, 1.1f),
			PivotOffset = new Vector2(30, 30),
			FocusMode = FocusModeEnum.All,
			Material = GD.Load<Material>("res://themes/canvas_item_material_additive_shared.tres")

		};
		AddChild(_icon);
		LocString description = new LocString("static_hover_tips", "STAMP_BUTTON.description");
		LocString title = new LocString("static_hover_tips", "STAMP_BUTTON.title");
		_hoverTip = new HoverTip(title, description);

		_drawingToolHolder = (Control)GetParent();
	}

	public void SetActive(bool active)
	{
		_isActive = active;
		ApplyVisual();
	}

	public void SetPickerOpen(bool open)
	{
		_pickerOpen = open;
		if (open)
			nHoverTipSet.Hide();
		else if (IsFocused)
			nHoverTipSet.Show();

		ApplyVisual();
	}
	
	private void ApplyVisual()
	{
		_icon.Texture = PreloadManager.Cache.GetTexture2D(ShouldGlow ? _glowImagePath : _imagePath);
		_icon.SelfModulate = ShouldGlow ? _activeColor : _inactiveColor;
		
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One * 1.2f, 0.05);
		_tween.TweenProperty(_icon, "self_modulate", _activeColor, 0.05);
		nHoverTipSet = NHoverTipSet.CreateAndShow(_drawingToolHolder, _hoverTip);
		nHoverTipSet.GlobalPosition = _drawingToolHolder.GlobalPosition + new Vector2(10f, -132f);
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		_tween?.Kill();
		_tween = CreateTween().SetParallel();
		_tween.TweenProperty(_icon, "scale", Vector2.One * 1.1f, 0.05);
		_tween.TweenProperty(_icon, "self_modulate", ShouldGlow ? _activeColor : _inactiveColor, 0.05);
		ApplyVisual();
		NHoverTipSet.Remove(_drawingToolHolder);
	}
}
