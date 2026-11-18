using MelonLoader;

[assembly: MelonInfo(typeof(SettingsHub.Mod), "SettingsHub", SettingsHub.Mod.Version, "WWTHP")]
[assembly: MelonGame(null, "over the hill")]

namespace SettingsHub
{
    /// <summary>
    /// One menu (MenuKey, default F5) with a tab per mod. Mods that register a tab (see
    /// <see cref="Registry"/>) draw their own page and drop their own menu key while the hub is
    /// running; every other mod's MelonPreferences get a plain editor tab.
    /// </summary>
    public sealed class Mod : MelonMod
    {
        public const string Version = "0.1.0";
        internal static MelonLogger.Instance Log;
        private bool _announced;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.Init();
            CrashGuard.Init();
            Announce();
            Log.Msg($"SettingsHub {Version} loaded. {Settings.MenuKey.Value} = settings for all mods.");
        }

        public override void OnUpdate()
        {
            if (_announced != Settings.Enabled.Value) Announce();
            if (!Settings.Enabled.Value) return;
            Hub.Update();
        }

        public override void OnGUI()
        {
            if (Settings.Enabled.Value) Hub.Draw();
        }

        private void Announce()
        {
            _announced = Settings.Enabled.Value;
            Registry.Announce(_announced);
            if (!_announced) Hub.Close();
        }
    }
}
