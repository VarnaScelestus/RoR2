using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using RoR2;

namespace EnhancedOSP
{
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.2.0")]
    public class EnhancedOSP : BaseUnityPlugin
    {
        private static ConfigEntry<float> invTime;
        private static ConfigEntry<float> invThreshold;
        private static ConfigEntry<bool> curseAffectsOSP;
        private static ConfigEntry<bool> shieldAffectsOSP;

        public void Awake()
        {
            invTime = Config.Bind("", "Invulnerable Time", 0.5f, new ConfigDescription("The amount of time a player remains invulnerable after one shot protection is triggered. Vanilla is 0.1"));
            IL.RoR2.HealthComponent.TriggerOneShotProtection += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(0.1f)
                    );
                c.Index++;
                c.Next.Operand = invTime.Value;
            };
            Logger.LogDebug("Invuln time successful");
            
            invThreshold = Config.Bind("", "OSP Threshold", 0.1f, new ConfigDescription("How much missing hp% before OSP is disabled. Vanilla is 0.1"));
            curseAffectsOSP = Config.Bind("", "Curse Behavior", false, new ConfigDescription("Whether or not sources of MaxHP reduction (Shaped Glass, etc) remove OSP. Vanilla is true"));
            IL.RoR2.CharacterBody.RecalculateStats += (il) =>
            {

                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(0.1f),
                    x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("set_oneShotProtectionFraction")
                    );
                c.Index++;
                c.Next.Operand = invThreshold.Value;
                Logger.LogDebug("OSP threshold successful");

                c.GotoNext(
                    x => x.MatchCallOrCallvirt<Mathf>("Max"),
                    x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("set_oneShotProtectionFraction")
                    );
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoR2.CharacterBody, float>>((origFrac, body) => { return curseAffectsOSP.Value ?  origFrac : body.oneShotProtectionFraction; });
                Logger.LogDebug("Curse behavior successful");
            };

            shieldAffectsOSP = Config.Bind("", "Shield Behavior", false, new ConfigDescription("Whether or not sources of Shield (Personal Shield Generator, Overloading affix, etc) count toward your maximum HP for OSP calculations. Vanilla is true [Note: Trancendence and Perfected Elite Affix behavior unaffected to avoid godmode issues]"));
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
                c.EmitDelegate<Func<RoR2.HealthComponent, float>>((hc) => { return shieldAffectsOSP.Value || hc.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || hc.body.HasBuff(RoR2Content.Buffs.AffixLunar) ? hc.fullCombinedHealth : hc.fullHealth; });
            };
            Logger.LogDebug("Shield behavior successful");
        }
    }
}
