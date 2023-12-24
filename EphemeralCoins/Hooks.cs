﻿using BepInEx;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Reflection;
using UnityEngine.Networking;

namespace EphemeralCoins
{
    public class Hooks
    {
        public static void Init()
        {
            //main setup hook; this is where most of the mod's settings are applied
            On.RoR2.Run.Start += Run_Start;

            //Coin counter
            On.RoR2.UI.HUD.Update += HUD_Update;
            On.RoR2.Run.OnUserAdded += Run_OnUserAdded;
            //On.RoR2.Run.OnUserRemoved += Run_OnUserRemoved;

            //Cost functions
            On.RoR2.NetworkUser.RpcAwardLunarCoins += NetworkUser_RpcAwardLunarCoins;
            On.RoR2.NetworkUser.RpcDeductLunarCoins += NetworkUser_RpcDeductLunarCoins;
            On.RoR2.NetworkUser.SyncLunarCoinsToServer += NetworkUser_SyncLunarCoinsToServer;

            //Coin base drop chance
            On.RoR2.PlayerCharacterMasterController.Awake += PlayerCharacterMasterController_Awake;

            //Coin drop multiplier
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            //Blue Orb on stage start chance reduction
            On.RoR2.TeleporterInteraction.Start += TeleporterInteraction_Start;

            //Bazaar is awful and requires a million hooks to do simple things
            //It has pre-loaded instances of the shop pods, seer stations, and recycler, so changing the prefabs does nothing.
            //Thus, we are forced to hook into their behaviors instead.
            On.RoR2.SceneDirector.Start += SceneDirector_Start;
            On.RoR2.PurchaseInteraction.SetAvailable += PurchaseInteraction_SetAvailable;
            On.RoR2.PurchaseInteraction.ScaleCost += PurchaseInteraction_ScaleCost;
            On.RoR2.ShopTerminalBehavior.GenerateNewPickupServer += ShopTerminalBehavior_GenerateNewPickupServer;
            On.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        }

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);

