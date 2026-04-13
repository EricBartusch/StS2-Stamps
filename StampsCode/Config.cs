using System.Runtime.InteropServices;
using System.Text.Json;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode;

public class Config : SimpleModConfig
{
    private ConfigStampPreviewControl _stampPreview; 
    private static readonly string DefaultStampDir = Path.Combine(OS.GetUserDataDir(), "mod_configs", "CustomStamps");


    public static string CustomStampDir { get; set; } = DefaultStampDir;
    [ConfigButton("Open Stamps Folder")]
    public void OpenStampsFolder()
    {
        var findErrorText = new LocString("settings_ui", "STAMP_FOLDER_FOUND_ERROR").GetFormattedText();
        var errorText = new LocString("settings_ui", "STAMP_FOLDER_ERROR").GetFormattedText();
        if (!Directory.Exists(CustomStampDir))
        {
            ModConfigLogger.Error($"{findErrorText} {CustomStampDir}", showInGui: true);
            return;
        }

        var path = CustomStampDir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            path = path.Replace('/', '\\');

        var error = OS.ShellShowInFileManager(path);
        if (error != Error.Ok)
            ModConfigLogger.Error($"{errorText} ({error}): {path}", showInGui: true);
    }

    private static double _buttonColumns = 4;
    [SliderRange(2, 7, 1)]
    public static double ButtonColumns
    {
        get => _buttonColumns;
        set
        {
          _buttonColumns = value;
          ButtonColumnsChanged?.Invoke();
        }
    }
    public static event Action? ButtonColumnsChanged;
    
    public static event Action? EnableDeleteButtonChanged;
    private static bool _enableDeleteButton = false;
    public static bool EnableDeleteButton
    {
        get => _enableDeleteButton;
        set
        {
            _enableDeleteButton = value;
            EnableDeleteButtonChanged?.Invoke();
        }
    }
    
    public static void SeedDefaultStampsIfNeeded(bool force = false)
    {
        if (!force)
        {
            if (CustomStampDir != DefaultStampDir || Directory.Exists(CustomStampDir))
                return;
        }

        if (Directory.Exists(CustomStampDir))
            Directory.Delete(CustomStampDir, recursive: true);

        Directory.CreateDirectory(CustomStampDir);

        var options = new JsonSerializerOptions { WriteIndented = true };

        foreach (var stamp in DefaultStamps.All)
        {
            var dto = new StampDto
            {
                Strokes = stamp.Strokes.Select(s => new StrokeDto
                {
                    Erase = s.Erase,
                    Points = s.Points.Select(p => new PointDto
                    {
                        X = p.X / CustomStampLoader.NormalizedScale + 0.5f,
                        Y = p.Y / CustomStampLoader.NormalizedScale + 0.5f,
                    }).ToArray()
                }).ToList()
            };

            File.WriteAllText(
                Path.Combine(CustomStampDir, stamp.Name + ".json"),
                JsonSerializer.Serialize(dto, options));
        }

        if (force)
        {
            
        }
    }

    [ConfigButton("Reload Stamps")]
    public void ReloadStamps()
    {
        CustomStampLoader.LoadStamps(CustomStampDir);
        _stampPreview.Reload();
    }
    
    public override void SetupConfigUI(Control optionContainer)
    {
        ClearUIEventHandlers();

        GenerateOptionsForAllProperties(optionContainer);
        AddConfigStampPreview(optionContainer);
        AddRestoreDefaultsAndSeedButton(optionContainer);
    }

    private void AddRestoreDefaultsAndSeedButton(Control optionContainer)
    {
        var resetButton = CreateRawButtonControl(GetBaseLibLabelText("RestoreDefaultsButton"), async void () =>
        {
            bool confirmed = false;
            void onReloaded() => confirmed = true;
            OnConfigReloaded += onReloaded;
            try
            {
                await ConfirmRestoreDefaults();
            }
            catch (Exception e)
            {
                ModConfigLogger.Error($"Unable to show restore confirmation dialog: {e.Message}");
            }
            OnConfigReloaded -= onReloaded;

            if (confirmed)
            {
                SeedDefaultStampsIfNeeded(true);
                CustomStampLoader.LoadStamps(CustomStampDir);
                _stampPreview.Reload();
            }
        });
        resetButton.CustomMinimumSize = new Vector2(360, resetButton.CustomMinimumSize.Y);
        resetButton.SetColor(0.45f, 1.5f, 0.8f);

        var centerContainer = new CenterContainer();
        centerContainer.CustomMinimumSize = new Vector2(0, 128);
        centerContainer.AddChild(resetButton);
        optionContainer.AddChild(centerContainer);
    }

    private void AddConfigStampPreview(Control optionContainer)
    {
        _stampPreview = new ConfigStampPreviewControl();
        var centerContainer = new CenterContainer();
        centerContainer.CustomMinimumSize = new Vector2(0, 128);
        centerContainer.AddChild(_stampPreview);
        optionContainer.AddChild(CreateSectionHeader("Stamps"));
        optionContainer.AddChild(centerContainer);

    }
}
