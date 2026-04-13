using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Stamps.StampsCode;
using Stamps.StampsCode.Stamps;

namespace Stamps;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Stamps";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new Config());

        Harmony harmony = new(ModId);
        Config.SeedDefaultStampsIfNeeded();
        CustomStampLoader.LoadStamps(Config.CustomStampDir);

        harmony.PatchAll();
    }
}