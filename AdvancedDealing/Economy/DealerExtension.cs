using AdvancedDealing.Messaging;
using AdvancedDealing.Messaging.Messages;
using AdvancedDealing.Persistence;
using AdvancedDealing.Persistence.Datas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdvancedDealing.NPCs.Behaviour;
using AdvancedDealing.Utils;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.GameTime;
#elif MONO
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

        public Conversation Conversation;

        public string ProductDeadDrop;

        public string CashDeadDrop;

        public bool IsFired;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        public bool DeliverCash;

        public bool PickupProducts;

        public float CashThreshold;

        public int ProductThreshold;

        public int DaysUntilNextNegotiation;

        public bool HasChanged;

        private DealerBehaviour _activeBehaviour;

        private DeliverCashDealerBehaviour _deliverCashBehaviour;

        private PickupProductsDealerBehaviour _pickupProductsBehaviour;

        public bool HasActiveBehaviour => _activeBehaviour != null;

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
            else if (dealerData.ModVersion != ModInfo.VERSION)
            {
                DealerData oldDealerData = dealerData;
                dealerData = new(oldDealerData.Identifier);
                dealerData.LoadDefaults();
                dealerData.Merge(oldDealerData);

                Logger.Debug("DealerExtension", $"Data for {Dealer.fullName} merged to newer Version");
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
                    Logger.Debug("DealerExtension", $"Extension for dealer created: {dealer.fullName}");
                }
                else
                {
                    DealerExtension dealer2 = GetDealer(dealer, true);
                    if (dealer2.IsFired)
                    {
                        dealer2.IsFired = false;
                        dealer2.Awake();
                        Logger.Debug("DealerExtension", $"Extension for dealer resumed: {dealer.fullName}");
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
            NetworkSingleton<TimeManager>.Instance?.onTick -= new Action(OnTick);
            NetworkSingleton<TimeManager>.Instance?.onSleepStart -= new Action(OnSleepStart);

            Conversation?.Destroy();
            _activeBehaviour?.Disable();

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

                Logger.Debug("DealerExtension", $"Data for {Dealer.fullName} patched");
            }
        }

        public void SetActiveBehaviour(DealerBehaviour behaviour)
        {
            _activeBehaviour = behaviour;
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

            Logger.Debug("DealerExtension", $"Dealer fired: {Dealer.fullName}");
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

        private void Awake()
        {
            NetworkSingleton<TimeManager>.Instance.onTick += new Action(OnTick);
            NetworkSingleton<TimeManager>.Instance.onSleepStart += new Action(OnSleepStart);

            _deliverCashBehaviour = new(this);
            _pickupProductsBehaviour = new(this);

            Conversation = new(Dealer);
            Conversation.AddSendableMessage(new EnableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new DisableDeliverCashMessage(this));
            Conversation.AddSendableMessage(new EnableProductPickupMessage(this));
            Conversation.AddSendableMessage(new DisableProductPickupMessage(this));
            Conversation.AddSendableMessage(new AccessInventoryMessage(this));
            Conversation.AddSendableMessage(new NegotiateCutMessage(this));
            Conversation.AddSendableMessage(new AdjustSettingsMessage(this));
            Conversation.AddSendableMessage(new FiredMessage(this));
        }

        private void Update()
        {
            NPCInventory inventory = Dealer.Inventory;

            if (!ConflictChecker.DisableMoreItemSlots)
            {
                if (inventory.ItemSlots.Count < ItemSlots)
                {
                    int slotsToAdd = ItemSlots - inventory.ItemSlots.Count;

                    for (int i = 0; i < slotsToAdd; i++)
                    {
                        inventory.ItemSlots.Add(new());
                        inventory.SlotCount = ItemSlots;
                    }

                    Logger.Debug("DealerExtension", $"Added item slots to {Dealer.fullName}: {slotsToAdd} ");
                }
                else if (inventory.ItemSlots.Count > ItemSlots)
                {
                    int slotsToRemove = inventory.ItemSlots.Count - ItemSlots;

                    inventory.ItemSlots.RemoveRange(inventory.ItemSlots.Count - slotsToRemove, slotsToRemove);
                    inventory.SlotCount = ItemSlots;

                    Logger.Debug("DealerExtension", $"Removed item slots from {Dealer.fullName}: {slotsToRemove} ");
                }
            }            

            if (Dealer.Cut != Cut)
            {
                Dealer.Cut = Cut;

                Logger.Debug("DealerExtension", $"Cut for {Dealer.fullName} set: {Cut}");
            }

            if (Dealer.Movement.SpeedController.SpeedMultiplier != SpeedMultiplier)
            {
                Dealer.Movement.SpeedController.SpeedMultiplier = SpeedMultiplier;

                Logger.Debug("DealerExtension", $"Speed multiplier for {Dealer.fullName} set: {SpeedMultiplier}");
            }
        }

        private void OnTick()
        {
            if (IsFired)
            {
                return;
            }

            if (HasChanged)
            {
                HasChanged = false;

                Update();
            }

            GetAllProducts(out var totalAmount);

            if (NetworkSynchronizer.IsNoSyncOrHost)
            {
                if (!_pickupProductsBehaviour.IsEnabled && PickupProducts && totalAmount <= ProductThreshold && !IsInventoryFull(out var freeSlots) && freeSlots > 1)
                {
                    _pickupProductsBehaviour.Enable();
                }

                if (!_deliverCashBehaviour.IsEnabled && DeliverCash && Dealer.Cash >= CashThreshold && Dealer.Cash > 0f)
                {
                    _deliverCashBehaviour.Enable();
                }

                if (_activeBehaviour == null)
                {
                    if (_pickupProductsBehaviour.IsEnabled && ProductDeadDrop != null && TimeManager.Instance.CurrentTime != 400)
                    {
                        _pickupProductsBehaviour.Start();
                    }
                    else if (_deliverCashBehaviour.IsEnabled && CashDeadDrop != null && Dealer.ActiveContracts.Count <= 0 && TimeManager.Instance.CurrentTime != 400)
                    {
                        _deliverCashBehaviour.Start();
                    }
                }
                else if (_activeBehaviour.IsActive)
                {
                    _activeBehaviour.OnActiveTick();
                }
            }
        }

        private void OnSleepStart()
        {
            if (DaysUntilNextNegotiation > 0)
            {
                DaysUntilNextNegotiation--;
            }
        }
    }
}
