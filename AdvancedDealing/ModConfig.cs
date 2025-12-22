using MelonLoader;
using MelonLoader.Utils;
using System.IO;

namespace AdvancedDealing
{
    public static class ModConfig
    {
        private static MelonPreferences_Category _category;

        private static bool _isCreated;

        public static bool Debug
        {
            get => _category.GetEntry<bool>("Debug").Value;
            set => _category.GetEntry<bool>("Debug").Value = value;
        }

        public static bool RealisticMode
        {
            get => _category.GetEntry<bool>("RealisticMode").Value;
            set => _category.GetEntry<bool>("RealisticMode").Value = value;
        }

        public static bool SettingsPerDealer
        {
            get => _category.GetEntry<bool>("SettingsPerDealer").Value;
            set => _category.GetEntry<bool>("SettingsPerDealer").Value = value;
        }

        public static bool SkipMovement
        {
            get => _category.GetEntry<bool>("SkipMovement").Value;
            set => _category.GetEntry<bool>("SkipMovement").Value = value;
        }

        public static void Create()
        {
            if (_isCreated) return;

            _category = MelonPreferences.CreateCategory($"{ModInfo.NAME}_01_General", $"{ModInfo.NAME} - General Settings", false, true);
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, $"{ModInfo.NAME}.cfg");

            _category.SetFilePath(path, true);
            CreateEntries();

            if (!File.Exists(path))
            {
                foreach (var entry in _category.Entries)
                {
                    entry.ResetToDefault();
                }

                _category.SaveToFile(false);
            }

            _isCreated = true;
        }

        private static void CreateEntries()
        {
            _category?.CreateEntry<bool>
            (
                identifier: "Debug",
                default_value: false,
                display_name: "Enable Debug Mode",
                description: "Enables debugging for this mod",
                is_hidden: true
            );
            _category?.CreateEntry<bool>
            (
                identifier: "RealisticMode",
                default_value: false,
                display_name: "Enable Realistic Mode",
                description: "Makes the mod less feel like a cheat",
                is_hidden: false
            );
            _category?.CreateEntry<bool>
            (
                identifier: "SettingsPerDealer",
                default_value: false,
                display_name: "Settings Per Dealer",
                description: "Enable seperate modification for each dealer",
                is_hidden: false
            );
            _category?.CreateEntry<bool>
            (
                identifier: "SkipMovement",
                default_value: false,
                display_name: "Skip Movement (Instant Delivery & Pickup)",
                description: "Skips all movement actions for dealers",
                is_hidden: false
            );
        }
    }
}
