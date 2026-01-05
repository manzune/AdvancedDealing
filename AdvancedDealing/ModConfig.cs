using AdvancedDealing.Persistence;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using System.IO;

namespace AdvancedDealing
{
    public static class ModConfig
    {
        private static MelonPreferences_Category generalCategory;

        private static bool isInitialized;

        public static bool Debug
        {
            get => generalCategory.GetEntry<bool>("Debug").Value;
            set => generalCategory.GetEntry<bool>("Debug").Value = value;
        }

        public static bool SkipMovement
        {
            get => generalCategory.GetEntry<bool>("SkipMovement").Value;
            set => generalCategory.GetEntry<bool>("SkipMovement").Value = value;
        }

        public static bool NotifyOnAction
        {
            get => generalCategory.GetEntry<bool>("NotifyOnAction").Value;
            set => generalCategory.GetEntry<bool>("NotifyOnAction").Value = value;
        }

        public static bool AccessInventory
        {
            get
            {
                if (NetworkSynchronizer.IsSyncing && NetworkSynchronizer.Instance.SessionData != null)
                {
                    return NetworkSynchronizer.Instance.SessionData.AccessInventory;
                }

                return generalCategory.GetEntry<bool>("AccessInventory").Value;
            }
            set => generalCategory.GetEntry<bool>("AccessInventory").Value = value;
        }

        public static bool SettingsMenu
        {
            get
            {
                if (NetworkSynchronizer.IsSyncing && NetworkSynchronizer.Instance.SessionData != null)
                {
                    return NetworkSynchronizer.Instance.SessionData.SettingsMenu;
                }

                return generalCategory.GetEntry<bool>("SettingsMenu").Value;
            }
            set => generalCategory.GetEntry<bool>("SettingsMenu").Value = value;
        }

        public static float NegotiationModifier
        {
            get
            {
                if (NetworkSynchronizer.IsSyncing && NetworkSynchronizer.Instance.SessionData != null)
                {
                    return NetworkSynchronizer.Instance.SessionData.NegotiationModifier;
                }
                
                return generalCategory.GetEntry<float>("NegotiationModifier").Value;
            }
            set => generalCategory.GetEntry<float>("NegotiationModifier").Value = value;
        }

        public static void Initialize()
        {
            if (isInitialized) return;

            generalCategory = MelonPreferences.CreateCategory($"{ModInfo.NAME}_01_General", $"{ModInfo.NAME} - General Settings", false, true);
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, $"{ModInfo.NAME}.cfg");

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
                identifier: "SkipMovement",
                default_value: false,
                display_name: "Skip Movement (Instant Delivery)",
                description: "Skips all movement actions for dealers",
                is_hidden: false
            );
            generalCategory.CreateEntry<bool>
            (
                identifier: "NotifyOnAction",
                default_value: true,
                display_name: "Notify On Actions",
                description: "Sends notifications after some actions got triggered",
                is_hidden: false
            );
            generalCategory.CreateEntry<bool>
            (
                identifier: "AccessInventory",
                default_value: false,
                display_name: "Access Dealer Inventories Remotely",
                description: "Enables the option to access the dealer inventory via text message",
                is_hidden: false
            );
            generalCategory.CreateEntry<bool>
            (
                identifier: "SettingsMenu",
                default_value: false,
                display_name: "Enable Dealer Settings Menu",
                description: "Allows access to the dealer settings menu via text message",
                is_hidden: false
            );
            generalCategory.CreateEntry<float>
            (
                identifier: "NegotiationModifier",
                default_value: 0.5f,
                display_name: "Negotiation Modifier (Higher = Better Chance)",
                description: "Modifier used to calculate the negotiation success.",
                is_hidden: false,
                validator: new ValueRange<float>(0f, 1f)
            );
        }
    }
}
