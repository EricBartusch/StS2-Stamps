using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Stamps.StampsCode.Networking;

[HarmonyPatch]
public static class NetworkingPatches
{
    [HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor, typeof(GameMode), typeof(INetGameService),
        typeof(IStartRunLobbyListener), typeof(int))]
    [HarmonyPostfix]
    static void OnStartRunLobbyConstructed(StartRunLobby __instance)
    {
        MultiplayerManager.Initialize(__instance.NetService);
    }

    [HarmonyPatch(typeof(LoadRunLobby), MethodType.Constructor, typeof(INetGameService),
        typeof(ILoadRunLobbyListener), typeof(SerializableRun))]
    [HarmonyPostfix]
    static void OnLoadRunLobbyConstructed(LoadRunLobby __instance)
    {
        MultiplayerManager.Initialize(__instance.NetService);
    }
}