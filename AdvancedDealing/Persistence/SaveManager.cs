using AdvancedDealing.Economy;
using AdvancedDealing.NPCs;
using AdvancedDealing.Persistence.Datas;
using AdvancedDealing.UI;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Networking;
using S1SaveManager = Il2CppScheduleOne.Persistence.SaveManager;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Networking;
using S1SaveManager = ScheduleOne.Persistence.SaveManager;
#endif

namespace AdvancedDealing.Persistence
{
    public class SaveManager
    {
        public SaveData SaveData { get; private set; }

        public bool SavegameLoaded { get; private set; }

        public static SaveManager Instance { get; private set; }

        public SaveManager()
        {
            if (Instance == null)
            {
                Singleton<S1SaveManager>.Instance.onSaveComplete.AddListener((UnityAction)OnSaveComplete);
                Instance = this;
            }
        }

        public void LoadSavegame()
        {
            Utils.Logger.Msg("Preparing savegame modifications...");

            if (Singleton<Lobby>.Instance.IsInLobby && !Singleton<Lobby>.Instance.IsHost)
            {
                MelonCoroutines.Start(ClientLoadRoutine());

                IEnumerator ClientLoadRoutine()
                {
                    SaveData = null;

                    bool dataExist = SyncManager.Instance.FetchDataFromLobby();
                    while (SaveData == null)
                    {
                        if (!dataExist)
                        {
                            SyncManager.Instance.SendDataUpdateRequest();
                            yield return new WaitForSecondsRealtime(2f);
                            dataExist = SyncManager.Instance.FetchDataFromLobby();
                        }
                        yield return new WaitForSecondsRealtime(2f);
                    }

                    SavegameLoaded = true;

                    DeadDropManager.Load();
                    DealerManager.Load();

                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                MelonCoroutines.Start(LoadRoutine());

                IEnumerator LoadRoutine()
                {
                    SaveData = DataManager.LoadFromFile();

                    while (SaveData == null)
                    {
                        yield return new WaitForSecondsRealtime(2f);
                    }

                    DeadDropManager.Load();
                    DealerManager.Load();

                    yield return new WaitForSecondsRealtime(2f);

                    SavegameLoaded = true;

                    if (SyncManager.IsActive)
                    {
                        SyncManager.Instance.SetAsHost();
                        SyncManager.Instance.PushUpdate();
                    }
                }
            }

            Utils.Logger.Msg("Savegame modifications successfully injected");
        }

        public void ClearSavegame()
        {
            DealerManagementAppModification.Clear();
            Schedule.ClearAll();

            Utils.Logger.Msg($"Savegame modifications cleared");
        }

        public void UpdateSaveData(SaveData saveData)
        {
            foreach (DealerData dealerData in saveData.Dealers)
            {
                Dealer dealer = DealerManager.GetDealer(dealerData.Identifier);

                DealerManager.SetData(dealer, dealerData);
                DealerManager.Update(dealer, false);
            }

            SaveData = saveData;
        }

        private void OnSaveComplete()
        {
            if (SyncManager.NoSyncOrActiveAndHost)
            {
                DataManager.SaveToFile(SaveData);
            }
        }
    }
}
