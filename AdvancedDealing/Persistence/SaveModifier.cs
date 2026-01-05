using AdvancedDealing.Economy;
using AdvancedDealing.UI;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Collections.Generic;
using AdvancedDealing.Persistence.IO;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Persistence;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
#endif

namespace AdvancedDealing.Persistence
{
    public class SaveModifier
    {
        public static SaveModifier Instance { get; private set; }

        public DataWrapper SaveData { get; private set; }

        public bool SavegameLoaded { get; private set; }

        private static string FilePath => Path.Combine(Singleton<LoadManager>.Instance.ActiveSaveInfo.SavePath, $"{ModInfo.NAME}.json");

        public SaveModifier()
        {
            if (Instance == null)
            {
                Singleton<SaveManager>.Instance.onSaveComplete.AddListener((UnityAction)OnSaveComplete);
                Instance = this;
            }
        }

        public void LoadModifications()
        {
            Utils.Logger.Msg("Preparing savegame modifications...");

            if (Singleton<Lobby>.Instance.IsInLobby && !Singleton<Lobby>.Instance.IsHost)
            {
                LoadModificationsAsClient();

                return;
            }

            MelonCoroutines.Start(LoadRoutine());

            IEnumerator LoadRoutine()
            {
                if (!FileSerializer.LoadFromFile<DataWrapper>(FilePath, out var data))
                {
                    SaveData = new()
                    {
                        SaveName = $"SaveGame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}",
                        Dealers = [],
                        DeadDrops = []
                    };
                }
                else
                {
                    SaveData = new()
                    {
                        SaveName = $"SaveGame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}",
                        Dealers = data.Dealers ?? [],
                        DeadDrops = data.DeadDrops ?? []
                    };
                }

                DeadDropExtension.Initialize();
                DealerExtension.Initialize();

                if (NetworkSynchronizer.IsSyncing)
                {
                    NetworkSynchronizer.Instance.SetAsHost();
                    NetworkSynchronizer.Instance.SessionData = new(Singleton<Lobby>.Instance.LobbySteamID.ToString())
                    {
                        AccessInventory = ModConfig.AccessInventory,
                        SettingsMenu = ModConfig.SettingsMenu,
                        NegotiationModifier = ModConfig.NegotiationModifier
                    };
                }

                yield return new WaitForSecondsRealtime(2f);

                SavegameLoaded = true;

                UIBuilder.Build();

                Utils.Logger.Msg("Savegame modifications successfully injected");
            }
        }

        private void LoadModificationsAsClient()
        {
            MelonCoroutines.Start(ClientLoadRoutine());

            IEnumerator ClientLoadRoutine()
            {
                SaveData = new()
                {
                    SaveName = "temporary",
                    Dealers = []
                };

                DeadDropExtension.Initialize();
                DealerExtension.Initialize();

                yield return new WaitForSecondsRealtime(2f);

                SavegameLoaded = true;

                NetworkSynchronizer.Instance.SendMessage("data_request");

                UIBuilder.Build();

                Utils.Logger.Msg("Savegame modifications successfully injected");
            }
        }

        public void ClearModifications()
        {
            UIBuilder.Reset();
            List<DealerExtension> dealers = DealerExtension.GetAllDealers();

            for (int i = dealers.Count - 1; i >= 0; i--)
            {
                dealers[i].Destroy(true);
            }

            SavegameLoaded = false;

            Utils.Logger.Msg($"Savegame modifications cleared");
        }

        private void OnSaveComplete()
        {
            if (NetworkSynchronizer.IsNoSyncOrHost)
            {
                DataWrapper wrapper = new()
                {
                    SaveName = $"SaveGame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}",
                    Dealers = DealerExtension.FetchAllDealerDatas(),
                    DeadDrops = DeadDropExtension.FetchAllDeadDropDatas()
                };

                Utils.Logger.Msg($"Data for {wrapper.SaveName} saved");

                FileSerializer.SaveToFile(FilePath, wrapper);
            }
        }
    }
}
