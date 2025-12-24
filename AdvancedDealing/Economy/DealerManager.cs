using AdvancedDealing.Messaging;
using AdvancedDealing.Messaging.Messages;
using AdvancedDealing.NPCs;
using AdvancedDealing.NPCs.Actions;
using AdvancedDealing.Persistence;
using AdvancedDealing.Persistence.Datas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.GameTime;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
using ScheduleOne.UI.Phone.Messages;
using ScheduleOne.GameTime;
#endif

namespace AdvancedDealing.Economy
{
    public class DealerManager
    {
        private static readonly List<DealerManager> cache = [];

        public readonly Dealer Dealer;

        public readonly ScheduleManager Schedule;

        public readonly ConversationManager Conversation;

        public readonly LevelWatcher LevelWatcher;

        public string DeadDrop;

        public bool IsFired;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        public float Experience;

        public int Level;

        public float Loyality;

        public bool DeliverCash;

        public bool NotifyOnCashDelivery;

        public float CashThreshold;

        public int DaysUntilNextNegotiation;

        public bool HasChanged;

        public DealerManager(Dealer dealer)
        {
            Dealer = dealer;
            DealerData dealerData = SaveManager.Instance.SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.GUID.ToString()));

            if (dealerData == null)
            {
                dealerData = new(dealer.GUID.ToString());
                dealerData.SetDefaults();
                SaveManager.Instance.SaveData.Dealers.Add(dealerData);
            }

            PatchData(dealerData);

            UpdateDealer();

            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance.onDayPass += new Action(OnDayPassed);

            Schedule = new(dealer);
            Schedule.AddAction(new DeliverCashSignal(this));

            Conversation = new(dealer);
            Conversation.AddMessage(new EnableDeliverCash(this));
            Conversation.AddMessage(new DisableDeliverCash(this));
            Conversation.AddMessage(new AccessInventory(this));
            Conversation.AddMessage(new NegotiateCut(this));
            Conversation.AddMessage(new AdjustSettings(this));
            Conversation.AddMessage(new Fired(this));

