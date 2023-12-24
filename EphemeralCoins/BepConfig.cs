﻿using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using R2API;

namespace EphemeralCoins
{
    public class BepConfig
    {
        public static ConfigEntry<float> EnableArtifact;
        public static ConfigEntry<float> StartingCoins;

        public static ConfigEntry<float> DropChance;
        public static ConfigEntry<float> DropMulti;
        public static ConfigEntry<float> DropMin;
        public static ConfigEntry<float> DropMax;
        public static ConfigEntry<float> PodCost;
        public static ConfigEntry<float> PortalChance;
        public static ConfigEntry<bool> PortalScale;
        public static ConfigEntry<float> FrogCost;
        public static ConfigEntry<float> FrogPets;

        public static ConfigEntry<float> ShopCost;
        public static ConfigEntry<bool> ShopRefresh;
        public static ConfigEntry<float> SeerCost;
        public static ConfigEntry<float> RerollCost;
        public static ConfigEntry<float> RerollAmount;
        public static ConfigEntry<float> RerollScale;

        public static void Init()
        {
            EnableArtifact = EphemeralCoins.instance.Config.Bind("1. Artifact", "Run Mode", 1f, new ConfigDescription("If set to 0, disables the artifact. If set to 1, enables the artifact. If set to 2, disables the artifact but enables the artifact's effects at all times."));

            StartingCoins = EphemeralCoins.instance.Config.Bind("1. Artifact", "Starting Coins", 0f, new ConfigDescription("The number of coins each player starts with when using the artifact."));

            DropChance = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Drop Chance", 5.0f, new ConfigDescription("The initial %chance for enemies to drop coins. Vanilla is 0.5%"));

            DropMulti = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Drop Multiplier", 0.90f, new ConfigDescription("The multiplier applied to the drop chance after a coin has dropped. Vanilla is 0.5 (50%)"));

            DropMin = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Drop Min Chance", 0.5f, new ConfigDescription("The lowest %chance for enemies to drop coins after DropMulti is applied. Vanilla has no lower limit"));

            DropMax = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Drop Max Chance", 5.0f, new ConfigDescription("The highest %chance for enemies to drop coins after DropMulti is applied. Use if DropMulti > 1"));

            PodCost = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Pod Cost", 0f, new ConfigDescription("The cost of Lunar Pods. Vanilla is 1"));

            PortalChance = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Portal Chance", 0.375f, new ConfigDescription("The chance of a Blue Orb appearing on stage start. Vanilla is 0.375 (37.5%)"));

            PortalScale = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Portal Scale", false, new ConfigDescription("Scale down the chance of a Blue Orb appearing for each time BTB has been visited? Vanilla behavior is true"));

            FrogCost = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Frog Cost", 0f, new ConfigDescription("The cost of the Frog. Vanilla is 1"));

            FrogPets = EphemeralCoins.instance.Config.Bind("2. Enemy and Stage", "Frog Pets", 1f, new ConfigDescription("The number of times you have to interact with the Frog before something happens. Vanilla is 10"));

            ShopCost = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Shop Cost", 1f, new ConfigDescription("The cost of Lunar Buds in BTB. Vanilla is 2"));

            ShopRefresh = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Shop Refresh", true, new ConfigDescription("Do empty Lunar Buds in BTB refresh when the Slab (reroller) is used? Vanilla is false"));

            SeerCost = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Seer Cost", 1f, new ConfigDescription("The cost of Lunar Seers in BTB. Vanilla is 3"));

            RerollCost = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Reroll Cost", 0f, new ConfigDescription("The initial cost of the Slab (reroller) in BTB. Vanilla is 1"));

            RerollAmount = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Reroll Amount", 1f, new ConfigDescription("How many times can the Slab (reroller) in BTB be used? Enter 0 for infinite (vanilla)."));

            RerollScale = EphemeralCoins.instance.Config.Bind("3. Bazaar Between Time", "Reroll Scale", 2f, new ConfigDescription("The cost multiplier per use of the Slab (reroller) in BTB. Vanilla is 2"));

            if (BepConfig.EnableArtifact.Value == 1f) ContentAddition.AddArtifactDef(Assets.NewMoonArtifact);

            if ( RiskOfOptionsCompatibility.enabled ) {

                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(EnableArtifact, 0, 2, 1f, false, true);

                /* Event to dynamically add/remove the artifact from the selectable pool. Needs more research
                EnableArtifact.SettingChanged += (object sender, EventArgs e) => {
                    switch (EnableArtifact.Value)
                    {
                        case 0f:
                        case 2f:
                            //remove the artifact
                            break;
                        case 1f:
                            //add the artifact
                            break;
                    }
                };
                */

                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(StartingCoins, 0, 100, 1f, true);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropChance, 0, 100, 0.5f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropMulti, 0, 2, 0.01f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropMin, 0, 100, 0.5f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(DropMax, 0, 100, 0.5f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(PodCost, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(PortalChance, 0, 1, 0.01f);
                RiskOfOptionsCompatibility.InvokeAddOptionCheckBox(PortalScale);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(FrogCost, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(FrogPets, 0, 20, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(ShopCost, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionCheckBox(ShopRefresh);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(SeerCost, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(RerollCost, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(RerollAmount, 0, 10, 1f);
                RiskOfOptionsCompatibility.InvokeAddOptionStepSlider(RerollScale, 0, 10, 1f);

                RiskOfOptionsCompatibility.InvokeSetModIcon(Assets.mainBundle.LoadAsset<Sprite>("texArtifactNewMoonEnabled"));

                RiskOfOptionsCompatibility.InvokeSetModDescription(
                    "<size=200%><uppercase><align=center><color=#adf2fa>Ephemeral Coins</color></align></uppercase></size>"
                    + "\n<size=80%>Provides settings to control various aspects relating to Lunar Coins, including an Artifact that replaces them with temporary, per-run fascimiles that do not affect your save file's coin count. Almost all settings are independant and can be used with or without the Artifact.</size>"
                    + "\n\n<b><color=#CECE00>### WARNING ###\nSettings cannot be changed during a run.\nIn multiplayer, if your settings differ from the host, there may be errors (UNTESTED)!</color></b>"
                    //TODO: put this somewhere where it can update each time a setting is changed
                    //+ "\n\n<style=cSub>"
                    //+ (EnableArtifact.Value ? "< ! > <color=#adf2fa>Artifact of the New Moon</color> is enabled. " + (StartingCoins.Value > 0 && EnableArtifact.Value ? " Start with " + StartingCoins.Value + " coin(s).\n" : "\n") : "")
                    //+ "< ! > Coins have a " + DropChance.Value + "% drop chance (" + DropMulti.Value + "x change per drop, " + DropMin.Value + "% min chance, " + DropMax.Value + "% max chance)"
                    //+ "\n"
                    //+ "< ! > Blue Orbs have a " + PortalChance.Value * 100f + "% chance to appear on stage start" + (PortalScale.Value ? ", divided by the number of times this has occured before." : ".")
                    //+ "\n"
                    //+ "< ! > Lunar Pods cost " + PodCost.Value + "."
                    //+ "\n"
                    //+ "< ! > Lunar Shop Pods cost " + ShopCost.Value + (ShopRefresh.Value ? " and they refresh when rerolled. " : ". ")
                    //+ "\n"
                    //+ "< ! > Rerolling costs " + RerollCost.Value + (RerollAmount.Value > 0 ? " and can be done " + RerollAmount.Value + " time(s)" : "") + (RerollScale.Value > 1 ? ", costing " + RerollScale.Value + "x each time." : ".")
                    //+ "\n"
                    //+ "< ! > Seer Stations cost " + SeerCost.Value + "."
                    //+ "\n"
                    //+ "< ! > The Glass Frog costs " + FrogCost.Value + " per use, and you must do so " + FrogPets.Value + " time(s)."
                    );
            }
        }
    }
}
