using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EphemeralCoins
{
	public static class Assets
	{
		public static AssetBundle mainBundle;

		public const string bundleName = "ephemeralcoins";

		public const string assetBundleFolder = "assetbundles";

		public static ArtifactDef NewMoonArtifact;

        internal static string[] lunarInteractables = {
            "RoR2/Base/LunarRecycler/LunarRecycler.prefab",
            "RoR2/Base/LunarChest/LunarChest.prefab",
            "RoR2/Base/LunarShopTerminal/LunarShopTerminal.prefab",
            "RoR2/Base/bazaar/SeerStation.prefab",
            "RoR2/Base/moon/FrogInteractable.prefab"
        };

        public static string AssetBundlePath
		{
			get
			{
				return System.IO.Path.Combine(EphemeralCoins.PInfo.Location.Substring(0, EphemeralCoins.PInfo.Location.Length - 18), assetBundleFolder, bundleName);
			}
		}

		public static void Init()
		{
			mainBundle = AssetBundle.LoadFromFile(AssetBundlePath);

			NewMoonArtifact = ScriptableObject.CreateInstance<ArtifactDef>();
			NewMoonArtifact.nameToken = "Artifact of the New Moon";
			NewMoonArtifact.descriptionToken = "Lunar Coins become Ephemeral Coins, a temporary per-run currency. <size=70%>(Your saved Lunar Coin count is unaffected.)</size>";
			NewMoonArtifact.smallIconSelectedSprite = mainBundle.LoadAsset<Sprite>("texArtifactNewMoonEnabled");
			NewMoonArtifact.smallIconDeselectedSprite = mainBundle.LoadAsset<Sprite>("texArtifactNewMoonDisabled");
		}
    }
}
