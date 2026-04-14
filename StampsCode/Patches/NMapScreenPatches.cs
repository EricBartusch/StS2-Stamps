using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Stamps.StampsCode.Networking;
using Stamps.StampsCode.Recorder;
using NStampPickerScreen = Stamps.StampsCode.StampUI.NStampPickerScreen;

namespace Stamps.StampsCode.Patches;

[HarmonyPatch(typeof(NMapScreen))]
public static class NMapScreenPatches
{
    static readonly AccessTools.FieldRef<NMapScreen, NMapDrawingInput> _drawingInputRef =
        AccessTools.FieldRefAccess<NMapScreen, NMapDrawingInput>("_drawingInput");
    
    static readonly Dictionary<NMapScreen, NStampPickerScreen> _pickers = new();
    static readonly Dictionary<NMapScreen, NStampSaveDialog> _dialogs = new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    static void AddStampButton(NMapScreen __instance)
    {
        var drawingTools = __instance.GetNode<NinePatchRect>("DrawingTools");
        drawingTools.OffsetRight += 120; // Add space for two new buttons, so add 60 for each one added

        var toolsBox = __instance.GetNode<HBoxContainer>("DrawingTools/HBoxContainer");
        toolsBox.OffsetLeft  -= 60;
        toolsBox.OffsetRight += 60; // Makes the black part of the toolbar bigger
        
        var stampButton = new StampUI.NMapStampButton { Name = "StampButton" };
        toolsBox.AddChild(stampButton);
        stampButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<StampUI.NMapStampButton>(OnStampButtonPressed)
        );

        var recordButton = new NMapRecordButton { Name = "RecordButton" };
        toolsBox.AddChild(recordButton);
        recordButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NMapRecordButton>(OnRecordButtonPressed)
        );

        var picker = new NStampPickerScreen
        {
            Name = "StampPickerScreen",
        };
        picker.Initialize(__instance, stampButton);
        __instance.AddChild(picker);
        _pickers[__instance] = picker;

        var dialog = new NStampSaveDialog { Name = "StampSaveDialog" };
        __instance.AddChild(dialog);
        _dialogs[__instance] = dialog;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    static void RemovePicker(NMapScreen __instance)
    {
        _pickers.Remove(__instance);
        _dialogs.Remove(__instance);
    }

    static void OnStampButtonPressed(StampUI.NMapStampButton button)
    {
        NMapScreen screen = button.GetParent().GetParent().GetParent<NMapScreen>();
        if (_pickers.TryGetValue(screen, out var picker))
            picker.Toggle();
    }

    static void OnRecordButtonPressed(NMapRecordButton button)
    {
        var screen = button.GetParent().GetParent().GetParent<NMapScreen>();
        var dialogValid = _dialogs.TryGetValue(screen, out var dialog);
        
        if (dialogValid && dialog.Visible)
        {
            return;
        }
        
        if (StampRecorder.IsRecording)
        {
            StampRecorder.CancelRecording();
            if (dialogValid)
                dialog.Prompt(button);
        }
        else if (MultiplayerManager.SharedStamps.Count > 0)
        {
            if (dialogValid)
            {
                var message = MultiplayerManager.ReadSharedStampDefinition();
                dialog.Prompt(button, message);
            }
        }
        else
        {
            StampRecorder.StartRecording();
            button.SetRecording(true);
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch("UpdateDrawingButtonStates")]
    private static void UpdateDrawingButtonStatesPatch(NMapScreen __instance)
    {
        var stampButton = __instance.GetNode<StampUI.NMapStampButton>("DrawingTools/HBoxContainer/StampButton");
        stampButton.SetActive(__instance.Drawings.GetLocalDrawingMode() == NMapDrawingsPatches.Stamp);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnClearMapDrawingButtonPressed")]
    private static void OnClearMapDrawingButtonPressedPostfix(NMapScreen __instance)
    {
        _drawingInputRef(__instance)?.StopDrawing();
    }
}
