using AdvancedDealing.Economy;
using AdvancedDealing.NPCs;
using AdvancedDealing.Persistence.Datas;
using AdvancedDealing.UI;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System.IO;


#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Persistence;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
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

        private static string FilePath => Path.Combine(Singleton<LoadManager>.Instance.ActiveSaveInfo.SavePath, $"{ModInfo.Name}.json");

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
                if (!ReaderWriter.LoadFromFile<DataWrapper>(FilePath, out var data))
                {
                    SaveData = new()
                    {
                        SaveName = $"savegame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}",
                        Dealers = []
                    };
                }
                else
                {
                    SaveData = data;
                }

                DeadDropExtension.ExtendDeadDrops();
                DealerExtension.ExtendDealers();

                yield return new WaitForSecondsRealtime(2f);

                SavegameLoaded = true;

                if (NetworkSynchronizer.IsSyncing)
                {
                    NetworkSynchronizer.Instance.SetAsHost();
                }

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

                DeadDropExtension.ExtendDeadDrops();
                DealerExtension.ExtendDealers();

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
            Schedule.ClearAllSchedules();

            SavegameLoaded = false;

            Utils.Logger.Msg($"Savegame modifications cleared");
        }

        private void OnSaveComplete()
        {
            if (NetworkSynchronizer.IsNoSyncOrHost)
            {
                DataWrapper wrapper = new()
                {
                    SaveName = $"savegame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}",
                    Dealers = DealerExtension.GetAllDealerData()
                };

                ReaderWriter.SaveToFile(FilePath, wrapper);
            }
        }
    }
}