            LevelWatcher = new(this);
        }

        public static List<DealerManager> GetAllInstances()
        {
            return cache;
        }

        public static DealerManager GetInstance(Dealer dealer) => GetInstance(dealer.GUID.ToString());

        public static DealerManager GetInstance(string guid)
        {
            if (guid == null)
            {
                return null;
            }

            return cache.Find(x => x.Dealer.GUID.ToString().Contains(guid));
        }

        public static void AddDealer(Dealer dealer)
        {
            if (dealer.IsRecruited && IsPlayerDealer(dealer) && !DealerExists(dealer))
            {
                cache.Add(new(dealer));

                Utils.Logger.Debug("DealerManager", $"Dealer added: {dealer.fullName}");
            }
        }

        public static bool DealerExists(Dealer dealer) => DealerExists(dealer.GUID.ToString());

        public static bool DealerExists(string guid)
        {
            if (guid == null)
            {
                return false;
            }

            return cache.Any(x => x.Dealer.GUID.ToString().Contains(guid));
        }

        public static bool IsPlayerDealer(Dealer dealer)
        {
            return Dealer.AllPlayerDealers.Contains(dealer);
        }

        public static void Load()
        {
            for (int i = cache.Count - 1; i >= 0; i--)
            {
                cache[i].Destroy();
            }

            foreach (Dealer dealer in Dealer.AllPlayerDealers)
            {
                AddDealer(dealer);
            }

            Dealer.onDealerRecruited -= new Action<Dealer>(OnDealerRecruited);
            Dealer.onDealerRecruited += new Action<Dealer>(OnDealerRecruited);
        }

        private static void OnDealerRecruited(Dealer dealer)
        {
            AddDealer(dealer);
        }

        public void Destroy()
        {
            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance.onDayPass -= new Action(OnDayPassed);

            Schedule.Destroy();
            Conversation.Destroy();

            cache.Remove(this);
        }

        public DealerData FetchData()
        {
            DealerData data = new(Dealer.GUID.ToString());
            FieldInfo[] fields = typeof(DealerData).GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo localField = GetType().GetField(fields[i].Name);
                if (localField != null)
                {
                    fields[i].SetValue(data, localField.GetValue(this));
                }
            }

            return data;
        }

        public void PatchData(DealerData data)
        {
            FieldInfo[] fields = typeof(DealerData).GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo localField = GetType().GetField(fields[i].Name);
                localField?.SetValue(this, fields[i].GetValue(data));
            }

            if (ModConfig.RealisticMode)
            {
                int multiplicator = Level - 1;
                MaxCustomers = LevelWatcher.MaxCustomersBase + (multiplicator * ModConfig.MaxCustomersPerLevel);
                ItemSlots = LevelWatcher.ItemSlotsBase + (multiplicator * ModConfig.ItemSlotsPerLevel);
                SpeedMultiplier = LevelWatcher.SpeedMultiplierBase + (multiplicator * ModConfig.SpeedIncreasePerLevel);
            }
        }

        public void SendMessage(string text, bool notify = true, bool network = true, float delay = 0)
        {
            if (delay != 0)
            {
                MessageChain msgChain = new();
                msgChain.Messages.Add(text);
                msgChain.id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

                Dealer.MSGConversation.SendMessageChain(msgChain, delay, notify, network);
            }
            else
            {
                Message msg = new(text, Message.ESenderType.Other);
                Dealer.MSGConversation.SendMessage(msg, notify, network);
            }
        }

        public void SendPlayerMessage(string text)
        {
            Message msg = new(text, Message.ESenderType.Player);
            Dealer.MSGConversation.SendMessage(msg);
        }

        public void Fire()
        {
            IsFired = true;

            Dealer.SetCash(0f);

            for (int i = Dealer.AssignedCustomers.Count - 1; i >= 0; i--)
            {
                Dealer.RemoveCustomer(Dealer.AssignedCustomers[i]);
            }

            Dealer.ActiveContracts.Clear();
            Dealer.Inventory.Clear();
            Dealer.HasChanged = true;

#if IL2CPP
            if (PlayerSingleton<DealerManagementApp>.Instance.SelectedDealer == Dealer)
            {
                PlayerSingleton<DealerManagementApp>.Instance.SelectedDealer = null;
            }

            PlayerSingleton<DealerManagementApp>.Instance.dealers.Remove(Dealer);
            Dealer.IsRecruited = false;
#elif MONO
            if (PlayerSingleton<DealerManagementApp>.Instance.SelectedDealer == Dealer)
            {
                typeof(DealerManagementApp).GetProperty("SelectedDealer").SetValue(PlayerSingleton<DealerManagementApp>.Instance, null);
            }
            
            List<Dealer> dealers = typeof(DealerManagementApp).GetField("dealers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(PlayerSingleton<DealerManagementApp>.Instance) as List<Dealer>;            
            dealers.Remove(Dealer);
            typeof(Dealer).GetProperty("IsRecruited").SetValue(Dealer, false);
#endif

            if (SyncManager.IsActiveAndHost)
            {
                SyncManager.Instance.SendLobbyChatMsg($"dealer_fired__{Dealer.GUID}");
            }

            SendMessage("Hmpf okay, get in touch if you need me", false, true, 0.5f);
            SaveManager.Instance.UpdateData(FetchData());
            Destroy();

            Utils.Logger.Debug("DealerManager", $"Dealer fired: {Dealer.fullName}");
        }

        private void UpdateDealer()
        {
            NPCInventory inventory = Dealer.Inventory;

            if (inventory.ItemSlots.Count < ItemSlots)
            {
                int slotsToAdd = ItemSlots - inventory.ItemSlots.Count;

                for (int i = 0; i < slotsToAdd; i++)
                {
                    inventory.ItemSlots.Add(new());
                    inventory.SlotCount = ItemSlots;
                }

                Utils.Logger.Debug("DealerManager", $"Added item slots to {Dealer.fullName}: {slotsToAdd} ");
            }
            else if (inventory.ItemSlots.Count > ItemSlots)
            {
                int slotsToRemove = inventory.ItemSlots.Count - ItemSlots;

                inventory.ItemSlots.RemoveRange(inventory.ItemSlots.Count - slotsToRemove, slotsToRemove);
                inventory.SlotCount = ItemSlots;

                Utils.Logger.Debug("DealerManager", $"Removed item slots from {Dealer.fullName}: {slotsToRemove} ");
            }

            if (Dealer.Cut != Cut)
            {
                Dealer.Cut = Cut;

                Utils.Logger.Debug("DealerManager", $"Cut for {Dealer.fullName} set: {Cut}");
            }

            if (Dealer.Movement.SpeedController.SpeedMultiplier != SpeedMultiplier)
            {
                Dealer.Movement.SpeedController.SpeedMultiplier = SpeedMultiplier;

                Utils.Logger.Debug("DealerManager", $"Speed multiplier for {Dealer.fullName} set: {SpeedMultiplier}");
            }
        }

        private void OnMinPassed()
        {
            if (IsFired)
            {
                return;
            }

            if (SyncManager.IsNoSyncOrActiveAndHost && Schedule != null && !Schedule.IsEnabled)
            {
                Schedule.Start();
            }

            if (HasChanged)
            {
                HasChanged = false;

                UpdateDealer();
            }
        }

        private void OnDayPassed()
        {
            if (DaysUntilNextNegotiation > 0)
            {
                DaysUntilNextNegotiation--;
            }
        }
    }
}
