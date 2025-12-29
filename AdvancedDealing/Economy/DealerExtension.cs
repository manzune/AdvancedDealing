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
using GameKit.Utilities;



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
    public class DealerExtension
    {
        private static readonly List<DealerExtension> cache = [];

        public readonly Dealer Dealer;

        public readonly Schedule Schedule;

        public readonly Conversation Conversation;

        public string DeadDrop;

        public bool IsFired;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        public float Loyality;

        public bool DeliverCash;

        public bool NotifyOnCashDelivery;

        public float CashThreshold;

        public int DaysUntilNextNegotiation;

        public bool HasChanged;

        public DealerExtension(Dealer dealer)
        {
            Dealer = dealer;
            DealerDataContainer dealerData = SaveModifier.Instance.SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.GUID.ToString()));

            if (dealerData == null)
            {
                dealerData = new(dealer.GUID.ToString());
                dealerData.LoadDefaults();
                SaveModifier.Instance.SaveData.Dealers.Add(dealerData);
            }

            PatchData(dealerData);

            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance.onSleepStart += new Action(OnSleepStart);

            Schedule = new(dealer);
            Schedule.AddAction(new DeliverCashSignal(this));

            Conversation = new(dealer);
            Conversation.AddSendableMessage(new EnableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new DisableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new AccessInventoryMessage(this));
            Conversation.AddSendableMessage(new NegotiateCutMessage(this));
            Conversation.AddSendableMessage(new AdjustSettingsMessage(this));
            Conversation.AddSendableMessage(new FiredMessage(this));
        }

        public static List<DealerExtension> GetAllExtensions()
        {
            return cache;
        }

        public static DealerExtension GetExtension(Dealer dealer) => GetExtension(dealer.GUID.ToString());

        public static DealerExtension GetExtension(string guid)
        {
            if (guid == null)
            {
                return null;
            }

            return cache.Find(x => x.Dealer.GUID.ToString().Contains(guid));
        }

        public static void CreateExtension(Dealer dealer)
        {
            if (dealer.IsRecruited && Dealer.AllPlayerDealers.Contains(dealer) && !ExtensionExists(dealer))
            {
                cache.Add(new(dealer));

                Utils.Logger.Debug("DeadDropExtension", $"Extension created for dealer: {dealer.fullName}");
            }
        }

        public static bool ExtensionExists(Dealer dealer) => ExtensionExists(dealer.GUID.ToString());

        public static bool ExtensionExists(string guid)
        {
            if (guid == null)
            {
                return false;
            }

            return cache.Any(x => x.Dealer.GUID.ToString().Contains(guid));
        }

        public static void ExtendDealers()
        {
            for (int i = cache.Count - 1; i >= 0; i--)
            {
                cache[i].Destroy();
            }

            foreach (Dealer dealer in Dealer.AllPlayerDealers)
            {
                CreateExtension(dealer);
            }

            Dealer.onDealerRecruited -= new Action<Dealer>(OnDealerRecruited);
            Dealer.onDealerRecruited += new Action<Dealer>(OnDealerRecruited);
        }

        private static void OnDealerRecruited(Dealer dealer)
        {
            CreateExtension(dealer);
        }

        public void Destroy()
        {
            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance.onSleepStart = new Action(OnSleepStart);

            Schedule.Destroy();
            Conversation.Destroy();

            cache.Remove(this);
        }

        public DealerDataContainer FetchData()
        {
            DealerDataContainer data = new(Dealer.GUID.ToString());
            FieldInfo[] fields = typeof(DealerDataContainer).GetFields();

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

        public void PatchData(DealerDataContainer data)
        {
            DealerDataContainer oldData = FetchData();

            if (!oldData.IsEqual(data))
            {
                FieldInfo[] fields = typeof(DealerDataContainer).GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo localField = GetType().GetField(fields[i].Name);
                    localField?.SetValue(this, fields[i].GetValue(data));
                }

                if (NetworkSynchronizer.IsNoSyncOrHost && ModConfig.LoyalityMode)
                {
                    // loyality mode
                }

                Utils.Logger.Debug("DealerExtension", $"Data for {Dealer.fullName} patched");
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

        public void FireDealer()
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

            if (NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendMessage("dealer_fired", Dealer.GUID.ToString());
            }

            SendMessage("Hmpf okay, get in touch if you need me", false, true, 0.5f);
            SaveModifier.Instance.UpdateData(FetchData());
            Destroy();

            Utils.Logger.Debug("DealerExtension", $"Dealer fired: {Dealer.fullName}");
        }

        public void ChangeLoyality(float amount, bool shouldSync = false)
        {
            float newLoyality = Loyality + amount;
            
            if (newLoyality > 100)
            {
                Loyality = 100;
            }
            else if (newLoyality < 0)
            {
                Loyality = 0;
            }
            else
            {
                Loyality = newLoyality;
            }

            if (shouldSync && NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendData(FetchData());
            }
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

                Utils.Logger.Debug("DealerExtension", $"Added item slots to {Dealer.fullName}: {slotsToAdd} ");
            }
            else if (inventory.ItemSlots.Count > ItemSlots)
            {
                int slotsToRemove = inventory.ItemSlots.Count - ItemSlots;

                inventory.ItemSlots.RemoveRange(inventory.ItemSlots.Count - slotsToRemove, slotsToRemove);
                inventory.SlotCount = ItemSlots;

                Utils.Logger.Debug("DealerExtension", $"Removed item slots from {Dealer.fullName}: {slotsToRemove} ");
            }

            if (Dealer.Cut != Cut)
            {
                Dealer.Cut = Cut;

                Utils.Logger.Debug("DealerExtension", $"Cut for {Dealer.fullName} set: {Cut}");
            }

            if (Dealer.Movement.SpeedController.SpeedMultiplier != SpeedMultiplier)
            {
                Dealer.Movement.SpeedController.SpeedMultiplier = SpeedMultiplier;

                Utils.Logger.Debug("DealerExtension", $"Speed multiplier for {Dealer.fullName} set: {SpeedMultiplier}");
            }
        }

        private void OnMinPassed()
        {
            if (IsFired)
            {
                return;
            }

            if (NetworkSynchronizer.IsNoSyncOrHost && Schedule != null && !Schedule.IsEnabled)
            {
                Schedule.Start();
            }

            if (HasChanged)
            {
                HasChanged = false;

                UpdateDealer();
            }
        }

        private void OnSleepStart()
        {
            if (DaysUntilNextNegotiation > 0)
            {
                DaysUntilNextNegotiation--;
            }

            if (ModConfig.LoyalityMode && NetworkSynchronizer.IsNoSyncOrHost)
            {
                StartRandomLoyalityAction();
            }
        }

        private void StartRandomLoyalityAction()
        {
            List<ActionBase> actions = [];

            if (Loyality.InRange(0, 10))
            {
                actions.Add(new StealProductsAction(this, 40, 50));
                actions.Add(new StealCashAction(this, 40, 50));
            }
            else if (Loyality.InRange(11, 30))
            {
                actions.Add(new StealProductsAction(this, 25, 40));
                actions.Add(new StealCashAction(this, 25, 40));
            }
            else if (Loyality.InRange(31, 50))
            {
                actions.Add(new StealProductsAction(this, 10, 25));
                actions.Add(new StealCashAction(this, 10, 25));
            }
            else if (Loyality.InRange(51, 70))
            {
                actions.Add(new StealProductsAction(this, 3, 10));
                actions.Add(new StealCashAction(this, 3, 10));
            }
            else if (Loyality.InRange(71, 90))
            {
                actions.Add(new StealProductsAction(this, 1, 3));
                actions.Add(new StealCashAction(this, 1, 3));
            }

            if (actions.Count == 1)
            {
                Schedule.AddAction(actions[0]);
            }
            else if (actions.Count > 1)
            {
                int i = UnityEngine.Random.Range(0, actions.Count - 1);

                Schedule.AddAction(actions[i]);
            }
        }
    }
}
