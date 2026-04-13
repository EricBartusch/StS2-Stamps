using Godot;
using MegaCrit.Sts2.Core.Localization;
using Stamps.StampsCode.StampUI;

namespace Stamps.StampsCode.Recorder;

public partial class NStampSaveDialog : Panel
{
    private LineEdit _nameInput = null!;
    private NMapRecordButton _recordButton = null!;
    private NStampPreviewControl _preview = null!;

    private static readonly Shader BlurShader = GD.Load<Shader>(
        "res://shaders/dark_blur.gdshader"
    );

    public override void _Ready()
    {
        var style = new StyleBoxFlat
        {
            BgColor                = Colors.Transparent,
        };
        AddThemeStyleboxOverride("panel", style);

        AnchorLeft   = AnchorRight  = 0.5f;
        AnchorTop    = AnchorBottom = 0.5f;
        OffsetLeft   = -160f;
        OffsetRight  =  160f;
        OffsetTop    = -200f;
        OffsetBottom =  170f;
        MouseFilter  = MouseFilterEnum.Stop;
        ZIndex       = 100;
        
        var britishBroadcastingCompany = new BackBufferCopy
        {
            CopyMode   = BackBufferCopy.CopyModeEnum.Viewport,
        };
        AddChild(britishBroadcastingCompany);
        
        var blurMat = new ShaderMaterial { Shader = BlurShader };
        blurMat.SetShaderParameter("lod", 3.0f);
        blurMat.SetShaderParameter("mix_percentage", 0.75f);
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

        var vbox = new VBoxContainer
        {
            LayoutMode  = 1,
            AnchorLeft  = 0f, AnchorTop    = 0f,
            AnchorRight = 1f, AnchorBottom  = 1f,
            OffsetLeft  = 16f, OffsetTop    = 16f,
            OffsetRight = -16f, OffsetBottom = -16f,
        };
        AddChild(vbox);

        _preview = new NStampPreviewControl
        {
            Name                = "StampPreview",
            CustomMinimumSize   = new Vector2(0, 200),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical   = SizeFlags.ShrinkBegin,
            MouseFilter         = MouseFilterEnum.Ignore,
        };
        vbox.AddChild(_preview);
        
        _nameInput = new LineEdit
        {
            PlaceholderText     = new LocString("map", "STAMP_SAVE_DIALOG.placeholder").GetFormattedText(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _nameInput.TextSubmitted += _ => OnSave();
        vbox.AddChild(_nameInput);

        var gap = new Control { CustomMinimumSize = new Vector2(0, 8) };
        vbox.AddChild(gap);

        var hbox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        hbox.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(hbox);

        var saveBtn = new Button
        {
            Text                = new LocString("map", "STAMP_SAVE_DIALOG.save").GetFormattedText(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        saveBtn.Pressed += OnSave;
        hbox.AddChild(saveBtn);

        var discardBtn = new Button
        {
            Text                = new LocString("map", "STAMP_SAVE_DIALOG.discard").GetFormattedText(),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        discardBtn.Pressed += OnDiscard;
        hbox.AddChild(discardBtn);

        Visible = false;
    }

    public void Prompt(NMapRecordButton button)
    {
        _recordButton   = button;
        _nameInput.Text = string.Empty;

        var previewStamp = StampRecorder.BuildPreview();
        if (previewStamp != null && !previewStamp.OnlyEraseStrokes())
        {
            _preview.SetStamp(previewStamp);
            Visible = true;
            _nameInput.GrabFocus();

        }
        else
        {
            _recordButton.SetRecording(false);
            StampRecorder.DiscardRecording();
        }
    }

    private void OnSave()
    {
        string name = SanitizeName(_nameInput.Text.Trim());
        if (string.IsNullOrEmpty(name)) return;

        Visible = false;
        _recordButton.SetRecording(false);
        StampRecorder.StampCompleted(name);
    }

    private void OnDiscard()
    {
        Visible = false;
        _recordButton.SetRecording(false);
        StampRecorder.DiscardRecording();
    }

    private static string SanitizeName(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c.ToString(), string.Empty);
        return input;
    }
}
