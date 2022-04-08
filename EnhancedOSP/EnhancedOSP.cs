using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using System;
using UnityEngine;
using RoR2;

namespace EnhancedOSP
{
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.3.1")]
    public class EnhancedOSP : BaseUnityPlugin
    {
        private static ConfigEntry<float> invTime;
        private static ConfigEntry<float> invThreshold;
        private static ConfigEntry<bool> curseAffectsOSP;
        private static ConfigEntry<bool> shieldAffectsOSP;
        private static ConfigEntry<bool> removeCurseDisplay;

        public void Awake()
        {
            invTime = Config.Bind("", "Invulnerable Time", 0.5f, new ConfigDescription("The amount of time a player remains invulnerable after one shot protection is triggered. Vanilla is 0.1"));
            invThreshold = Config.Bind("", "OSP Threshold", 0.1f, new ConfigDescription("How much missing hp% before OSP is disabled. Vanilla is 0.1"));
            curseAffectsOSP = Config.Bind("", "Curse Behavior", false, new ConfigDescription("Whether or not sources of MaxHP reduction (Shaped Glass, etc) remove OSP. Vanilla is true"));
            shieldAffectsOSP = Config.Bind("", "Shield Behavior", false, new ConfigDescription("Whether or not sources of Shield (Personal Shield Generator, Overloading affix, etc) count toward your maximum HP for OSP calculations. Vanilla is true [Note: Trancendence and Perfected Elite Affix behavior unaffected to avoid godmode issues]"));
            removeCurseDisplay = Config.Bind("", "Curse Healthbar Display", false, new ConfigDescription("Whether or not sources of MaxHP reduction (Shaped Glass, etc) are represented on the HUD via a pointless 'glass' effect that takes up space and makes the bar harder to read during gameplay. Vanilla is true"));

            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.HealthComponent.TriggerOneShotProtection += HealthComponent_TriggerOneShotProtection;
            On.RoR2.HealthComponent.GetHealthBarValues += HealthComponent_GetHealthBarValues;

            IL.RoR2.HealthComponent.TakeDamage += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<RoR2.HealthComponent>("get_fullCombinedHealth"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<RoR2.HealthComponent>("barrier")
                    );
                c.Index++;
                c.Remove();
                c.EmitDelegate<Func<RoR2.HealthComponent, float>>((hc) => { 
                    return (shieldAffectsOSP.Value || hc.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || hc.body.HasBuff(RoR2Content.Buffs.AffixLunar)) 
                    ? 
                    hc.fullCombinedHealth 
                    : 
                    hc.fullHealth;
                });
            };
            Logger.LogDebug("Shield behavior IL patched");

            /*
            IL.RoR2.HealthComponent.GetHealthBarValues += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.Match(OpCodes.Ret)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<RoR2.HealthComponent.HealthBarValues, RoR2.HealthComponent, RoR2.HealthComponent.HealthBarValues>>((values, hc) => {
                    values.curseFraction = 1f - 1f / (removeCurseDisplay.Value ? hc.body.cursePenalty : 1f);

                    var num2 = (1f - values.curseFraction) / hc.fullCombinedHealth;

                    values.cullFraction = ((hc.isInFrozenState && (hc.body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) == 0) ? Mathf.Clamp01(0.3f * hc.fullCombinedHealth * num2) : 0f);
                    values.healthFraction = Mathf.Clamp01(hc.health * num2);
                    values.shieldFraction = Mathf.Clamp01(hc.shield * num2);
                    values.barrierFraction = Mathf.Clamp01(hc.barrier * num2);
                    values.magneticFraction = Mathf.Clamp01(hc.magnetiCharge * num2);

                    values.ospFraction = (shieldAffectsOSP.Value || hc.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || hc.body.HasBuff(RoR2Content.Buffs.AffixLunar))
                    ?
                    (hc.body.oneShotProtectionFraction * hc.fullCombinedHealth - hc.missingCombinedHealth) * num2
                    :
                    (hc.body.oneShotProtectionFraction * hc.fullHealth - (hc.fullHealth - hc.health)) * num2;

                    return values;
                });
                Logger.LogDebug("HealthBar display patched");
            };
            */
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            self.oneShotProtectionFraction = Mathf.Max(0f, invThreshold.Value - (1f - 1f / (curseAffectsOSP.Value ? self.cursePenalty : 1f)));
        }

        private void HealthComponent_TriggerOneShotProtection(On.RoR2.HealthComponent.orig_TriggerOneShotProtection orig, HealthComponent self)
        {
            orig(self);
            self.ospTimer = invTime.Value;
        }

        private HealthComponent.HealthBarValues HealthComponent_GetHealthBarValues(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.curseFraction = 1f - 1f / (removeCurseDisplay.Value ? self.body.cursePenalty : 1f);

            var num2 = (1f - values.curseFraction) / self.fullCombinedHealth;

            values.cullFraction = ((self.isInFrozenState && (self.body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) == 0) ? Mathf.Clamp01(0.3f * self.fullCombinedHealth * num2) : 0f);
            values.healthFraction = Mathf.Clamp01(self.health * num2);
            values.shieldFraction = Mathf.Clamp01(self.shield * num2);
            values.barrierFraction = Mathf.Clamp01(self.barrier * num2);
            values.magneticFraction = Mathf.Clamp01(self.magnetiCharge * num2);

            values.ospFraction = (shieldAffectsOSP.Value || self.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || self.body.HasBuff(RoR2Content.Buffs.AffixLunar))
            ?
            (self.body.oneShotProtectionFraction * self.fullCombinedHealth - self.missingCombinedHealth) * num2
            :
            (self.body.oneShotProtectionFraction * self.fullHealth - (self.fullHealth - self.health)) * num2;

            return values;
        }
    }
}