using AdvancedDealing;
using AdvancedDealing.Persistence;
using MelonLoader;

[assembly: MelonInfo(typeof(AdvancedDealing.AdvancedDealing), $"{ModInfo.NAME}", ModInfo.VERSION, ModInfo.AUTHOR, ModInfo.DOWNLOAD_LINK)]
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
        private bool _isInitialized;

        public SaveManager SaveManager { get; private set; }

        public SyncManager SyncManager { get; private set; }

        public override void OnInitializeMelon()
        {
            ModConfig.Initialize();

            Utils.Logger.Msg($"{ModInfo.NAME} v{ModInfo.VERSION} initialized");
        }

        public override void OnEarlyInitializeMelon()
        {
#if IL2CPP
            if (!MelonUtils.IsGameIl2Cpp())
#elif MONO
            if (MelonUtils.IsGameIl2Cpp())
#endif
            {
                Unregister("Prevent initializing mod for wrong domain version", false);
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                SaveManager.LoadSavegame();
            }
            else if (sceneName == "Menu")
            {
                if (!_isInitialized)
                {
                    SaveManager = new();
                    SyncManager = new();
                    _isInitialized = true;
                }

                if (SaveManager.SavegameLoaded)
                {
                    SaveManager.ClearSavegame();
                }
            }
        }
    }
}
