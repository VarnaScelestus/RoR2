using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace EphemeralCoins
{
    [BepInDependency("com.bepis.r2api")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Varna.EphemeralCoins", "Ephemeral_Coins", "1.2.1")]
    public class EphemeralCoins : BaseUnityPlugin
    {
        private static ConfigEntry<bool> ResetCoins;
        private static ConfigEntry<int> StartingCoins;
        private static ConfigEntry<float> DropChance;
        private static ConfigEntry<float> DropMulti;
        private static ConfigEntry<int> PodCost;
        private static ConfigEntry<int> ShopCost;
        private static ConfigEntry<bool> ShopRefresh;
        private static ConfigEntry<int> SeerCost;
        private static ConfigEntry<int> RerollCost;
        private static ConfigEntry<int> RerollAmount;
        private static ConfigEntry<int> RerollScale;
        private int numTimesRerolled;
        private static ConfigEntry<float> PortalChance;
        private static ConfigEntry<bool> PortalScale;

        public void Awake()
        {
            ResetCoins = Config.Bind("", "ResetCoins", true, new ConfigDescription("Remove all Lunar Coins on run start?"));
            StartingCoins = Config.Bind("", "StartingCoins", 0, new ConfigDescription("The number of Lunar Coins each player starts with. (only if ResetCoins is true)"));
            DropChance = Config.Bind("", "DropChance", 5.0f, new ConfigDescription("The initial %chance for enemies to drop coins. Vanilla is 0.5%"));
            DropMulti = Config.Bind("", "DropMulti", 0.90f, new ConfigDescription("The multiplier applied to the drop chance after a coin has dropped. Vanilla is 0.5 (50%)"));
            PodCost = Config.Bind("", "PodCost", 0, new ConfigDescription("The cost of Lunar Pods. Vanilla is 1"));
            ShopCost = Config.Bind("", "ShopCost", 1, new ConfigDescription("The cost of Lunar Buds in BTB. Vanilla is 2"));
            ShopRefresh = Config.Bind("", "ShopRefresh", true, new ConfigDescription("Do empty Lunar Buds in BTB refresh when the Slab (reroller) is used? Vanilla is false"));
            SeerCost = Config.Bind("", "SeerCost", 1, new ConfigDescription("The cost of Lunar Seers in BTB. Vanilla is 3"));
            RerollCost = Config.Bind("", "RerollCost", 0, new ConfigDescription("The initial cost of the Slab (reroller) in BTB. Vanilla is 1"));
            RerollAmount = Config.Bind("", "RerollAmount", 1, new ConfigDescription("How many times can the Slab (reroller) in BTB be used? Enter 0 for infinite (vanilla)."));
            RerollScale = Config.Bind("", "RerollScale", 2, new ConfigDescription("The cost multiplier per use of the Slab (reroller) in BTB. Vanilla is 2"));
            PortalChance = Config.Bind("", "PortalChance", 0.375f, new ConfigDescription("The chance of a Blue Orb appearing on stage start. Vanilla is 0.375 (37.5%)"));
            PortalScale = Config.Bind("", "PortalScale", false, new ConfigDescription("Scale down the chance of a Blue Orb appearing for each time BTB has been visited? Vanilla behavior is true"));

            numTimesRerolled = 0;

            On.RoR2.Run.Start += Run_Start;
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);
            On.RoR2.PlayerCharacterMasterController.Awake += PlayerCharacterMasterController_Awake;
            On.RoR2.SceneDirector.Start += SceneDirector_Start;
            On.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
            On.RoR2.PurchaseInteraction.SetAvailable += PurchaseInteraction_SetAvailable;
            On.RoR2.PurchaseInteraction.ScaleCost += PurchaseInteraction_ScaleCost;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += ShopTerminalBehavior_GenerateNewPickupServer;
            On.RoR2.TeleporterInteraction.Start += TeleporterInteraction_Start;
        }

        private void PlayerCharacterMasterController_Awake(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
        {
            orig(self);
            self.SetFieldValue("lunarCoinChanceMultiplier", DropChance.Value);
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            if (NetworkServer.active && ResetCoins.Value)
            {
                bool check;
                if (ProperSaveCompatibility.enabled) { check = ProperSaveCompatibility.IsRunNew(); }
                else { check = Run.instance.stageClearCount == 0?true:false; }

                if (check)
                    {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#beeca1><size=15px>A new moon rises...</size></color>" });
                    if (RoR2Application.isInSinglePlayer) { StartCoroutine(DelayedAutomaticCoinRemovalLocal()); }
                    else { StartCoroutine(DelayedAutomaticCoinRemoval()); }
                }
            }
        }

        private void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                StartCoroutine(DelayedLunarPriceChange());
                numTimesRerolled = 0;
            }
        }

        private void BazaarController_SetUpSeerStations(On.RoR2.BazaarController.orig_SetUpSeerStations orig, BazaarController self)
        {
            orig(self);
            foreach (SeerStationController seerStationController in self.seerStations)
            {
                seerStationController.GetComponent<PurchaseInteraction>().Networkcost = SeerCost.Value;
                if (seerStationController.GetComponent<PurchaseInteraction>().Networkcost == 0) { seerStationController.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None; }
            }
        }

        private void TeleporterInteraction_Start(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction self)
        {
            self.baseShopSpawnChance = PortalChance.Value;
            if (!PortalScale.Value)
            {
                int shopCount = Run.instance.shopPortalCount;
                Run.instance.shopPortalCount = 0;
                orig(self);
                Run.instance.shopPortalCount = shopCount;
            }
            else
            {
                orig(self);
            }
        }

        public void PurchaseInteraction_ScaleCost(On.RoR2.PurchaseInteraction.orig_ScaleCost orig, PurchaseInteraction self, float scalar)
        {
            if (self.name.StartsWith("LunarRecycler")) { scalar = RerollScale.Value; }
            orig(self, scalar);
        }

        [Server]
        private void PurchaseInteraction_SetAvailable(On.RoR2.PurchaseInteraction.orig_SetAvailable orig, PurchaseInteraction self, bool newAvailable)
        {
            if(self.name.StartsWith("LunarRecycler")){
                if (RerollAmount.Value < 1 || RerollAmount.Value > numTimesRerolled) { orig(self, newAvailable); }
                else { orig(self, false); }
                numTimesRerolled++;
            } else { orig(self, newAvailable); }
        }

        [Server]
        private void ShopTerminalBehavior_GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior self)
        {
            if (ShopRefresh.Value && self.name.StartsWith("LunarShop")) { self.NetworkhasBeenPurchased = false; }
            orig(self);
            if (ShopRefresh.Value && self.name.StartsWith("LunarShop")) { self.GetComponent<PurchaseInteraction>().SetAvailable(true); }
        }

        private void CoinDropHook(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchDup(),
                x => x.MatchLdfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier"),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul()
                );
            c.Index += 2;
            c.Next.Operand = DropMulti.Value;
        }

        public IEnumerator DelayedAutomaticCoinRemovalLocal()
        {
            yield return new WaitForSeconds(2f);

            foreach (var n in NetworkUser.readOnlyLocalPlayersList)
            {
                if (n.localUser.userProfile.coins > 0)
                {
                    string coinRemovalMessage = $"<color=#beeca1>{n.localUser.userProfile.name}'s</color> <nobr><color=#C6ADFA><sprite name=\"LunarCoin\" tint=1>{n.localUser.userProfile.coins}</color></nobr> vanished into the aether...";
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = coinRemovalMessage
                    });

                    n.localUser.userProfile.coins = 0;
                    n.CallCmdSetNetLunarCoins(n.localUser.userProfile.coins);
                }
                if (StartingCoins.Value > 0)
                {
                    n.localUser.userProfile.coins = (uint)StartingCoins.Value;
                    n.CallCmdSetNetLunarCoins(n.localUser.userProfile.coins);
                }
            }
        }

        public IEnumerator DelayedAutomaticCoinRemoval()
        {
            yield return new WaitForSeconds(4f);
            for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
            {
                if (PlayerCharacterMasterController.instances[i].networkUser.lunarCoins > 0)
                {
                    string coinRemovalMessage = $"<color=#beeca1>{PlayerCharacterMasterController.instances[i].networkUser.userName}'s</color> <nobr><color=#C6ADFA><sprite name=\"LunarCoin\" tint=1>{PlayerCharacterMasterController.instances[i].networkUser.lunarCoins}</color></nobr> vanished into the aether...";
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = coinRemovalMessage
                    });

                    PlayerCharacterMasterController.instances[i].networkUser.DeductLunarCoins(PlayerCharacterMasterController.instances[i].networkUser.lunarCoins);
                }
                if (StartingCoins.Value > 0)
                {
                    PlayerCharacterMasterController.instances[i].networkUser.AwardLunarCoins((uint)StartingCoins.Value);
                }
            }
        }

        private IEnumerator DelayedLunarPriceChange()
        {
            yield return new WaitForSeconds(2f);
            var purchaseInteractions = InstanceTracker.GetInstancesList<PurchaseInteraction>();
            //SceneDef mostRecentSceneDef = SceneCatalog.mostRecentSceneDef;
            foreach (PurchaseInteraction purchaseInteraction in purchaseInteractions)
            {
                if (purchaseInteraction.name.StartsWith("LunarChest") && !SceneInfo.instance.sceneDef.baseSceneName.StartsWith("bazaar"))
                {
                    purchaseInteraction.Networkcost = PodCost.Value;
                    if (purchaseInteraction.Networkcost == 0) { purchaseInteraction.costType = CostTypeIndex.None; }
                }
                else if (purchaseInteraction.name.StartsWith("LunarShop"))
                {
                    purchaseInteraction.Networkcost = ShopCost.Value;
                    if (purchaseInteraction.Networkcost == 0) { purchaseInteraction.costType = CostTypeIndex.None; }
                }
                else if (purchaseInteraction.name.StartsWith("LunarRecycler"))
                {
                    purchaseInteraction.Networkcost = RerollCost.Value;
                    if (purchaseInteraction.Networkcost == 0) { purchaseInteraction.costType = CostTypeIndex.None; }
                }
            }
        }
    }
}
