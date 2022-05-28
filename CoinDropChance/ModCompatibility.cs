using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace CoinDropChance
{
    public static class RiskOfOptionsCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
                }
                return (bool)_enabled;
            }
        }

        public static void InvokeAddOptionStepSlider(ConfigEntry<float> configEntry, int min, int max, float increment) => InvokeAddOptionStepSlider(configEntry, min, max, increment, false, false);
        public static void InvokeAddOptionStepSlider(ConfigEntry<float> configEntry, int min, int max, float increment, bool checkArtifact, bool restartRequired)
        {
            RiskOfOptions.ModSettingsManager.AddOption(
                new StepSliderOption(
                    configEntry, 
                    new StepSliderConfig() { min = min, max = max, increment = increment, restartRequired = restartRequired }
                    )
                );
        }

        public static void InvokeSetModDescription(string desc)
        {
            RiskOfOptions.ModSettingsManager.SetModDescription(desc);
        }

    }
}