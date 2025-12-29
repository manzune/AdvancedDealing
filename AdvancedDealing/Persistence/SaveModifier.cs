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

        public SaveDataContainer SaveData { get; private set; }

        public bool SavegameLoaded { get; private set; }

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
                MelonCoroutines.Start(ClientLoadRoutine());

                IEnumerator ClientLoadRoutine()
                {
                    SaveData = new("temporary");

                    DeadDropExtension.ExtendDeadDrops();
                    DealerExtension.ExtendDealers();

                    yield return new WaitForSecondsRealtime(2f);

                    SavegameLoaded = true;

                    NetworkSynchronizer.Instance.SendMessage("data_request");

                    UIBuilder.Build();

                    Utils.Logger.Msg("Savegame modifications successfully injected");
                }
            }
            else
            {
                MelonCoroutines.Start(LoadRoutine());

                IEnumerator LoadRoutine()
                {
                    SaveData = null;
                    SaveData = DataReaderWriter.LoadFromFile();

                    while (SaveData == null)
                    {
                        yield return new WaitForSecondsRealtime(2f);
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
        }

        public void ClearModifications()
        {
            UIBuilder.Reset();
            Schedule.ClearAllSchedules();

            SaveData = null;
            SavegameLoaded = false;

            Utils.Logger.Msg($"Savegame modifications cleared");
        }

        public void UpdateSaveData(SaveDataContainer saveData, bool isSaveDataRequest = false)
        {
            if (!isSaveDataRequest)
            {
                foreach (DealerDataContainer dealerData in saveData.Dealers)
                {
                    DealerExtension dealer = DealerExtension.GetExtension(dealerData.Identifier);
                    dealer.PatchData(dealerData);
                    dealer.HasChanged = true;
                }
            }

            SaveData = saveData;
        }

        public void CollectData()
        {
            foreach (Dealer dealer in Dealer.AllPlayerDealers)
            {
                if (DealerExtension.ExtensionExists(dealer))
                {
                    DealerExtension dealerExtension = DealerExtension.GetExtension(dealer);
                    DealerDataContainer dealerData = SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.GUID.ToString()));

                    if (dealerData != null)
                    {
                        SaveData.Dealers.Remove(dealerData);
                    }

                    SaveData.Dealers.Add(dealerExtension.FetchData());
                }
            }
        }

        public void UpdateData(DealerDataContainer dealerData = null)
        {
            if (dealerData != null)
            {
                DealerDataContainer oldDealerData = SaveData.Dealers.Find(x => x.Identifier.Contains(dealerData.Identifier));

                if (oldDealerData != null)
                {
                    SaveData.Dealers.Remove(oldDealerData);
                }

                SaveData.Dealers.Add(dealerData);

                Utils.Logger.Debug("SaveManager", $"Dealer data updated: {dealerData.Identifier}");
            }
        }

        private void OnSaveComplete()
        {
            if (NetworkSynchronizer.IsNoSyncOrHost)
            {
                CollectData();
                DataReaderWriter.SaveToFile(SaveData);
            }
        }
    }
}
