using AdvancedDealing.Persistence.Datas;
using Newtonsoft.Json;
using System;
using AdvancedDealing.Economy;


#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Networking;
using Il2CppSteamworks;
using Il2CppSystem.Text;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Networking;
using Steamworks;
using System.Text;
#endif

namespace AdvancedDealing.Persistence
{
    public class SyncManager
    {
        protected Callback<LobbyChatMsg_t> LobbyChatMsgCallback;

        protected Callback<LobbyDataUpdate_t> LobbyDataUpdateCallback;

        private CSteamID _lobbySteamID;

        private bool _isRunning;

        private bool _isHost;

        public static bool IsActive => (Instance._isRunning && SaveManager.Instance.SavegameLoaded);

        public static bool IsNoSyncOrActiveAndHost => (!IsActive || (IsActive && Instance._isHost));

        public static bool IsActiveAndHost => (IsActive && Instance._isHost);

        public static SyncManager Instance { get; private set; }

        public SyncManager()
        {
            if (Instance == null)
            {
                Singleton<Lobby>.Instance.onLobbyChange += new Action(OnLobbyChange);

                LobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create((Callback<LobbyChatMsg_t>.DispatchDelegate)OnLobbyChatMsg);
                LobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create((Callback<LobbyDataUpdate_t>.DispatchDelegate)OnLobbyDataUpdate);

                Instance = this;
            }
        }

        private void Start()
        {
            _isHost = false;
            _lobbySteamID = Singleton<Lobby>.Instance.LobbySteamID;
            _isRunning = true;

            Utils.Logger.Msg("SyncManager", "Synchronization started");
        }

        private void Stop()
        {
            _isHost = false;
            _lobbySteamID = CSteamID.Nil;
            _isRunning = false;

            Utils.Logger.Msg("SyncManager", "Synchronization stopped");
        }

        public void SetAsHost() =>
            _isHost = true;

        public void PushUpdate()
        {
            if (!IsActive) return;

            SaveManager.Instance.CollectData();

            if (_isHost)
            {
                SyncDataAsServer();
            }
            else
            {
                SyncDataAsClient();
            }
        }

        private void SyncDataAsServer(string dataString = null)
        {
            if (!IsActive || !_isHost) return;

            string key = ModInfo.Name;
            dataString ??= JsonConvert.SerializeObject(SaveManager.Instance.SaveData);

            SteamMatchmaking.SetLobbyData(_lobbySteamID, key, dataString);

            Utils.Logger.Debug("SyncManager", "Sent data update to clients");
        }

        private void SyncDataAsClient()
        {
            if (!IsActive || _isHost) return;

            string key = ModInfo.Name;
            string dataString = JsonConvert.SerializeObject(SaveManager.Instance.SaveData);

            SteamMatchmaking.SetLobbyMemberData(_lobbySteamID, key, dataString);

            Utils.Logger.Debug("SyncManager", "Sent data update to server");
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t res)
        {
            if (!IsActive) return;

            string data;

            if (res.m_ulSteamIDMember == res.m_ulSteamIDLobby)
            {
                if (_isHost) return;

                Utils.Logger.Debug("SyncManager", "Receiving data update from server ...");

                data = SteamMatchmaking.GetLobbyData(_lobbySteamID, ModInfo.Name);
            }
            else
            {
                if (!_isHost) return;

                Utils.Logger.Debug("SyncManager", "Receiving data update from client ...");

                data = SteamMatchmaking.GetLobbyMemberData(_lobbySteamID, (CSteamID)res.m_ulSteamIDMember, ModInfo.Name);
            }

            if (data == null)
            {
                Utils.Logger.Error("SyncManager", "Could not fetch data");
                return;
            }

            string currentData = JsonConvert.SerializeObject(SaveManager.Instance.SaveData);

            if (currentData != data)
            {
                SaveManager.Instance.UpdateSaveData(JsonConvert.DeserializeObject<SaveData>(data));

                if (_isHost)
                {
                    SyncDataAsServer(data);
                }

                Utils.Logger.Debug("SyncManager", "Data synchronised");
            }
        }

        public bool FetchDataFromLobby()
        {
            string data = SteamMatchmaking.GetLobbyData(_lobbySteamID, ModInfo.Name);

            if (data == null)
            {
                return false;
            }

            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(data);
            SaveManager.Instance.UpdateSaveData(saveData);

            return true;
        }

        public void SendDataUpdateRequest()
        {
            SendLobbyChatMsg("data_request");
        }

        public void SendLobbyChatMsg(string text)
        {
            text = $"{ModInfo.Name}__{text}";
#if IL2CPP
            Il2CppStructArray<byte> bytes = Encoding.ASCII.GetBytes(text);
#elif MONO
            byte[] bytes = Encoding.ASCII.GetBytes(text);
#endif

            SteamMatchmaking.SendLobbyChatMsg(_lobbySteamID, bytes, bytes.Length);
        }

        private void OnLobbyChatMsg(LobbyChatMsg_t res)
        {
            if (!IsActive) return;

#if IL2CPP
            Il2CppStructArray<byte> bytes = new byte[4096];
#elif MONO
            byte[] bytes = new byte[4096];
#endif

            SteamMatchmaking.GetLobbyChatEntry(_lobbySteamID, (int)res.m_iChatID, out CSteamID userSteamID, bytes, bytes.Length, out _);

            string text = Encoding.ASCII.GetString(bytes);
            text = text.TrimEnd(new char[1]);
            string[] data = text.Split("__");

            if (data[0] == ModInfo.Name)
            {
                Utils.Logger.Debug("SyncManager", $"Received msg: {data[1]}");

                switch (data[1])
                {
                    case "data_request": // Client only message
                        if (_isHost)
                        {
                            PushUpdate();
                        }
                        break;
                    case "dealer_fired": // Host only message
                        if (!_isHost)
                        {
                            DealerManager.GetInstance(data[2])?.Fire();
                        }
                        break;
                }
            }
        }

        private void OnLobbyChange()
        {
            if (Singleton<Lobby>.Instance.IsInLobby && _isRunning && Singleton<Lobby>.Instance.LobbySteamID != _lobbySteamID)
            {
                _lobbySteamID = Singleton<Lobby>.Instance.LobbySteamID;
            }
            else if (Singleton<Lobby>.Instance.IsInLobby && !_isRunning)
            {
                Start();
            }
            else if (!Singleton<Lobby>.Instance.IsInLobby && _isRunning)
            {
                Stop();
            }
        }
    }
}
