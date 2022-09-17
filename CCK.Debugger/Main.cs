﻿using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.UserGeneratedContent;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using CCK.Debugger.Components;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;

namespace CCK.Debugger;


[BepInPlugin(Properties.AssemblyInfoParams.Name, Properties.AssemblyInfoParams.Name, Properties.AssemblyInfoParams.Version)]
public class CCKDebugger : BaseUnityPlugin {
    public static ManualLogSource instanceLogger;
    CCKDebugger() => instanceLogger = Logger;

    internal static bool TestMode;

    private const string _assetPath = "Assets/Prefabs/CCKDebuggerMenu.prefab";

    public void Awake() {

        // Check if it is in debug mode (to test functionalities that are waiting for bios to be enabled)
        // Keeping it hard-ish to enable so people don't abuse it
        foreach (var commandLineArg in Environment.GetCommandLineArgs()) {
            if (!commandLineArg.StartsWith("--cck-debugger-test=")) continue;
            var input = commandLineArg.Split(new[] { "=" }, StringSplitOptions.None)[1];
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new System.Text.StringBuilder();
            foreach (var t in hashBytes) sb.Append(t.ToString("X2"));
            TestMode = sb.ToString().Equals("738A9A4AD5E2F8AB10E702D44C189FA8");
            if (TestMode) Logger.LogMessage("Test Mode is ENABLED!");
        }
    }

    [HarmonyPatch]
    private class HarmonyPatches
    {
        // Spawnables
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Spawnable_t), nameof(Spawnable_t.Recycle))]
        static void BeforeSpawnableDetailsRecycle(Spawnable_t __instance) {
            Events.Spawnable.OnSpawnableDetailsRecycled(__instance);
        }

        // Avatar
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AvatarDetails_t), nameof(AvatarDetails_t.Recycle))]
        static void BeforeAvatarDetailsRecycle(AvatarDetails_t __instance) {
            Events.Avatar.OnAvatarDetailsRecycled(__instance);
        }

        // Player
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MovementSystem), nameof(MovementSystem.UpdateAnimatorManager))]
        static void AfterUpdateAnimatorManager(CVRAnimatorManager manager) {
            Events.Avatar.OnAnimatorManagerUpdate(manager);
        }

        //Quick Menu
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), nameof(CVR_MenuManager.ToggleQuickMenu))]
        private static void AfterMenuToggle(bool show) {
            Events.QuickMenu.OnQuickMenuIsShownChanged(show);
        }

        // Players
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CVRPlayerManager), nameof(CVRPlayerManager.TryCreatePlayer))]
        private static void BeforeCreatePlayer(ref CVRPlayerManager __instance, out int __state) {
            __state = __instance.NetworkPlayers.Count;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVRPlayerManager), nameof(CVRPlayerManager.TryCreatePlayer))]
        private static void AfterCreatePlayer(ref CVRPlayerManager __instance, int __state) {
            if(__state < __instance.NetworkPlayers.Count) {
                var player = __instance.NetworkPlayers[__state];
                Events.Player.OnPlayerLoaded(player.Uuid, player.Username);
            }
        }

        // Scene
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
        private static void AfterMenuCreated(ref CVR_MenuManager __instance) {
            var quickMenuTransform = __instance.quickMenu.transform;

            // Load prefab from asset bundle
            AssetBundle assetBundle = AssetBundle.LoadFromMemory(Resources.Resources.cckdebugger);
            assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var prefab = assetBundle.LoadAsset<GameObject>(_assetPath);

            // Instantiate and add the controller script
            var cckDebugger = UnityEngine.Object.Instantiate(prefab, quickMenuTransform);
            cckDebugger.AddComponent<Menu>();

            // Add ourselves to the player list (why not here xd)
            Events.Player.OnPlayerLoaded(MetaPort.Instance.ownerId, MetaPort.Instance.username);
        }
    }
}
