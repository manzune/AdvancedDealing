using AdvancedDealing;
using AdvancedDealing.Economy;
using AdvancedDealing.Persistence;
using MelonLoader;

[assembly: MelonInfo(typeof(AdvancedDealing.AdvancedDealing), $"{ModInfo.Name}", ModInfo.Version, ModInfo.Author, ModInfo.DownloadLink)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonColor(255, 170, 0, 255)]
#if IL2CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#elif MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif

namespace AdvancedDealing
{
    public class AdvancedDealing : MelonMod
    {
        public bool IsInitialized { get; private set; }

        public SaveManager SaveManager { get; private set; }

        public SyncManager SyncManager { get; private set; }

        public override void OnInitializeMelon()
        {
            ModConfig.Initialize();

            Utils.Logger.Msg($"{ModInfo.Name} v{ModInfo.Version} initialized");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                SaveManager.LoadSavegame();
            }
            else if (sceneName == "Menu")
            {
                if (!IsInitialized)
                {
                    SaveManager = new();
                    SyncManager = new();

                    IsInitialized = true;
                }

                if (SaveManager.SavegameLoaded)
                {
                    SaveManager.ClearSavegame();
                }
            }
        }
    }
}
