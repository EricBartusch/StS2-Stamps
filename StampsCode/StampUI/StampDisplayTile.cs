using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.StampUI;

public partial class StampDisplayTile : NClickableControl
{
    private static readonly Color BgColor     = new(0.15f, 0.15f, 0.2f, 0.9f);
    private static readonly Color BorderColor = new(0.4f, 0.4f, 0.5f, 0.8f);
    private static readonly Color LineColor   = new(0.8f, 0.8f, 1f);
    private const float DrawWidth    = 3f;
    private const float DrawPadding  = 12f;

    private StampDefinition _stamp = null!;
    private HoverTip? _hoverTip;
    private ConfigStampPreviewControl _configStampPreviewControl;

    public StampDefinition Stamp
    {
        get => _stamp;
        set
        {
            _stamp = value;
            QueueRedraw();
        }
    }

    public void Initialize(ConfigStampPreviewControl configStampPreviewControl)
    {
        _configStampPreviewControl = configStampPreviewControl;
    }
    
    public override void _Ready()
    {
        ConnectSignals();
        _hoverTip = new HoverTip(new LocString("settings_ui", "STAMP_DISPLAY.title"), _stamp.Name);
        CustomMinimumSize = new Vector2(128, 128);
        MouseFilter       = MouseFilterEnum.Stop;
        Name = _stamp.Name;
        
        var deleteButton = new NDeleteButton();
        deleteButton.Initialize(this, _configStampPreviewControl);
        deleteButton.Visible = Config.EnableDeleteButton;
        AddChild(deleteButton);
        
        var shareButton = new NShareButton();
        shareButton.Initialize(this, _configStampPreviewControl);
        shareButton.Visible = Config.EnableShareButton;
        AddChild(shareButton);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            QueueRedraw();
    }

    public override void _Draw()
    {
        // Background
        DrawRect(new Rect2(Vector2.Zero, Size), BgColor);
        DrawRect(new Rect2(Vector2.One, Size - Vector2.One * 2), BorderColor, filled: false, width: 1.5f);

        if (_stamp == null || _stamp.Strokes.Count == 0) return;

        var (normalized, scale) = Normalize(_stamp.Strokes);
        foreach (var (points, erase) in normalized)
        {
            if (points.Count < 2 || erase) continue;   // skip erase strokes in thumbnails
            DrawPolyline(points.ToArray(), LineColor, DrawWidth * scale, antialiased: true);
        }
    }

    protected override void OnFocus()
    {
        if (_stamp == null) return;
        NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, _hoverTip);
        nHoverTipSet.GlobalPosition = GlobalPosition + new Vector2(Size.X - nHoverTipSet.Size.X, Size.Y + 20f);
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
    }
    
    private (List<(List<Vector2> Points, bool Erase)> Strokes, float Scale) Normalize(List<StampStroke> strokes)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var stroke in strokes)
            foreach (var p in stroke.Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

        float stampW = maxX - minX; if (stampW == 0) stampW = 1;
        float stampH = maxY - minY; if (stampH == 0) stampH = 1;

        float availW = Size.X - DrawPadding * 2;
        float availH = Size.Y - DrawPadding * 2;
        float scale   = Mathf.Min(availW / stampW, availH / stampH) * 0.8f;

        float offsetX = DrawPadding + (availW - stampW * scale) * 0.5f - minX * scale;
        float offsetY = DrawPadding + (availH - stampH * scale) * 0.5f - minY * scale;

        var result = new List<(List<Vector2>, bool)>();
        foreach (var stroke in strokes)
        {
            var pts = new List<Vector2>(stroke.Points.Count);
            foreach (var p in stroke.Points)
                pts.Add(new Vector2(p.X * scale + offsetX, p.Y * scale + offsetY));
            result.Add((pts, stroke.Erase));
        }
        return (result, scale);
    }
}
