using AdvancedDealing.Economy;
using AdvancedDealing.Persistence.Datas;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using AdvancedDealing.Persistence;


#if IL2CPP
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class SettingsPopup
    {
        public GameObject Container;

        public Text TitleLabel;

        public Button ApplyButton;

        public Transform Content;

        private readonly List<GameObject> _inputFields = [];

        private DealerExtension _dealer;

        private GameObject _inputFieldTemplate;

        public bool UICreated { get; private set; }

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
                Close();
            }
        }

        public void Open(DealerExtension dealerExtension)
        {
            IsOpen = true;
            _dealer = dealerExtension;

            Container.SetActive(true);
            TitleLabel.text = $"Adjust Settings ({_dealer.Dealer.name})";

            foreach (GameObject field in _inputFields)
            {
                field.GetComponent<InputField>().text = GetDataValue(field.name);
                field.SetActive(true);
            }

            if (ModConfig.LoyalityMode)
            {
                GameObject speedMultiplierField = _inputFields.Find(x => x.name == "SpeedMultiplier");
                speedMultiplierField?.SetActive(false);

                GameObject maxCustomersField = _inputFields.Find(x => x.name == "MaxCustomers");
                maxCustomersField?.SetActive(false);
            }
        }

        public void Close()
        {
            IsOpen = false;
            Container.SetActive(false);
        }

        private void OnApply()
        {
            if (!IsOpen) return;

            bool updated = false;

            foreach (GameObject field in _inputFields)
            {
                InputField input = field.GetComponent<InputField>();
                string oldValue = GetDataValue(field.name);
                string value = input.text;

                if (value != oldValue)
                {
                    if (input.contentType == InputField.ContentType.IntegerNumber)
                    {
                        typeof(DealerExtension).GetField(field.name).SetValue(_dealer, int.Parse(value));
                    }
                    else if (input.contentType == InputField.ContentType.DecimalNumber)
                    {
                        typeof(DealerExtension).GetField(field.name).SetValue(_dealer, float.Parse(value));
                    }

                    updated = true;
                }
            }

            if (updated)
            {
                if (NetworkSynchronizer.IsSyncing)
                {
                    NetworkSynchronizer.Instance.SendData(_dealer.FetchData());
                }

                _dealer.HasChanged = true;
                _dealer.SendPlayerMessage("Damn.. please change your behavior!");
                _dealer.SendMessage($"Hmkay .. i'm sorry", false, true, 2f);
            }

            Close();
        }

        private void OnCancel()
        {
            Close();
        }

        public void BuildUI()
        {
            if (UICreated) return;

            GameObject target = PlayerSingleton<MessagesApp>.Instance.ConfirmationPopup.gameObject;

            Container = Object.Instantiate(target, target.transform.parent);
            Container.name = "SettingsPopup";
            Container.SetActive(true);

            CreateInputFieldTemplate();

            Content = Container.transform.Find("Shade/Content");
            Content.GetComponent<RectTransform>().sizeDelta = new Vector2(-160f, 100f);
            Object.Destroy(Content.Find("Subtitle").gameObject);

            TitleLabel = Content.Find("Title").GetComponent<Text>();
            TitleLabel.text = "Adjust Settings";

            Button[] buttons = Content.GetComponentsInChildren<Button>();
            buttons[0].onClick.RemoveAllListeners();
            buttons[0].onClick.AddListener((UnityAction)OnCancel);

            ApplyButton = buttons[2];
            ApplyButton.gameObject.name = "Apply";
            ApplyButton.GetComponentInChildren<Text>().text = "Apply";
            ApplyButton.colors = new()
            {
                normalColor = new Color(0.2941f, 0.6863f, 0.8824f, 1f),
                highlightedColor = new Color(0.4532f, 0.7611f, 0.9151f, 1f),
                pressedColor = new Color(0.5674f, 0.8306f, 0.9623f, 1f),
                selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1),
                disabledColor = new Color(0.2941f, 0.6863f, 0.8824f, 1),
                colorMultiplier = 1f,
                fadeDuration = 0f,
            };
            ApplyButton.onClick.RemoveAllListeners();
            ApplyButton.onClick.AddListener((UnityAction)OnApply);
            ApplyButton.GetComponent<Image>().color = Color.white;

            Object.Destroy(buttons[1].gameObject);

            CreateInputField(InputField.ContentType.IntegerNumber, "MaxCustomers", "Max Customers", 0, 24);
            CreateInputField(InputField.ContentType.IntegerNumber, "ItemSlots", "Item Slots", 0, 20);
            CreateInputField(InputField.ContentType.DecimalNumber, "Cut", "Cut %", 0, 1);
            CreateInputField(InputField.ContentType.DecimalNumber, "SpeedMultiplier", "Speed Multiplier", 0, 0);

            Utils.Logger.Debug("SettingsPopup", "Settings popup UI created");

            UICreated = true;
        }

        private void CreateInputFieldTemplate()
        {
            GameObject template = Object.Instantiate(PlayerSingleton<MessagesApp>.Instance.CounterofferInterface.transform.Find("Shade/Content/Selection/SearchInput").gameObject);
            template.SetActive(false);
            template.name = "InputFieldUITemplate";
            template.GetComponent<InputField>().onEndEdit.RemoveAllListeners();

            RectTransform transform = template.GetComponent<RectTransform>();
            transform.offsetMax = new Vector2(-20f, -100f);
            transform.offsetMin = new Vector2(20f, -160f);

            RectTransform image = transform.Find("Image").GetComponent<RectTransform>();
            image.offsetMin = new Vector2(350f, image.offsetMin.y);

            RectTransform textArea = transform.Find("Text Area").GetComponent<RectTransform>();
            textArea.offsetMin = new Vector2(350f, textArea.offsetMin.y);
            Text placeholder = textArea.Find("Placeholder").GetComponent<Text>();
            placeholder.text = "Set value...";

            RectTransform title = new GameObject("Title").AddComponent<RectTransform>();
            title.SetParent(transform);
            title.SetAsFirstSibling();
            title.sizeDelta = new Vector2(-358f, -13f);
            title.offsetMax = new Vector2(-150f, -10f);
            title.offsetMin = new Vector2(10f, 8f);
            title.anchorMax = new Vector2(1f, 1f);
            title.anchorMin = new Vector2(0f, 0f);

            Text text = title.gameObject.AddComponent<Text>();
            text.font = placeholder.font;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "Title";
            text.color = Color.black;
            text.fontSize = 20;

            _inputFieldTemplate = template;
        }

        private void CreateInputField(InputField.ContentType type, string key, string description, float rangeMin = 0, float rangeMax = 0)
        {
            GameObject field = Object.Instantiate(_inputFieldTemplate, Content);
            field.SetActive(true);
            field.name = key;
            
            float offset = 0f;
            if (_inputFields.Count > 0)
            {
                offset = 80f * _inputFields.Count;
            }
            
            RectTransform transform2 = field.GetComponent<RectTransform>();
            transform2.offsetMax = new Vector2(transform2.offsetMax.x, transform2.offsetMax.y - offset);
            transform2.offsetMin = new Vector2(transform2.offsetMin.x, transform2.offsetMin.y - offset);
            transform2.Find("Title").GetComponent<Text>().text = description;
            
            InputField input = field.GetComponent<InputField>();
            input.contentType = type;

            if (rangeMin != 0 || rangeMax != 0)
            {
                input.onEndEdit.AddListener((UnityAction<string>)ValidateRange);
            }
            
            void ValidateRange(string text)
            {
                if (text.Length > 0)
                {
                    float value = float.Parse(text);
                    if (!(rangeMin <= value && value <= rangeMax))
                    {
                        input.text = GetDataValue(key);
                    }
                }
            }

            _inputFields.Add(field);
        }

        private string GetDataValue(string key)
        {
            if (_dealer == null)
            {
                return null;
            }

            return typeof(DealerExtension).GetField(key).GetValue(_dealer).ToString();
        }
    }
}
