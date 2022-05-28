using BepInEx;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Reflection;
using UnityEngine.Networking;

namespace CoinDropChance
{
    public class Hooks
    {
        public static void Init()
        {
            //main setup hook; this is where most of the mod's settings are applied
            On.RoR2.Run.Start += Run_Start;

            //Coin base drop chance
            On.RoR2.PlayerCharacterMasterController.Awake += PlayerCharacterMasterController_Awake;

            //Coin drop multiplier
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);
        }

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = "ExamplePlugin started"
            });
        }

        private static void PlayerCharacterMasterController_Awake(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
        {
            orig(self);
            self.SetFieldValue("lunarCoinChanceMultiplier", BepConfig.DropChance.Value);
        }

        private static void CoinDropHook(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchDup(),
                x => x.MatchLdfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier"),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul()
                );
            c.Index += 2;
            c.Next.Operand = BepConfig.DropMulti.Value;
            c.Index += 2;
            c.EmitDelegate<Func<float, float>>((originalChance) =>
            {
                return Math.Max(originalChance, BepConfig.DropMin.Value);
            });
        }
    }
}
