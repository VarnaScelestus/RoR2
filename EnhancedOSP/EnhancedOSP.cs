using BepInEx;
using BepInEx.Configuration;
using IL.RoR2;
using MonoMod.Cil;

namespace EnhancedOSP
{
    [BepInPlugin("com.Varna.EnhancedOSP", "EnhancedOSP", "1.0.1")]
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
                    );
                c.Index += 1;
                c.Next.Operand = invTime.Value;
            };
        }
    }
}
