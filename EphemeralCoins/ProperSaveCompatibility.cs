using System.Runtime.CompilerServices;

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
}