using BepInEx;
using BepInEx.Configuration;
using IL.RoR2;
using R2API.Utils;
using MonoMod.Cil;
using System;

namespace EnhancedOSP
{
    [BepInDependency("com.bepis.r2api")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.0.0")]
    public class EnhancedOSP : BaseUnityPlugin
    {
        private static ConfigEntry<float> invTime;

        public void Awake()
        {
            invTime = Config.Bind("", "Invulnerable Time", 0.5f, new ConfigDescription("The amount of time a player remains invulnerable after one shot protection is triggered. Vanilla is 0.1"));
            HealthComponent.TriggerOneShotProtection += (il) =>
            {
                var c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(0.1f)
                    //x => x.MatchStfld("float32", "RoR2.HealthComponent::ospTimer"),
                    //x => x.MatchLdstr("OSP Triggered.")
                    );
                c.Index += 1;
                c.Next.Operand = invTime.Value;
            };
        }
    }
}
