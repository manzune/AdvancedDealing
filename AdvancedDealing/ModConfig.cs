using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using System.IO;

namespace AdvancedDealing
{
    public static class ModConfig
    {
        private static MelonPreferences_Category generalCategory;

        private static MelonPreferences_Category loyalityModeCategory;

        private static bool isInitialized;

        // General
        public static bool Debug
        {
            get => generalCategory.GetEntry<bool>("Debug").Value;
            set => generalCategory.GetEntry<bool>("Debug").Value = value;
        }

        public static bool LoyalityMode
        {
            get => generalCategory.GetEntry<bool>("LoyalityMode").Value;
            set => generalCategory.GetEntry<bool>("LoyalityMode").Value = value;
        }

        public static bool SkipMovement
        {
            get => generalCategory.GetEntry<bool>("SkipMovement").Value;
            set => generalCategory.GetEntry<bool>("SkipMovement").Value = value;
        }

        public static void Initialize()
        {
            if (isInitialized) return;

            generalCategory = MelonPreferences.CreateCategory($"{ModInfo.Name}_01_General", $"{ModInfo.Name} - General Settings", false, true);
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, $"{ModInfo.Name}.cfg");

            generalCategory.SetFilePath(path, true, false);

            CreateEntries();

            if (!File.Exists(path))
            {
                foreach (var entry in generalCategory.Entries)
                {
                    entry.ResetToDefault();
                }
                
                generalCategory.SaveToFile(false);
            }

            isInitialized = true;
        }

        private static void CreateEntries()
        {
            generalCategory.CreateEntry<bool>
            (
                identifier: "Debug",
                default_value: false,
                display_name: "Enable Debug Mode",
                description: "Enables debugging for this mod",
                is_hidden: false
            );
            generalCategory.CreateEntry<bool>
            (
                identifier: "LoyalityMode",
                default_value: false,
                display_name: "Loyality Mode (WIP)",
                description: "Makes the mod less feel like a cheat",
                is_hidden: false
            );
            generalCategory.CreateEntry<bool>
            (
                identifier: "SkipMovement",
                default_value: false,
                display_name: "Skip Movement (Instant Delivery)",
                description: "Skips all movement actions for dealers",
                is_hidden: false
            );
        }
    }
}
