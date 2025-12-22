using AdvancedDealing;
using MelonLoader;

[assembly: MelonInfo(typeof(AdvancedDealing.AdvancedDealing), ModInfo.NAME, ModInfo.VERSION, ModInfo.AUTHOR, ModInfo.DOWNLOAD_LINK)]
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

        public readonly Persistence.SaveManager loader = new();

        public override void OnInitializeMelon()
        {
            ModConfig.Create();

            Utils.Logger.Msg($"{ModInfo.NAME} {ModInfo.VERSION} initialized");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (_isInitialized || sceneName != "Menu") return;

            _isInitialized = true;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                loader.LoadSavegame();
            }
            else if (sceneName == "Menu" && loader.SavegameLoaded)
            {
                loader.ClearSavegame();
            }
        }
    }
}