            //Start message + starting coins functionality
            if (NetworkServer.active && EphemeralCoins.instance.artifactEnabled)
            {
                bool check;
                if (ProperSaveCompatibility.enabled) { check = ProperSaveCompatibility.IsRunNew(); }
                else { check = Run.instance.stageClearCount == 0 ? true : false; }
                if (check)
                {
                    EphemeralCoins.instance.SetupCoinStorage(EphemeralCoins.instance.coinCounts);
                    EphemeralCoins.instance.StartCoroutine(EphemeralCoins.instance.DelayedStartingLunarCoins());
                }
            }
        }

        #region EphemeralCoin
        private static void HUD_Update(On.RoR2.UI.HUD.orig_Update orig, RoR2.UI.HUD self)
        {
            orig(self);
            if (EphemeralCoins.instance.artifactEnabled)
            {
                self.lunarCoinText.targetValue = (int)EphemeralCoins.instance.getCoinsFromUser(self._localUserViewer.currentNetworkUser);
                if (self.lunarCoinContainer.transform.Find("LunarCoinSign") != null) self.lunarCoinContainer.transform.Find("LunarCoinSign").GetComponent<RoR2.UI.HGTextMeshProUGUI>().text = "<sprite name=\"LunarCoin\" color=#adf2fa>";
            }
        }

        private static void Run_OnUserAdded(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {
            orig(self, user);
            if (NetworkServer.active && Run.instance.time > 1f)
                EphemeralCoins.instance.SetupCoinStorage(EphemeralCoins.instance.coinCounts, false);
        }

        private static void NetworkUser_RpcAwardLunarCoins(On.RoR2.NetworkUser.orig_RpcAwardLunarCoins orig, RoR2.NetworkUser self, uint count)
        {
            if (EphemeralCoins.instance.artifactEnabled)
            {
                orig(self, 0);
                EphemeralCoins.instance.giveCoinsToUser(self, count);
                self.SyncLunarCoinsToServer();
            }
            else orig(self, count);
        }

        private static void NetworkUser_RpcDeductLunarCoins(On.RoR2.NetworkUser.orig_RpcDeductLunarCoins orig, RoR2.NetworkUser self, uint count)
        {
            if (EphemeralCoins.instance.artifactEnabled)
            {
                orig(self, 0);
                EphemeralCoins.instance.takeCoinsFromUser(self, count);
                self.SyncLunarCoinsToServer();
            }
            else orig(self, count);
        }

        private static void NetworkUser_SyncLunarCoinsToServer(On.RoR2.NetworkUser.orig_SyncLunarCoinsToServer orig, RoR2.NetworkUser self)
        {
            orig(self);
            if (EphemeralCoins.instance.artifactEnabled)
            {
                self.CallCmdSetNetLunarCoins((uint)EphemeralCoins.instance.getCoinsFromUser(self));
            }
        }
        #endregion

        #region CoinDrop
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
            if (BepConfig.DropMulti.Value > 1)
            {    
                c.EmitDelegate<Func<float, float>>((originalChance) =>
                {
                    return Math.Min(originalChance, BepConfig.DropMax.Value);
                });
            }
            else
            {    
                c.EmitDelegate<Func<float, float>>((originalChance) =>
                {
                    return Math.Max(originalChance, BepConfig.DropMin.Value);
                });
            }
        }
        #endregion

        #region BlueOrb
        private static void TeleporterInteraction_Start(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction self)
        {
            self.baseShopSpawnChance = BepConfig.PortalChance.Value;
            if (!BepConfig.PortalScale.Value)
            {
                int shopCount = Run.instance.shopPortalCount;
                Run.instance.shopPortalCount = 0;
                orig(self);
                Run.instance.shopPortalCount = shopCount;
            }
            else { orig(self); }
        }
        #endregion

        #region BazaarOfBullshit
        private static void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                EphemeralCoins.instance.StartCoroutine(EphemeralCoins.instance.DelayedLunarPriceChange());
                EphemeralCoins.instance.numTimesRerolled = 0;
            }
        }

        private static void BazaarController_SetUpSeerStations(On.RoR2.BazaarController.orig_SetUpSeerStations orig, BazaarController self)
        {
            orig(self);
            foreach (SeerStationController seerStationController in self.seerStations)
            {
                seerStationController.GetComponent<PurchaseInteraction>().Networkcost = (int)BepConfig.SeerCost.Value;
                if (BepConfig.SeerCost.Value == 0) { seerStationController.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None; }
            }
        }

        public static void PurchaseInteraction_ScaleCost(On.RoR2.PurchaseInteraction.orig_ScaleCost orig, PurchaseInteraction self, float scalar)
        {
            if (self.name.StartsWith("LunarRecycler")) { scalar = BepConfig.RerollScale.Value; }
            orig(self, scalar);
        }

        [Server]
        private static void PurchaseInteraction_SetAvailable(On.RoR2.PurchaseInteraction.orig_SetAvailable orig, PurchaseInteraction self, bool newAvailable)
        {
            if (self.name.StartsWith("LunarRecycler"))
            {
                if (BepConfig.RerollAmount.Value < 1 || BepConfig.RerollAmount.Value > EphemeralCoins.instance.numTimesRerolled) { orig(self, newAvailable); }
                else { orig(self, false); }
                EphemeralCoins.instance.numTimesRerolled++;
            }
            else { orig(self, newAvailable); }
        }

        [Server]
        private static void ShopTerminalBehavior_GenerateNewPickupServer(On.RoR2.ShopTerminalBehavior.orig_GenerateNewPickupServer orig, ShopTerminalBehavior self)
        {
            if (BepConfig.ShopRefresh.Value && self.name.StartsWith("LunarShop")) { self.NetworkhasBeenPurchased = false; }
            orig(self);
            if (BepConfig.ShopRefresh.Value && self.name.StartsWith("LunarShop")) { self.GetComponent<PurchaseInteraction>().SetAvailable(true); }
        }
        #endregion

    }
}
