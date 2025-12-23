using AdvancedDealing.Messaging;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using AdvancedDealing.Economy;




#if IL2CPP
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.UI
{
    public class SettingsPopup
    {
        public GameObject Container;

        public Text TitleText;

        public Button ApplyButton;

        public InputField MaxCustomersField;

        public InputField ItemSlotsField;

        public InputField CutField;

        public InputField SpeedMultiplierField;

        private int _maxCustomersValue;

        private int _itemSlotsValue;

        private float _cutValue;

        private float _speedMultiplierValue;

        private ConversationManager _conversation;

        private MSGConversation _s1Conversation;

        private DealerManager _dealerManager;

        public bool IsOpen { get; private set; }

        public SettingsPopup()
        {
            GameInput.RegisterExitListener((GameInput.ExitDelegate)RightClick, 4);
        }

        private void RightClick(ExitAction action)
        {
            if (!action.Used && IsOpen)
            {
                action.Used = true;
                Exit();
            }
        }

        public void Open(ConversationManager conv, DealerManager dealerManager)
        {
            IsOpen = true;
            _conversation = conv;
            _s1Conversation = conv.conversation;
            _dealerManager = dealerManager;

            Container.SetActive(true);

            TitleText.text = $"Adjust Settings ({_dealerManager.ManagedDealer.name})";

            _maxCustomersValue = _dealerManager.DealerData.MaxCustomers;
            MaxCustomersField.text = _dealerManager.DealerData.MaxCustomers.ToString();
            _itemSlotsValue = _dealerManager.DealerData.ItemSlots;
            ItemSlotsField.text = _dealerManager.DealerData.ItemSlots.ToString();
            _cutValue = _dealerManager.DealerData.Cut;
            CutField.text = _dealerManager.DealerData.Cut.ToString();
            _speedMultiplierValue = _dealerManager.DealerData.SpeedMultiplier;
            SpeedMultiplierField.text = _dealerManager.DealerData.SpeedMultiplier.ToString();
        }

        public void Close()
        {
            IsOpen = false;
            Container.SetActive(false);
        }

        public void Apply()
        {
            if (!IsOpen) return;

            bool updated = false;

            if (MaxCustomersField.text != null || MaxCustomersField.text != "")
            {
                int newValue = int.Parse(MaxCustomersField.text);

                if (newValue != _dealerManager.DealerData.MaxCustomers)
                {
                    _dealerManager.DealerData.MaxCustomers = newValue;
                    updated = true;
                }
            }
            if (ItemSlotsField.text != null || ItemSlotsField.text != "")
            {
                int newValue = int.Parse(ItemSlotsField.text);

                if (newValue != _dealerManager.DealerData.ItemSlots)
                {
                    _dealerManager.DealerData.ItemSlots = newValue;
                    updated = true;
                }
            }
            if (CutField.text != null || CutField.text != "")
            {
                float newValue = float.Parse(CutField.text);

                if (newValue != _dealerManager.DealerData.Cut)
                {
                    _dealerManager.DealerData.Cut = newValue;
                    updated = true;
                }
            }
            if (SpeedMultiplierField.text != null || SpeedMultiplierField.text != "")
            {
                float newValue = float.Parse(SpeedMultiplierField.text);

                if (newValue != _dealerManager.DealerData.SpeedMultiplier)
                {
                    _dealerManager.DealerData.SpeedMultiplier = newValue;
                    updated = true;
                }
            }

            if (updated)
            {
                DealerManager.Update(_dealerManager.ManagedDealer, true);
                _dealerManager.SendMessage($"True story! I will change my behavior ...", false, true, 0.5f);
            }

            Close();
        }

        public void Exit()
        {
            Close();
        }

        public string GetOldValue(string key)
        {
            string value = null;

            switch (key)
            {
                case "MaxCustomers":
                    value = _maxCustomersValue.ToString();
                    break;
                case "ItemSlots":
                    value = _itemSlotsValue.ToString();
                    break;
                case "Cut":
                    value = _cutValue.ToString();
                    break;
                case "SpeedMultiplier":
                    value = _speedMultiplierValue.ToString();
                    break;
            }

            return value;
        }
    }
}
