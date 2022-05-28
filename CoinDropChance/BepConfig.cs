using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;

namespace CoinDropChance
{
    public class BepConfig
    {
        public static ConfigEntry<float> DropChance;
        public static ConfigEntry<float> DropMulti;
        public static ConfigEntry<float> DropMin;

        public static void Init()
        {
            DropChance = CoinDropChance.instance.Config.Bind("Lunar Coin Drop Chance", "Drop Chance", 1f, new ConfigDescription("The initial %chance for enemies to drop coins. Vanilla is 0.5%. Careful, this value is in % i.e. a value of 0.5 means a probability of 0.005"));
            DropMulti = CoinDropChance.instance.Config.Bind("Lunar Coin Drop Chance", "Drop Multiplier", 0.75f, new ConfigDescription("The multiplier applied to the drop chance after a coin has dropped. Vanilla is 0.5"));
            DropMin = CoinDropChance.instance.Config.Bind("Lunar Coin Drop Chance", "Drop Min Chance", 0.05f, new ConfigDescription("The lowest %chance for enemies to drop coins after DropMulti is applied. Vanilla has no lower limit. Careful, this value is in % i.e. a value of 0.5 means a probability of 0.005"));

            if ( RiskOfOptionsCompatibility.enabled ) {

                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropChance, 0, 10, 0.1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropMulti, 0, 1, 0.01f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropMin, 0, 1, 0.001f);

                RiskOfOptionsCompatibility.InvokeSetModDescription(
                    "<size=200%><uppercase><align=center><color=#adf2fa>Higher Coin Drop Chance</color></align></uppercase></size>"
                    + "\n<size=80%>Provides settings to control the drop chance of Lunar Coins.</size>"
                    + "\n\n<b><color=#CECE00>### WARNING ###\nSettings cannot be changed during a run.</color></b>"
                    );
            }
        }
    }
}
