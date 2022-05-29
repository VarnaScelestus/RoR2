using BepInEx;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoinDropChance
{
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("com.Varna.EphemeralCoins")]
    [BepInPlugin("com.Varna.CoinDropChance", "CoinDropChance", "1.0.0")]
    public class CoinDropChance : BaseUnityPlugin
    {        
        public static PluginInfo PInfo { get; private set; }
        public static CoinDropChance instance;

        public static new BepInEx.Logging.ManualLogSource Logger;

        public void Awake()
        {
            PInfo = Info;
            instance = this;
            Logger = base.Logger;

            BepConfig.Init();
            Hooks.Init();
        }
    }
}
