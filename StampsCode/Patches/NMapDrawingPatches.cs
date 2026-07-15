using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using Stamps.StampsCode.Networking;
using Stamps.StampsCode.Recorder;
using Stamps.StampsCode.Stamps;
using NStampPickerScreen = Stamps.StampsCode.StampUI.NStampPickerScreen;

namespace Stamps.StampsCode.Patches;

[HarmonyPatch(typeof(NMapDrawings))]
public static class NMapDrawingsPatches
{
    [CustomEnum]
    public static DrawingMode Stamp;

    // This is used to line up the stamp icon with where the stamp actually stamps to
    public static Vector2 StampHotspot = new(30, 55);

    public static Color PlayerColor;

    public static ulong LocalNetId;
    private static bool _replayingStamp;
    private static bool _stampModeBroadcast;

    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    private static void InitializePostfix(NMapDrawings __instance, INetGameService netService, IPlayerCollection playerCollection, PeerInputSynchronizer inputSynchronizer)
    {
        LocalNetId  = netService.NetId;
        PlayerColor = playerCollection.GetPlayer(netService.NetId).Character.MapDrawingColor;
        _stampModeBroadcast = false;
    }

    // use the normal line drawing methods so we not modded clients can still see stamps
    [HarmonyPrefix]
    [HarmonyPatch("BeginLineLocal")]
    private static bool BeginLineLocalPatch(NMapDrawings __instance, Vector2 position, DrawingMode? overrideDrawingMode)
    {
        // if we are in the process of drawing a stamp or if the player is using one of the 
        // mouse buttons to make a mark on the map, just do normal stuff
        if (_replayingStamp || overrideDrawingMode.HasValue)
            return true;
        if (__instance.GetLocalDrawingMode(false) != Stamp)
            return true;

        // somehow we tried to make a stamp without an active one or we're in a place we don't want to put stamps
        // like the toolbar itself or the stamp screen is up
        if (StampRegistry.ActiveStamp == null
            || IsInsideToolbar(__instance, position)
            || IsInsidePickerScreen(__instance, position))
            return false;


        // start up the replay and go through each stroke and call the vanilla methods
        // because we call BeginLineLocal in this patch, we have those checks above
        _replayingStamp = true;
        try
        {
            foreach (var stroke in StampRegistry.ActiveStamp.Strokes)
            {
                if (stroke.Points.Count == 0)
                    continue;

                var mode = stroke.Erase ? DrawingMode.Erasing : DrawingMode.Drawing;
                __instance.BeginLineLocal(position + stroke.Points[0], mode);
                for (int i = 1; i < stroke.Points.Count; i++)
                    __instance.UpdateCurrentLinePositionLocal(position + stroke.Points[i]);
                __instance.StopLineLocal();
            }
        }
        finally
        {
            _replayingStamp = false;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("BeginLine")]
    private static bool BeginLinePatch(NMapDrawings __instance, object state, Vector2 position,
        DrawingMode? overrideDrawingMode)
    {
        if (_replayingStamp)
            return true;

        var traverse = Traverse.Create(state);
        var mode = overrideDrawingMode ?? traverse.Field("drawingMode").GetValue<DrawingMode>();

        //  never draw a raw line in stamp mode
        if (mode == Stamp)
            return false;

        // Don't draw behind the stamp picker screen
        if (IsInsidePickerScreen(__instance, position))
            return false;

        if (mode == DrawingMode.Drawing || mode == DrawingMode.Erasing)
        {
            if (StampRecorder.IsRecording
                && traverse.Field("playerId").GetValue<ulong>() == LocalNetId)
            {
                if (!IsInsideToolbar(__instance, position))
                    StampRecorder.RecordClickPosition(position);
                else
                    StampRecorder.SuppressNextStroke();
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("StopDrawingLine")]
    private static bool StopDrawingLinePatch(NMapDrawings __instance, object state)
    {
        var traverse = Traverse.Create(state);
        var mode = traverse.Field("drawingMode").GetValue<DrawingMode>();

        if (mode == DrawingMode.Drawing || mode == DrawingMode.Erasing)
        {
            if (StampRecorder.IsRecording)
            {
                Line2D line = traverse.Field("currentlyDrawingLine").GetValue() as Line2D;
                if (line != null)
                {
                    StampRecorder.OnLineCompleted(line, StampRecorder.PendingClickPosition,
                        erase: mode == DrawingMode.Erasing);
                }
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetDrawingModeLocal")]
    private static bool SetDrawingModeLocalPatch(NMapDrawings __instance, DrawingMode drawingMode)
    {
        // if we aren't stamping but we just were, tell them we stopped
        if (drawingMode != Stamp)
        {
            if (_stampModeBroadcast)
            {
                _stampModeBroadcast = false;
                MultiplayerManager.BroadcastStampMode(false);
            }
            return true;
        }

        var t = Traverse.Create(__instance);
        var netService = t.Field("_netService").GetValue<INetGameService>();
        if (netService == null)
            return true;

        var state = t.Method("GetDrawingStateForPlayer", netService.NetId).GetValue<object>();
        t.Method("SetDrawingMode", state, drawingMode).GetValue();
        netService.SendMessage(new MapDrawingModeChangedMessage { drawingMode = DrawingMode.None });
        t.Method("UpdateLocalCursor").GetValue();

        // if we just started stamping, tell them 
        if (!_stampModeBroadcast)
        {
            _stampModeBroadcast = true;
            MultiplayerManager.BroadcastStampMode(true);
        }
        return false;
    }

    private static bool IsInsidePickerScreen(NMapDrawings __instance, Vector2 position)
    {
        var stampPickerScreen = __instance.GetParent().GetParent().GetNode<NStampPickerScreen>("StampPickerScreen");
        return stampPickerScreen.Visible
            && stampPickerScreen.GetGlobalRect().HasPoint(__instance.GetGlobalTransform() * position);
    }

    // Icon toolbar at the bottom of the map screen
    private static bool IsInsideToolbar(NMapDrawings __instance, Vector2 position)
    {
        var rect = __instance.GetParent().GetParent().GetChild<NinePatchRect>(4).GetGlobalRect();
        return rect.HasPoint(__instance.GetGlobalTransform() * position);
    }

    [HarmonyPrefix]
    [HarmonyPatch("UpdateCurrentLinePosition")]
    private static bool UpdateCurrentLinePositionPatch(object state)
    {
        // Skip this entirely when stamping
        var traverse = Traverse.Create(state);
        var mode = traverse.Field("overrideDrawingMode").GetValue<DrawingMode?>()
                   ?? traverse.Field("drawingMode").GetValue<DrawingMode>();
        return mode != Stamp;
    }

    [HarmonyPostfix]
    [HarmonyPatch("ClearDrawnLinesLocal")]
    private static void ClearRecordingToo()
    {
        if (StampRecorder.IsRecording)
        {
            StampRecorder.RestartRecording();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("UpdateLocalCursor")]
    private static bool UpdateLocalCursorPatch(NMapDrawings __instance)
    {
        var t = Traverse.Create(__instance);
        var netService = t.Field("_netService").GetValue<object>();
        if (netService == null)
            return true;

        ulong netId = Traverse.Create(netService).Property("NetId").GetValue<ulong>();
        var drawingState = t.Method("GetDrawingStateForPlayer", netId).GetValue<object>();
        var mode = Traverse.Create(drawingState).Property("CurrentDrawingMode").GetValue<DrawingMode>();
        if (mode != Stamp)
            return true;

        Texture2D cursor = GD.Load<Texture2D>(Path.Join(MainFile.ModId, "stamp_cursor.png"));
        Texture2D click = GD.Load<Texture2D>(Path.Join(MainFile.ModId, "stamp_click.png"));
        Image image = cursor.GetImage();
        Image image2 = click.GetImage();
        Vector2 hotspot = StampHotspot;
        var cursorManager = t.Field("_cursorManager").GetValue<object>();
        Traverse.Create(cursorManager).Method("OverrideCursor", image2, image, hotspot).GetValue();
        return false;
    }
}
