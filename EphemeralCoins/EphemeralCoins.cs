using BepInEx;
using R2API.Utils;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EphemeralCoins
{
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Varna.EphemeralCoins", "Ephemeral_Coins", "2.2.0")]
    public class EphemeralCoins : BaseUnityPlugin
    {
        public int numTimesRerolled;

        public class CoinStorage
        {
            public NetworkUser user;
            public uint ephemeralCoinCount;
        }
        public List<CoinStorage> coinCounts = new List<CoinStorage>();

        public bool artifactEnabled {
            get
            {
                return BepConfig.EnableArtifact.Value == 2f || RunArtifactManager.instance.IsArtifactEnabled(Assets.NewMoonArtifact);
            }
        }
        
        //public int ephemeralCoinCount;

        public static PluginInfo PInfo { get; private set; }
        public static EphemeralCoins instance;

        public void Awake() //might have to change to Start()?
        {
            PInfo = Info;
            instance = this;

            //internal counters
            numTimesRerolled = 0;
            //ephemeralCoinCount = 0;

            //cost override
            RoR2Application.onLoad += AddCostType;

            Assets.Init();
            BepConfig.Init();
            Hooks.Init();

            //Utterly broken, fix later
            //if (ProperSaveCompatibility.enabled) ProperSaveSetup();
        }

        ///
        /// Based on the PlayerStorage system used in https://github.com/WondaMegapon/Refightilization/blob/master/Refightilization/Refightilization.cs
        ///
        public void SetupCoinStorage(List<CoinStorage> coinStorage, bool NewRun = true)
        {
            if (NewRun) coinStorage.Clear();
            foreach (PlayerCharacterMasterController playerCharacterMaster in PlayerCharacterMasterController.instances)
            {
                // Skipping over Disconnected Players.
                if (coinStorage != null && playerCharacterMaster.networkUser == null)
                {
                    Logger.LogInfo("A player disconnected! Skipping over what remains of them...");
                    continue;
                }

                // If this is ran mid-stage, just skip over existing players and add anybody who joined.
                if (!NewRun && coinStorage != null)
                {
                    // Skipping over players that are already in the game.
                    bool flag = false;
                    foreach (CoinStorage player in coinStorage)
                    {
                        if (player.user == playerCharacterMaster.networkUser)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag) continue;
                }
                CoinStorage newPlayer = new CoinStorage();
                if (playerCharacterMaster.networkUser) newPlayer.user = playerCharacterMaster.networkUser;
                newPlayer.ephemeralCoinCount = 0;
                coinStorage.Add(newPlayer);
                Logger.LogInfo(newPlayer.user.userName + " added to CoinStorage!");
            }
            Logger.LogInfo("Setting up CoinStorage finished.");
        }

        public void giveCoinsToUser(NetworkUser user, uint count)
        {
            foreach (CoinStorage player in coinCounts)
            {
                if (player.user == user)
                {
                    player.ephemeralCoinCount += count;
                    Logger.LogInfo("giveCoinsToUser: " + user.userName + " " + count);
                }
            }
        }

        public void takeCoinsFromUser(NetworkUser user, uint count)
        {
            foreach (CoinStorage player in coinCounts)
            {
                if (player.user == user)
                {
                    player.ephemeralCoinCount -= count;
                    Logger.LogInfo("takeCoinsFromUser: " + user.userName + " " + count);
                }
            }
        }

        public uint getCoinsFromUser(NetworkUser user)
        {
            foreach (CoinStorage player in coinCounts)
            {
                if (player.user == user)
                {
                    //Spams the console due to HUD hook, only used for debugging.
                    //Logger.LogInfo("getCoinsFromUser: " + user.userName + player.ephemeralCoinCount);
                    return player.ephemeralCoinCount;
                }
            }
            return 0;
        }

        ///
        /// Override the CostType delegates so that we can use a different coin count check when the artifact is active. Hacky, but works.
        /// 
        public void AddCostType()
        {
            CostTypeDef newdef = new CostTypeDef
            {
                costStringFormatToken = "COST_LUNARCOIN_FORMAT",
                saturateWorldStyledCostString = false,
                darkenWorldStyledCostString = true,
                isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
                {
                    NetworkUser networkUser2 = Util.LookUpBodyNetworkUser(context.activator.gameObject);
                    if (artifactEnabled) {
                        foreach (CoinStorage player in coinCounts)
                        {
                            if (player.user == networkUser2)
                            {
                                return player.ephemeralCoinCount >= context.cost;
                            }
                        }
                    }
                    return (bool)networkUser2 && networkUser2.lunarCoins >= context.cost;
                },
                payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
                {
                    NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
                    if ((bool)networkUser)
                    {
                        networkUser.DeductLunarCoins((uint)context.cost);
                        RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase(context);
                    }
                },
                colorIndex = ColorCatalog.ColorIndex.LunarCoin
            };

            CostTypeCatalog.Register(CostTypeIndex.LunarCoin, newdef);
        }

        public void RunStartPrefabSetup(bool set = false)
        {
            ///
            /// Swap the Lunar Coin's model and pickup settings around based on whether the artifact is enabled.
            ///
            //Text stuff
            PickupDef TheCoinDef = PickupCatalog.FindPickupIndex("LunarCoin.Coin0").pickupDef;
            TheCoinDef.nameToken = set ? "Ephemeral Coin" : "PICKUP_LUNAR_COIN";
            TheCoinDef.interactContextToken = set ? "Pick up Ephemeral Coin" : "LUNAR_COIN_PICKUP_CONTEXT";

            //Outline color
            TheCoinDef.baseColor = set ? new Color32(96, 254, byte.MaxValue, byte.MaxValue) : new Color32(48, 127, byte.MaxValue, byte.MaxValue);

            //Chatbox color
            TheCoinDef.darkColor = set ? new Color32(152, 168, byte.MaxValue, byte.MaxValue) : new Color32(76, 84, 144, byte.MaxValue);

            //Filling our Lunar Coin's hole.
            GameObject TheCoin = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarCoin/PickupLunarCoin.prefab").WaitForCompletion();
            TheCoin.transform.Find("Coin5Mesh").GetComponent<MeshFilter>().mesh = set ?
                Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/mdlLunarCoin.fbx").WaitForCompletion().GetComponent<MeshFilter>().mesh
                :
                Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/mdlLunarCoinWithHole.fbx").WaitForCompletion().GetComponent<MeshFilter>().mesh;

            //Changing the material used for rendering, so we can have a semi-transparent effect. Hopoo's standard shader doesn't do transparency I guess???
            TheCoin.transform.Find("Coin5Mesh").GetComponent<MeshRenderer>().material = set ? Assets.mainBundle.LoadAsset<Material>("matEphemeralCoin") : Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLunarCoinPlaceholder.mat").WaitForCompletion();

            /// Model tint
            /// Swapping the material directly (above) instead of modifying it here.
            //Material TheCoinMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLunarCoinPlaceholder.mat").WaitForCompletion();
            //TheCoinMaterial.SetColor("_Color", set ? new Color32(100, 220, 250, byte.MaxValue) : new Color32(198, 173, 250, byte.MaxValue));
            //TheCoinMaterial.SetTexture("_MainTex", set ? Assets.mainBundle.LoadAsset<Texture2D>("texEphemeralCoinDiffuse") : Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/VFX/texLunarCoinDiffuse.png").WaitForCompletion());

            /// Transparency
            /// Can't actually touch the textures in memory because of RoR2's unity import settings.
            /// Solved by changing the texture instead. Kept this snippet for educational purposes.
            /*Texture2D TheCoinDiffuse = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/VFX/texLunarCoinDiffuse.png").WaitForCompletion();
            Color[] d = TheCoinDiffuse.GetPixels();
            foreach (Color p in d)
            {
                p.SetFieldValue("a", set ? 0.5f : 1f);
            }
            TheCoinDiffuse.SetPixels(d);
            */

            ///
            /// Changing the interactable costs.
            ///
            /// Only the LunarChest (pods) and FrogInteractable (moonfrog) actually do anything here, because Bazaar is full of pre-loaded instances of the prefabs.
            /// Still change the other prefabs anyway, just in case Hopoo decides to change something down the line.
            /// The Seer Stations will always require hooks, because their scripts set their price at runtime. Why? Ask Hopoo.
            foreach (string x in Assets.lunarInteractables)
            {
                GameObject z = Addressables.LoadAssetAsync<GameObject>(x).WaitForCompletion();
                int zValue = 0;

                switch (z.name)
                {
                    case "LunarRecycler":
                        zValue = (int)BepConfig.RerollCost.Value;
                        //Debug.Log("EphemeralCoins PrefabSetup LunarRecycler " + zValue);
                        break;
                    case "LunarChest":
                        zValue = (int)BepConfig.PodCost.Value;
                        //Debug.Log("EphemeralCoins PrefabSetup LunarChest " + zValue);
                        break;
                    case "LunarShopTerminal":
                        zValue = (int)BepConfig.ShopCost.Value;
                        //Debug.Log("EphemeralCoins PrefabSetup LunarShopTerminal " + zValue);
                        break;
                    case "SeerStation":
                        zValue = (int)BepConfig.SeerCost.Value;
                        //Debug.Log("EphemeralCoins PrefabSetup SeerStation " + zValue);
                        break;
                    case "FrogInteractable":
                        zValue = (int)BepConfig.FrogCost.Value;
                        z.GetComponent<FrogController>().maxPets = (int)BepConfig.FrogPets.Value;
                        //Debug.Log("EphemeralCoins PrefabSetup FrogInteractable " + zValue);
                        break;
                    default:
                        Debug.LogWarning("EphemeralCoins: Unknown lunarInteractable " + x + ", will default to 0 cost!");
                        break;
                }

                z.GetComponent<PurchaseInteraction>().Networkcost = zValue;
                if (zValue == 0) { z.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None; }
            }
        }

        /*
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ProperSaveSetup()
        {
            ProperSave.SaveFile.OnGatherSaveData += (dict) =>
            {
                if (dict.ContainsKey("ephemeralCoinCount"))
                    dict["ephemeralCoinCount"] = coinCounts;
                else
                    dict.Add("ephemeralCoinCount", coinCounts);
            };

            ProperSave.Loading.OnLoadingEnded += (save) =>
            {
                coinCounts = save.GetModdedData<List<CoinStorage>>("ephemeralCoinCount");
            };
        }
        */

        ///
        /// Required for BTB to change the costs of the pre-loaded prefab instances.
        /// 
        public IEnumerator DelayedLunarPriceChange()
        {
            yield return new WaitForSeconds(2f);
            var purchaseInteractions = InstanceTracker.GetInstancesList<PurchaseInteraction>();
            foreach (PurchaseInteraction purchaseInteraction in purchaseInteractions)
            {
                if (purchaseInteraction.name.StartsWith("LunarShop"))
                {
                    purchaseInteraction.Networkcost = (int)BepConfig.ShopCost.Value;
                    if (BepConfig.ShopCost.Value == 0) { purchaseInteraction.costType = CostTypeIndex.None; }
                }
                else if (purchaseInteraction.name.StartsWith("LunarRecycler"))
                {
                    purchaseInteraction.Networkcost = (int)BepConfig.RerollCost.Value;
                    if (BepConfig.RerollCost.Value == 0) { purchaseInteraction.costType = CostTypeIndex.None; }
                }
            }
        }

        ///
        /// Our opening message + starting coins functionality, packed in a coroutine so that it can run alongside Run.Start().
        ///
        public IEnumerator DelayedStartingLunarCoins()
        {
            yield return new WaitForSeconds(1f);
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = "<color=#beeca1><size=15px>A new moon rises...</size></color>" });
            yield return new WaitForSeconds(3f);
            if (BepConfig.StartingCoins.Value > 0)
            {
                for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "<nobr><color=#adf2fa><sprite name=\"LunarCoin\" tint=1>" + BepConfig.StartingCoins.Value + "</color></nobr> " + (BepConfig.StartingCoins.Value > 1 ? "coins fade" : "coin fades") + " into existence..."
                    });
                    PlayerCharacterMasterController.instances[i].networkUser.AwardLunarCoins((uint)BepConfig.StartingCoins.Value);
                }
            }
        }
    }
}
