using AdvancedDealing.Persistence.Datas;
using Newtonsoft.Json;
using System;
using AdvancedDealing.Economy;


#if IL2CPP
using Il2CppGameKit.Utilities;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Networking;
using Il2CppSteamworks;
using Il2CppSystem.Text;
#elif MONO
using GameKit.Utilities;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Networking;
using Steamworks;
using System.Text;
#endif

namespace AdvancedDealing.Persistence
{
    public class NetworkSynchronizer
    {
        protected Callback<LobbyChatMsg_t> LobbyChatMsgCallback;

        protected Callback<LobbyDataUpdate_t> LobbyDataUpdateCallback;

        private readonly string _prefix = ModInfo.Name.GetStableHashU16().ToString();

        private CSteamID _lobbySteamID;

        private bool _isSyncing;

        private bool _isHost;

        public static NetworkSynchronizer Instance { get; private set; }

        public static bool IsSyncing => Instance._isSyncing;

        public static bool IsNoSyncOrHost => !IsSyncing || (IsSyncing && Instance._isHost);

        public static CSteamID LocalSteamID => Singleton<Lobby>.Instance.LocalPlayerID;

        public NetworkSynchronizer()
        {
            if (Instance == null)
            {
                Singleton<Lobby>.Instance.onLobbyChange += new Action(OnLobbyChange);

                LobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create((Callback<LobbyChatMsg_t>.DispatchDelegate)OnLobbyMessage);

                Instance = this;
            }
        }

        private void StartSyncing()
        {
            _isHost = false;
            _lobbySteamID = Singleton<Lobby>.Instance.LobbySteamID;
            _isSyncing = true;

            Utils.Logger.Msg("NetworkSynchronizer", "Synchronization started");
        }

        private void StopSyncing()
        {
            _isHost = false;
            _lobbySteamID = CSteamID.Nil;
            _isSyncing = false;

            Utils.Logger.Msg("NetworkSynchronizer", "Synchronization stopped");
        }

        public void SetAsHost()
        {
            _isHost = true;
            Utils.Logger.Debug("NetworkSynchronizer", "Set as host");
        }

        public void SendData(DataBase data) => SendData(data.DataType, data.Identifier, JsonConvert.SerializeObject(data, ReaderWriter.JsonSerializerSettings));

        public void SendData(string dataType, string identifier, string dataString)
        {
            if (!IsSyncing) return;

            string key = $"{_prefix}_{dataType}_{identifier}";

            SteamMatchmaking.SetLobbyMemberData(_lobbySteamID, key, dataString);

            SendMessage(key);

            Utils.Logger.Debug("NetworkSynchronizer", $"Data synced with lobby: {key}");
        }

        public void SendMessage(string text, string identifier = null)
        {
            text = $"{_prefix}__{text}";

            if (identifier != null)
            {
                text += $"__{identifier}";
            }

#if IL2CPP
            Il2CppStructArray<byte> bytes = Encoding.UTF8.GetBytes(text);
#elif MONO
            byte[] bytes = Encoding.UTF8.GetBytes(text);
#endif

            SteamMatchmaking.SendLobbyChatMsg(_lobbySteamID, bytes, bytes.Length);
        }

        public void FetchData(CSteamID steamId, string key)
        {
            string dataString = SteamMatchmaking.GetLobbyMemberData(_lobbySteamID, steamId, key);

            bool success = false;

            if (dataString != null)
            {
                string[] keyArray = key.Split("_");

                if (keyArray[1] !=  null && keyArray[2] != null)
                {
                    string dataType = keyArray[1];
                    string identifier = keyArray[2];

                    if (dataType == "DealerData")
                    {
                        DealerExtension dealerExtension = DealerExtension.GetExtension(identifier);

                        if (dealerExtension != null)
                        {
                            DealerData dealerData = JsonConvert.DeserializeObject<DealerData>(dataString);

                            dealerExtension.PatchData(dealerData);

                            success = true;
                        }
                    }
                }
            }

            if (success)
            {
                Utils.Logger.Debug("NetworkSynchronizer", $"Data from lobby fetched: {key}");
            }
            else
            {
                Utils.Logger.Debug("NetworkSynchronizer", $"Could not fetch data from lobby: {key}");
            }
        }

        private void OnLobbyMessage(LobbyChatMsg_t res)
        {
            if (!IsSyncing) return;

#if IL2CPP
            Il2CppStructArray<byte> bytes = new byte[4096];
#elif MONO
            byte[] bytes = new byte[4096];
#endif

            SteamMatchmaking.GetLobbyChatEntry(_lobbySteamID, (int)res.m_iChatID, out CSteamID userSteamID, bytes, bytes.Length, out _);

            if (userSteamID == LocalSteamID) return;

            string text = Encoding.UTF8.GetString(bytes);
            text = text.TrimEnd(new char[1]);
            string[] textArray = text.Split("__");

            if (textArray[0] == _prefix)
            {
                Utils.Logger.Debug("NetworkSynchronizer", $"Received msg: {textArray[1]}");

                switch (textArray[1])
                {
                    case "data_request": // Client only message

                        if (_isHost)
                        {
                            foreach (DealerData data in SaveModifier.Instance.SaveData.Dealers)
                            {
                                SendData(data);
                            }
                        }
                        break;
                    case "dealer_fired":
                        
                        DealerExtension.GetExtension(textArray[2])?.FireDealer();
                        break;
                    default:

                        FetchData(userSteamID, textArray[1]);
                        break;
                }
            }
        }

        private void OnLobbyChange()
        {
            if (Singleton<Lobby>.Instance.IsInLobby && _isSyncing && Singleton<Lobby>.Instance.LobbySteamID != _lobbySteamID)
            {
                _lobbySteamID = Singleton<Lobby>.Instance.LobbySteamID;
            }
            else if (Singleton<Lobby>.Instance.IsInLobby && !_isSyncing)
            {
                StartSyncing();
            }
            else if (!Singleton<Lobby>.Instance.IsInLobby && _isSyncing)
            {
                StopSyncing();
            }
        }
    }
}
