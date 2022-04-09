using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace EphemeralCoins
{
    public static class ProperSaveCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null) {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsRunNew()
        {
            return !ProperSave.Loading.IsLoading;
        }
    }

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

        public static void InvokeAddOptionStepSlider(ConfigEntry<float> configEntry, int min, int max, float increment, bool checkIfDisabled) => InvokeAddOptionStepSlider(configEntry, min, max, increment, checkIfDisabled, false);
        public static void InvokeAddOptionStepSlider(ConfigEntry<float> configEntry, int min, int max, float increment, bool checkIfDisabled, bool restartRequired)
        {
            RiskOfOptions.ModSettingsManager.AddOption(
                new StepSliderOption(
                    configEntry, 
                    new StepSliderConfig() { min = min, max = max, increment = increment, checkIfDisabled = delegate () { return checkIfDisabled; } }
                    )
                );
        }

        public static void InvokeAddOptionCheckBox(ConfigEntry<bool> configEntry, bool checkIfDisabled) => InvokeAddOptionCheckBox(configEntry, checkIfDisabled, false);
        public static void InvokeAddOptionCheckBox(ConfigEntry<bool> configEntry, bool checkIfDisabled, bool restartRequired)
        {
            RiskOfOptions.ModSettingsManager.AddOption(
                new CheckBoxOption(
                    configEntry,
                    new CheckBoxConfig() { checkIfDisabled = delegate () { return checkIfDisabled; } }
                    )
                );
        }

        public static void InvokeSetModIcon(UnityEngine.Sprite iconSprite)
        {
            RiskOfOptions.ModSettingsManager.SetModIcon(iconSprite);
        }

        public static void InvokeSetModDescription(string desc)
        {
            RiskOfOptions.ModSettingsManager.SetModDescription(desc);
        }

    }
}