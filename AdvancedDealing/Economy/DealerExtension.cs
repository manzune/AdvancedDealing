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

#if IL2CPP
using Il2CppGameKit.Utilities;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.GameTime;
#elif MONO
using GameKit.Utilities;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;
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

        public Schedule Schedule;

        public Conversation Conversation;

        public string DeadDrop;

        public bool IsFired;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        public float Loyality;

        public bool DeliverCash;

        public bool PickupProducts;

        public float CashThreshold;

        public int ProductThreshold;

        public int DaysUntilNextNegotiation;

        public int DailyContractCount;

        public bool HasChanged;

        public DealerExtension(Dealer dealer)
        {
            Dealer = dealer;
            DealerData dealerData = SaveModifier.Instance.SaveData.Dealers.Find(x => x.Identifier.Contains(dealer.GUID.ToString()));

            if (dealerData == null)
            {
                dealerData = new(dealer.GUID.ToString());
                dealerData.LoadDefaults();
                SaveModifier.Instance.SaveData.Dealers.Add(dealerData);
            }
            else if (dealerData.ModVersion != ModInfo.Version)
            {
                DealerData oldDealerData = dealerData;
                dealerData = new(oldDealerData.Identifier);
                dealerData.LoadDefaults();
                dealerData.Merge(oldDealerData);

                Utils.Logger.Debug("DealerExtension", $"Data for {Dealer.fullName} merged to newer Version");
            }

            PatchData(dealerData);
            Update();
            Awake();
        }

        public static DealerExtension GetDealer(Dealer dealer, bool includeFired = false) => GetDealer(dealer.GUID.ToString(), includeFired);

        public static DealerExtension GetDealer(string guid, bool includeFired = false)
        {
            if (guid == null)
            {
                return null;
            }

            DealerExtension dealer = cache.Find(x => x.Dealer.GUID.ToString().Contains(guid));

            if (!includeFired && dealer.IsFired)
            {
                return null;
            }

            return dealer;
        }

        public static List<DealerExtension> GetAllDealers()
        {
            return cache;
        }

        public static void AddDealer(Dealer dealer)
        {
            if (dealer.IsRecruited && dealer.DealerType == EDealerType.PlayerDealer)
            {
                if (!DealerExists(dealer))
                {
                    cache.Add(new(dealer));
                    Utils.Logger.Debug("DealerExtension", $"Extension for dealer created: {dealer.fullName}");
                }
                else
                {
                    DealerExtension dealer2 = GetDealer(dealer, true);
                    if (dealer2.IsFired)
                    {
                        dealer2.IsFired = false;
                        dealer2.Awake();
                        Utils.Logger.Debug("DealerExtension", $"Extension for dealer resumed: {dealer.fullName}");
                    }
                }
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

        public static void Initialize()
        {
            Dealer.onDealerRecruited -= new Action<Dealer>(AddDealer);
            Dealer.onDealerRecruited += new Action<Dealer>(AddDealer);
        }

        public static List<DealerData> FetchAllDealerDatas()
        {
            List<DealerData> dataCollection = [];

            foreach (DealerExtension dealer in cache)
            {
                dataCollection.Add(dealer.FetchData());
            }

            return dataCollection;
        }

        public void Destroy(bool clearCache = true)
        {
            NetworkSingleton<TimeManager>.Instance?.onMinutePass -= new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance?.onSleepStart -= new Action(OnSleepStart);

            Schedule?.Destroy();
            Conversation?.Destroy();

            if (clearCache)
            {
                cache.Remove(this);
            }
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
            DealerData oldData = FetchData();

            if (!oldData.IsEqual(data))
            {
                FieldInfo[] fields = typeof(DealerData).GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo localField = GetType().GetField(fields[i].Name);
                    localField?.SetValue(this, fields[i].GetValue(data));
                }

                Utils.Logger.Debug("DealerExtension", $"Data for {Dealer.fullName} patched");
            }

            // Loyality Mode
            if (ModConfig.LoyalityMode)
            {
                SetLoyalityStats();
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

            Dealer.DealerPoI?.enabled = false;

            if (Dealer.DialogueController != null)
            {
                Dealer.DialogueController.GreetingOverrides.Clear();

                foreach (var choice in Dealer.DialogueController.Choices)
                {
                    if (!choice.Enabled)
                    {
                        choice.Enabled = true;
                    }
                    else if (choice.ChoiceText != "Nevermind")
                    {

                        choice.Enabled = false;
                    }
                }
            }

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

            Dealer.HasChanged = true;

            if (NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendMessage("dealer_fired", Dealer.GUID.ToString());
            }

            Destroy(false);

            Utils.Logger.Debug("DealerExtension", $"Dealer fired: {Dealer.fullName}");
        }

        public Dictionary<ProductItemInstance, ItemSlot> GetAllProducts(out int totalAmount)
        {
            Dictionary<ProductItemInstance, ItemSlot> products = [];
            totalAmount = 0;

            foreach (ItemSlot slot in Dealer.GetAllSlots())
            {
                if (slot.ItemInstance != null && slot.ItemInstance.Category == EItemCategory.Product)
                {
#if IL2CPP
                    ProductItemInstance product = slot.ItemInstance.Cast<ProductItemInstance>();
#elif MONO
                    ProductItemInstance product = slot.ItemInstance as ProductItemInstance;
#endif
                    products.Add(product, slot);
                    totalAmount += product.Quantity * product.Amount;
                }
            }

            return products;
        }

        public bool IsInventoryFull(out int freeSlots)
        {
            freeSlots = 0;

            foreach (ItemSlot slot in Dealer.Inventory.ItemSlots)
            {
                if (slot.ItemInstance == null)
                {
                    freeSlots++;
                }
            }

            if (freeSlots > 0)
            {
                return false;
            }

            return true;
        }

        public void ChangeLoyality(float amount)
        {
            float newLoyality = Loyality + amount;
            
            if (newLoyality >= 100f)
            {
                Loyality = 100f;
            }
            else if (newLoyality <= 0f)
            {
                Loyality = 0f;
            }
            else
            {
                Loyality = newLoyality;
            }

            Utils.Logger.Debug("DealerExtension", $"Loyality for {Dealer.fullName} changed: {newLoyality}");

            SetLoyalityStats();

            if (NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendData(FetchData());
            }
        }

        private void SetLoyalityStats()
        {
            int maxCustomers;
            float speedMultiplier;

            if (Loyality <= 19)
            {
                maxCustomers = 4;
                speedMultiplier = 0.6f;
            }
            else if (Loyality <= 39)
            {
                maxCustomers = 6;
                speedMultiplier = 0.8f;
            }
            else if (Loyality <= 59)
            {
                maxCustomers = 8;
                speedMultiplier = 1f;
            }
            else if (Loyality <= 79)
            {
                maxCustomers = 12;
                speedMultiplier = 1.3f;
            }
            else
            {
                maxCustomers = 16;
                speedMultiplier = 1.6f;
            }

            if (Dealer.AssignedCustomers.Count > maxCustomers)
            {
                for (int i = Dealer.AssignedCustomers.Count - 1; i >= 0; i--)
                {
                    if (i + 1 <= maxCustomers)
                    {
                        break;
                    }

                    Dealer.RemoveCustomer(Dealer.AssignedCustomers[i]);
                }
            }

            MaxCustomers = maxCustomers;
            SpeedMultiplier = speedMultiplier;
            HasChanged = true;

            Utils.Logger.Debug("DealerExtension", $"Loyality stats for {Dealer.fullName} updated");
        }

        private void Awake()
        {
            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(OnMinPassed);
            NetworkSingleton<TimeManager>.Instance.onSleepStart += new Action(OnSleepStart);

            Schedule = new(Dealer);
            Schedule.AddAction(new DeliverCashAction(this));
            Schedule.AddAction(new PickupProductsAction(this));

            Conversation = new(Dealer);
            Conversation.AddSendableMessage(new EnableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new DisableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new EnableProductPickupMessage(this));
            Conversation.AddSendableMessage(new DisableProductPickupMessage(this));
            Conversation.AddSendableMessage(new AccessInventoryMessage(this));
            Conversation.AddSendableMessage(new PayBonusMessage(this));
            Conversation.AddSendableMessage(new NegotiateCutMessage(this));
            Conversation.AddSendableMessage(new AdjustSettingsMessage(this));
            Conversation.AddSendableMessage(new FiredMessage(this));
        }

        private void Update()
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

                Update();
            }
        }

        private void OnSleepStart()
        {
            if (DaysUntilNextNegotiation > 0)
            {
                DaysUntilNextNegotiation--;
            }

            // Loyality Mode
            if (ModConfig.LoyalityMode && NetworkSynchronizer.IsNoSyncOrHost)
            {
                StartRandomLoyalityAction();
            }

            DailyContractCount = 0;
        }

        private void StartRandomLoyalityAction()
        {
            List<ActionBase> actions = [];

            if (Loyality.InRange(0, 10))
            {
                actions.Add(new ResignAction(this));
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
                int i = UnityEngine.Random.Range(0, actions.Count);

                Schedule.AddAction(actions[i]);
            }
        }
    }
}
