using AdvancedDealing.Economy;
using AdvancedDealing.NPCs;
using AdvancedDealing.Persistence.Datas;
using AdvancedDealing.UI;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using System.IO;

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
        public static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static SaveManager Instance { get; private set; }

        public SaveData SaveData { get; private set; }

        public SaveData LastLoadedData { get; private set; }

        public string LastLoadedDataString { get; private set; }

        public bool SavegameLoaded { get; private set; }

        private static string FilePath => Path.Combine(Singleton<LoadManager>.Instance.ActiveSaveInfo.SavePath, $"{ModInfo.Name}.json");

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

                    Utils.Logger.Msg("Savegame modifications successfully injected");

                    UIModification.Load();
                }
            }
            else
            {
                MelonCoroutines.Start(LoadRoutine());

                IEnumerator LoadRoutine()
                {
                    SaveData = LoadFromFile();

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

                    Utils.Logger.Msg("Savegame modifications successfully injected");

                    UIModification.Load();
                }
            }
        }

        public SaveData LoadFromFile()
        {
            SaveData data;
            string text;

            if (!File.Exists(FilePath))
            {
                string id = $"Savegame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}";

                data = new SaveData(id);
                data.SetDefaults();

                text = JsonConvert.SerializeObject(data, JsonSerializerSettings);
            }
            else
            {
                text = File.ReadAllText(FilePath);
                data = JsonConvert.DeserializeObject<SaveData>(text, JsonSerializerSettings);
            }

            LastLoadedData = data;
            LastLoadedDataString = text;

            Utils.Logger.Msg($"Data for {data.Identifier} loaded");

            return data;
        }

        public void SaveToFile(SaveData data)
        {
            data ??= LoadFromFile();

            string text = JsonConvert.SerializeObject(data, JsonSerializerSettings);
            File.WriteAllText(FilePath, text);

            Utils.Logger.Msg($"Data for {data.Identifier} saved");
        }

        public void ClearSavegame()
        {
            UIModification.Clear();
            ScheduleManager.ClearAll();

            SaveData = null;
            SavegameLoaded = false;

            Utils.Logger.Msg($"Savegame modifications cleared");
        }

        public void UpdateSaveData(SaveData saveData)
        {
            foreach (DealerData dealerData in saveData.Dealers)
            {
                DealerManager manager = DealerManager.GetInstance(dealerData.Identifier);
                manager.PatchData(dealerData);
                manager.HasChanged = true;
            }

            SaveData = saveData;
        }

        public void CollectData()
        {
            foreach (Dealer dealer in Dealer.AllPlayerDealers)
            {
                if (DealerManager.DealerExists(dealer))
                {
                    DealerManager dealerManager = DealerManager.GetInstance(dealer);
                    DealerData dealerData = SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.GUID.ToString()));

                    if (dealerData != null)
                    {
                        SaveData.Dealers.Remove(dealerData);
                    }

                    SaveData.Dealers.Add(dealerManager.FetchData());
                }
            }
        }

        public void UpdateData(DealerData dealerData = null)
        {
            if (dealerData != null)
            {
                DealerData oldDealerData = SaveData.Dealers.Find(x => x.Identifier.Contains(dealerData.Identifier));

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
            if (SyncManager.IsNoSyncOrActiveAndHost)
            {
                CollectData();
                SaveToFile(SaveData);
            }
        }
    }
}
