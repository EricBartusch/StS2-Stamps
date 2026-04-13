using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes;
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

    static readonly Func<NMapDrawings, Player, bool, Line2D> _createLineForPlayer =
        AccessTools.MethodDelegate<Func<NMapDrawings, Player, bool, Line2D>>(
            AccessTools.Method(typeof(NMapDrawings), "CreateLineForPlayer",
                [typeof(Player), typeof(bool)])
        );

    public static ulong LocalNetId;

    [HarmonyPostfix]
    [HarmonyPatch("Initialize")]
    private static void InitializePostfix(NMapDrawings __instance, INetGameService netService, IPlayerCollection playerCollection, PeerInputSynchronizer inputSynchronizer)
    {
        LocalNetId  = netService.NetId;
        PlayerColor = playerCollection.GetPlayer(netService.NetId).Character.MapDrawingColor;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("BeginLine")]
    private static bool BeginLinePatch(NMapDrawings __instance, object state, Vector2 position,
        DrawingMode? overrideDrawingMode, IPlayerCollection ____playerCollection)
    {
        var traverse = Traverse.Create(state);
        var mode = overrideDrawingMode ?? traverse.Field("drawingMode").GetValue<DrawingMode>();

        if (mode != Stamp)
        {
            // Don't draw behind the stamp picker screen
            if (IsInsidePickerScreen(__instance, position))
                return false;

            if (mode == DrawingMode.Drawing || mode == DrawingMode.Erasing)
            {
                if (StampRecorder.IsRecording
                    && traverse.Field("playerId").GetValue<ulong>() == LocalNetId)
                {
                    var toolbarRect = __instance.GetParent().GetParent().GetChild<NinePatchRect>(4).GetGlobalRect();
                    var globalPos = __instance.GetGlobalTransform() * position;
                    if (!toolbarRect.HasPoint(globalPos))
                        StampRecorder.RecordClickPosition(position);
                    else
                        StampRecorder.SuppressNextStroke();
                }
            }

            return true;
        }

        
        if (SkipDrawingBecauseInBox(__instance, position, traverse, out var drawViewport))
        {
            return false;
        }
        
        ulong playerId = traverse.Field("playerId").GetValue<ulong>();
        Player player = ____playerCollection.GetPlayer(playerId);

        if (LocalContext.IsMe(player))
        {
            if (StampRegistry.ActiveStamp == null)
                return false;

            foreach (var stroke in StampRegistry.ActiveStamp.Strokes)
            {
                Line2D line = _createLineForPlayer(__instance, player, stroke.Erase);
                foreach (var point in stroke.Points)
                    line.AddPoint((position + point) * 0.5f);

                traverse.Field("currentlyDrawingLine").SetValue(line);
                drawViewport.AddChildSafely(line);
                NGame.Instance.RemoteCursorContainer.DrawingCursorStateChanged(playerId);
            }
        }
        else
        {
            StampDefinition stamp = MultiplayerManager.GetPlayerStampDefinition(playerId);
            foreach (var stroke in stamp?.Strokes)
            {
                Line2D line = _createLineForPlayer(__instance, player, stroke.Erase);
                foreach (var point in stroke.Points)
                    line.AddPoint((position + point) * 0.5f);

                traverse.Field("currentlyDrawingLine").SetValue(line);
                drawViewport.AddChildSafely(line);
                NGame.Instance.RemoteCursorContainer.DrawingCursorStateChanged(playerId);
            }
        }

        return false;
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

    private static bool IsInsidePickerScreen(NMapDrawings __instance, Vector2 position)
    {
        var stampPickerScreen = __instance.GetParent().GetParent().GetNode<NStampPickerScreen>("StampPickerScreen");
        return stampPickerScreen.Visible
            && stampPickerScreen.GetGlobalRect().HasPoint(__instance.GetGlobalTransform() * position);
    }

    private static bool SkipDrawingBecauseInBox(NMapDrawings __instance, Vector2 position, Traverse traverse,
        out SubViewport drawViewport)
    {
        drawViewport = traverse.Field("drawViewport").GetValue<SubViewport>();
        var rect = __instance.GetParent().GetParent().GetChild<NinePatchRect>(4).GetGlobalRect();

        // Icon toolbar at bottom
        if (rect.HasPoint(__instance.GetGlobalTransform() * position))
        {
            return true;
        }

        // Stamp screen
        if (IsInsidePickerScreen(__instance, position))
        {
            return true;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("UpdateCurrentLinePosition")]
    private static bool UpdateCurrentLinePositionPatch(object state)
    {
        // Skip this entirely when stamping
        return Traverse.Create(state).Field("drawingMode").GetValue<DrawingMode>() != Stamp;
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
