using AdvancedDealing.Messaging;
using AdvancedDealing.NPCs;
using AdvancedDealing.NPCs.Actions;
using AdvancedDealing.Persistence;
using AdvancedDealing.Persistence.Datas;
using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedDealing.Messaging.Messages;


#if IL2CPP
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
#elif MONO
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.Economy
{
    public class DealerManager
    {
        private readonly Dealer _dealer;

        public DealerData DealerData { get; private set; }

        public readonly ScheduleManager Schedule;

        public readonly ConversationManager Conversation;

        public Dealer ManagedDealer => _dealer;

        // public ConversationManager Conversation { get; private set; }

        private static readonly List<DealerManager> _cache = [];

        public static readonly List<string> ValidDealers =
        [
            "Benji",
            "Molly",
            "Brad",
            "Jane",
            "Wei",
            "Leo"
        ];

        private DealerManager(Dealer dealer)
        {
            _dealer = dealer;

            DealerData dealerData = SaveManager.Instance.SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.name));

            if (dealerData == null)
            {
                dealerData = new(dealer.GUID.ToString());
                dealerData.SetDefaults();
                SaveManager.Instance.SaveData.Dealers.Add(dealerData);
            }

            ScheduleManager schedule = new(dealer);
            schedule.AddAction(new NPCSignal_DeliverCash(dealer));

            ConversationManager conversation = new(dealer);
            conversation.AddMessage(new Message_EnableDeliverCash(this));
            conversation.AddMessage(new Message_DisableDeliverCash(this));

            DealerData = dealerData;
            Schedule = schedule;
            Conversation = conversation;
        }

        public static void SetData(Dealer dealer, DealerData dealerData)
        {
            if (!DealerExists(dealer)) return;

            DealerManager manager = GetManager(dealer);
            manager.DealerData = dealerData;
        }

        public static DealerManager GetManager(Dealer dealer)
        {
            return _cache.Find(x => x._dealer == dealer);
        }

        public static DealerManager GetManager(string dealerGuid)
        {
            return _cache.Find(x => x._dealer.GUID.ToString().Contains(dealerGuid));
        }

        public static void AddDealer(Dealer dealer)
        {
            if (!dealer.IsRecruited || !IsValid(dealer) || DealerExists(dealer)) return;

            DealerManager manager = new(dealer);
            _cache.Add(manager);

            Update(manager._dealer, SyncManager.NoSyncOrActiveAndHost);

            Utils.Logger.Debug("DealerManager", $"Dealer added: {dealer.GUID}");
        }

        public static Dealer GetDealer(string dealerGuid)
        {
            DealerManager manager = GetManager(dealerGuid);

            if (manager == null)
            {
                Utils.Logger.Debug("DealerManager", $"Could not find dealer: {dealerGuid}");

                return null;
            }

            return manager._dealer;
        }

        public static List<Dealer> GetAllDealers()
        {
            List<Dealer> dealers = [];

            foreach (DealerManager manager in _cache)
            {
                dealers.Add(manager._dealer);
            }

            return dealers;
        }

        public static bool DealerExists(string dealerGuid)
        {
            return _cache.Any(x => x._dealer.GUID.ToString().Contains(dealerGuid));
        }

        public static bool DealerExists(Dealer dealer)
        {
            return _cache.Any(x => x._dealer == dealer);
        }

        private static bool IsValid(Dealer dealer)
        {
            return ValidDealers.Any(s => s.Contains(dealer.name));
        }

        public static void Update(Dealer dealer, bool shouldSync = true)
        {
            if (!DealerExists(dealer)) return;

            DealerManager manager = GetManager(dealer);

            // Maybe start schedule
            if (SyncManager.NoSyncOrActiveAndHost && manager.Schedule != null && !manager.Schedule.IsEnabled)
            {
                manager.Schedule.Start();
            }

            SetItemSlots(dealer, manager.DealerData.ItemSlots);
            SetCut(dealer, manager.DealerData.Cut);
            SetSpeedMultiplier(dealer, manager.DealerData.SpeedMultiplier);

            if (shouldSync)
            {
                SyncManager.Instance.PushUpdate();
            }
        }

        public static void SetCut(Dealer dealer, float cut)
        {
            float currentCut = dealer.Cut;

            if (!DealerExists(dealer) || currentCut == cut) return;

            try
            {
                dealer.Cut = cut;

                DealerManager manager = GetManager(dealer);
                manager.DealerData.Cut = cut;

                Utils.Logger.Debug("DealerManager", $"Cut for {dealer.name} set: {cut}");
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("DealerManager", $"Could not set cut for {dealer.name}", ex);
            }
        }

        public static void SetSpeedMultiplier(Dealer dealer, float multiplier)
        {
            NPCSpeedController speedController = dealer.Movement.SpeedController;
            float currentMultiplier = speedController.SpeedMultiplier;

            if (!DealerExists(dealer) || currentMultiplier == multiplier) return;

            try
            {
                speedController.SpeedMultiplier = multiplier;

                DealerManager manager = GetManager(dealer);
                manager.DealerData.SpeedMultiplier = multiplier;

                Utils.Logger.Debug("DealerManager", $"Speed multiplier for {dealer.name} set: {multiplier}");
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("DealerManager", $"Could not set speed multiplier for {dealer.name}", ex);
            }
        }

        public static void SetItemSlots(Dealer dealer, int slots)
        {
            NPCInventory inventory = dealer.Inventory;
            int currentSlots = inventory.ItemSlots.Count;

            if (!DealerExists(dealer) || currentSlots == slots) return;

            try
            {
                if (currentSlots < slots)
                {
                    int slotsToAdd = slots - currentSlots;

                    for (int i = 0; i < slotsToAdd; i++)
                    {
                        inventory.ItemSlots.Add(new ItemSlot());
                    }

                    Utils.Logger.Debug("DealerManager", $"Added item slots to {dealer.name}: {slotsToAdd} ");
                }
                else
                {
                    int slotsToRemove = currentSlots - slots;

                    inventory.ItemSlots.RemoveRange(currentSlots - slotsToRemove, slotsToRemove);

                    Utils.Logger.Debug("DealerManager", $"Removed item slots from {dealer.name}: {slotsToRemove} ");
                }

                DealerManager manager = GetManager(dealer);
                manager.DealerData.ItemSlots = slots;
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("DealerManager", $"Could not set item slots for {dealer.name}", ex);
            }
        }

        public static void SetDeadDrop(Dealer dealer, string guid)
        {
            if (!DealerExists(dealer)) return;

            DealerManager manager = GetManager(dealer);
            string currentDeadDrop = manager.DealerData.DeadDrop;

            if (currentDeadDrop == guid) return;

            if (guid != null)
            {
                DeadDrop deadDrop = DeadDropManager.GetDeadDrop(guid);
                if (deadDrop == null)
                {
                    guid = null;
                }
            }

            manager.DealerData.DeadDrop = guid;

            Utils.Logger.Debug("DealerManager", $"Dead drop for {dealer.name} set: {guid}");
        }

        public static DeadDrop GetDeadDrop(Dealer dealer)
        {
            if (!DealerExists(dealer))
            {
                return null;
            }

            DealerManager manager = GetManager(dealer);
            string guid = manager.DealerData.DeadDrop;

            if (guid == null)
            {
                return null;
            }
            else
            {
                return DeadDropManager.GetDeadDrop(guid);
            }
        }

        public void SendMessage(string text, bool notify = true, bool network = true)
        {
            SendMessage(_dealer, text, notify, network);
        }

        public static void SendMessage(Dealer dealer, string text, bool notify = true, bool network = true)
        {
            Message msg = new(text, Message.ESenderType.Other);

            dealer.MSGConversation.SendMessage(msg, notify, network);
        }

        public static void Load()
        {
            _cache.Clear();

            foreach (Dealer dealer in Dealer.AllPlayerDealers)
            {
                AddDealer(dealer);
            }

            Dealer.onDealerRecruited -= new Action<Dealer>(OnDealerRecruited);
            Dealer.onDealerRecruited += new Action<Dealer>(OnDealerRecruited);
        }

        private static void OnDealerRecruited(Dealer dealer)
        {
            Utils.Logger.Debug("Recruited");
            AddDealer(dealer);
        }
    }
}
