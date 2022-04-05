using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;
using RoR2;

namespace EnhancedOSP
{
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.3.0")]
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
            Logger.LogDebug("Invuln time patched");
            
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
                Logger.LogDebug("OSP threshold patched");

                c.GotoNext(
                    x => x.MatchCallOrCallvirt<Mathf>("Max"),
                    x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("set_oneShotProtectionFraction")
                    );
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoR2.CharacterBody, float>>((origFrac, body) => { return curseAffectsOSP.Value ?  origFrac : body.oneShotProtectionFraction; });
                Logger.LogDebug("Curse behavior patched");
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
                c.EmitDelegate<Func<RoR2.HealthComponent, float>>((hc) => { 
                    return (shieldAffectsOSP.Value || hc.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || hc.body.HasBuff(RoR2Content.Buffs.AffixLunar)) 
                    ? 
                    hc.fullCombinedHealth 
                    : 
                    hc.fullHealth;
                });
            };
            Logger.LogDebug("Shield behavior patched");

            removeCurseDisplay = Config.Bind("", "Curse Healthbar Display", false, new ConfigDescription("Whether or not sources of MaxHP reduction (Shaped Glass, etc) are represented on the HUD via a pointless 'glass' effect that takes up space and makes the bar harder to read during gameplay. Vanilla is true"));
            IL.RoR2.HealthComponent.GetHealthBarValues += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    // 	float num = 1f - 1f / body.cursePenalty;
                    /*
                    IL_0000: ldc.r4 1

                    IL_0005: ldc.r4 1

                    IL_000a: ldarg.0

                    IL_000b: ldfld class RoR2.CharacterBody RoR2.HealthComponent::body

                    IL_0010: callvirt instance float32 RoR2.CharacterBody::get_cursePenalty()
                    IL_0015: div
                    IL_0016: sub
                    IL_0017: stloc.0
                    */
                    x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("get_cursePenalty")
                    );
                c.Remove();
                c.EmitDelegate<Func<RoR2.CharacterBody, float>>( (body) => { return removeCurseDisplay.Value ? body.cursePenalty : 1f; } );
                Logger.LogDebug("Curse healthbar display setting patched");

                //This section fixes the HUD to work with our previous changes.
                c.GotoNext(
                    // 	float num3 = body.oneShotProtectionFraction * fullCombinedHealth - missingCombinedHealth;
                    /*
                    IL_0027: ldarg.0

                    IL_0028: ldfld class RoR2.CharacterBody RoR2.HealthComponent::body

                    IL_002d: callvirt instance float32 RoR2.CharacterBody::get_oneShotProtectionFraction()
                    IL_0032: ldarg.0

                    IL_0033: call instance float32 RoR2.HealthComponent::get_fullCombinedHealth()
                    IL_0038: mul
                    IL_0039: ldarg.0

                    IL_003a: call instance float32 RoR2.HealthComponent::get_missingCombinedHealth()
                    IL_003f: sub
                    IL_0040: stloc.2
                    */
                    x => x.MatchCallOrCallvirt<RoR2.CharacterBody>("get_oneShotProtectionFraction")
                    );
                c.Index--;
                c.RemoveRange(8);
                c.EmitDelegate<Func<RoR2.HealthComponent, float>>((hc) => { 
                    return (shieldAffectsOSP.Value || hc.body.inventory.GetItemCount(RoR2Content.Items.ShieldOnly) > 0 || hc.body.HasBuff(RoR2Content.Buffs.AffixLunar)) 
                    ?
                    hc.body.oneShotProtectionFraction * hc.fullCombinedHealth - hc.missingCombinedHealth
                    :
                    hc.body.oneShotProtectionFraction * hc.fullHealth - (hc.fullHealth - hc.health); 
                });
                Logger.LogDebug("OSP healthbar display fix patched");
            };

        }
    }
}
