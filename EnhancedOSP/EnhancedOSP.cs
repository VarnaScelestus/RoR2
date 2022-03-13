using BepInEx;
using BepInEx.Configuration;
using IL.RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using UnityEngine;

namespace EnhancedOSP
{
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.1.0")]
    public class EnhancedOSP : BaseUnityPlugin
    {
        private static ConfigEntry<float> invTime;
        private static ConfigEntry<float> invThreshold;
        private static ConfigEntry<bool> curseAffectsOSP;

        public void Awake()
        {
            invTime = Config.Bind("", "Invulnerable Time", 0.5f, new ConfigDescription("The amount of time a player remains invulnerable after one shot protection is triggered. Vanilla is 0.1"));
            HealthComponent.TriggerOneShotProtection += (il) =>
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
            CharacterBody.RecalculateStats += (il) =>
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
        }
    }
}
