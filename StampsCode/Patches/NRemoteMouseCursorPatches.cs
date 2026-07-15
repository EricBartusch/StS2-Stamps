using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Stamps.StampsCode.Networking;

namespace Stamps.StampsCode.Patches;

public class NRemoteMouseCursorPatches
{
    [HarmonyPatch(typeof(NRemoteMouseCursor), "UpdateImage")]
    public static class UpdateImagePatch
    {
        static void Prefix(NRemoteMouseCursor __instance, ref DrawingMode drawingMode)
        {
            if (drawingMode == DrawingMode.None
                && MultiplayerManager.IsPlayerInStampMode(__instance.PlayerId))
            {
                drawingMode = NMapDrawingsPatches.Stamp;
            }
        }
    }

    [HarmonyPatch(typeof(NRemoteMouseCursor), "GetTexture")]
    public static class GetTexturePatch
    {
        static bool Prefix(
            ref Texture2D __result,
            bool isDown,
            DrawingMode drawingMode
        )
        {
            if (drawingMode == NMapDrawingsPatches.Stamp)
            {
                var cursorImage = isDown ? "stamp_click.png" : "stamp_cursor.png";
                __result = GD.Load<Texture2D>(Path.Join(MainFile.ModId, cursorImage));
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(NRemoteMouseCursor), "GetHotspot")]
    public static class GetHotspotPatch
    {
        static bool Prefix(
            ref Vector2 __result,
            DrawingMode drawingMode
        )
        {
            if (drawingMode == NMapDrawingsPatches.Stamp)
            {
                __result = -NMapDrawingsPatches.StampHotspot;
                return false;
            }

            return true;
        }
    }
}