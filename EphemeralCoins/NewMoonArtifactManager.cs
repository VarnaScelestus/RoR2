using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EphemeralCoins
{
	public static class NewMoonArtifactManager
	{
		private static ArtifactDef myArtifact => Assets.NewMoonArtifact;

		[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
		private static void Init()
		{
            RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
            RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
        }

		private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
		{
			if (!(artifactDef != myArtifact))
			{
                On.RoR2.Run.Start += Run_Start;
			}
		}

        private static void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            EphemeralCoins.Logger.LogDebug("Artifact enabled");
            PrefabSetup(true);
            orig(self);
        }

        private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
		{
			if (!(artifactDef != myArtifact))
			{
                On.RoR2.Run.Start -= Run_Start;
                EphemeralCoins.Logger.LogDebug("Artifact disabled");
                PrefabSetup(false);
            }
		}

        public static void PrefabSetup(bool set = false)
        {
            EphemeralCoins.Logger.LogDebug("PrefabSetup " + set);

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
                        break;
                    case "LunarChest":
                        zValue = (int)BepConfig.PodCost.Value;
                        break;
                    case "LunarShopTerminal":
                        zValue = (int)BepConfig.ShopCost.Value;
                        break;
                    case "SeerStation":
                        zValue = (int)BepConfig.SeerCost.Value;
                        break;
                    case "FrogInteractable":
                        zValue = (int)BepConfig.FrogCost.Value;
                        z.GetComponent<FrogController>().maxPets = (int)BepConfig.FrogPets.Value;
                        break;
                    default:
                        EphemeralCoins.Logger.LogWarning("Unknown lunarInteractable " + x + ", will default to 0 cost!");
                        break;
                }

                z.GetComponent<PurchaseInteraction>().Networkcost = zValue;
                if (zValue == 0) { z.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None; }
            }
        }
    }
}