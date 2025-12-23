using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppScheduleOne.DevUtilities;
using System;
using Object = UnityEngine.Object;



#if IL2CPP
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class MessagesAppModification
    {
        public static SettingsPopup SettingsPopup { get; private set; }

        private static GameObject _inputFieldTemplate;

        public static void Create()
        {
            MessagesApp app = PlayerSingleton<MessagesApp>.Instance;
            if (SettingsPopup == null)
            {
                CreateSettingsPopup(app);
            }
        }

        public static void Clear()
        {

        }

        private static void CreateSettingsPopup(MessagesApp app)
        {
            SettingsPopup = new();

            GameObject confirmationPopup = app.ConfirmationPopup.gameObject;
            GameObject settingsPopup = Object.Instantiate(confirmationPopup, confirmationPopup.transform.parent);

            SettingsPopup.Container = settingsPopup;

            settingsPopup.name = "SettingsPopup";
            settingsPopup.SetActive(false);

            // Title
            GameObject title = settingsPopup.transform.Find("Shade/Content/Title").gameObject;

            Text titleText = title.GetComponent<Text>();
            titleText.text = "Adjust Settings";

            SettingsPopup.TitleText = titleText;

            // Buttons
            GameObject cancel = settingsPopup.transform.Find("Shade/Content/Cancel").gameObject;
            cancel.name = "Exit";

            Button cancelButton = cancel.GetComponent<Button>();
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener((UnityAction)SettingsPopup.Exit);

            Object.Destroy(settingsPopup.transform.Find("Shade/Content/Send").gameObject);

            GameObject apply = settingsPopup.transform.Find("Shade/Content/Cancel").gameObject;
            apply.name = "Apply";

            Text applyText = apply.GetComponentInChildren<Text>();
            applyText.text = "Apply";

            Button applyButton = apply.GetComponent<Button>();
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener((UnityAction)SettingsPopup.Apply);
            applyButton.colors = new()
            {
                m_NormalColor = new Color(0.2941f, 0.6863f, 0.8824f, 1f),
                m_HighlightedColor = new Color(0.4532f, 0.7611f, 0.9151f, 1f),
                m_PressedColor = new Color(0.5674f, 0.8306f, 0.9623f, 1f),
                m_SelectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1),
                m_DisabledColor = new Color(0.2941f, 0.6863f, 0.8824f, 1),
                m_ColorMultiplier = 1f,
                m_FadeDuration = 0f
            };

            SettingsPopup.ApplyButton = applyButton;

            // Content
            GameObject content = settingsPopup.transform.Find("Shade/Content").gameObject;

            Object.Destroy(content.transform.Find("Subtitle").gameObject);

            RectTransform contentTransform = content.GetComponent<RectTransform>();
            contentTransform.sizeDelta = new Vector2(-160f, 100f);

            CreateInputFieldTemplate(app);

            SettingsPopup.MaxCustomersField = CreateInputField("MaxCustomers", "Max Customers (8 - 24)", contentTransform, InputField.ContentType.IntegerNumber, 0, 8, 24);
            SettingsPopup.ItemSlotsField = CreateInputField("ItemSlots", "Item Slots (5 - 20)", contentTransform, InputField.ContentType.IntegerNumber, 1, 5, 20);
            SettingsPopup.CutField = CreateInputField("Cut", "Cut (0 - 1)", contentTransform, InputField.ContentType.DecimalNumber, 2, 0, 1);
            SettingsPopup.SpeedMultiplierField = CreateInputField("SpeedMultiplier", "Speed Multiplier (1 - 10)", contentTransform, InputField.ContentType.DecimalNumber, 3, 1, 10);

            Object.Destroy(_inputFieldTemplate);
        }

        private static void CreateInputFieldTemplate(MessagesApp app)
        {
            GameObject counterOfferInterface = app.CounterofferInterface.gameObject;

            GameObject template = Object.Instantiate(counterOfferInterface.transform.Find("Shade/Content/Selection/SearchInput").gameObject);
            template.name = "InputFieldTemplate";

            GameObject image = template.transform.Find("Image").gameObject;

            RectTransform imageTransform = image.GetComponent<RectTransform>();
            imageTransform.offsetMin = new Vector2(350f, imageTransform.offsetMin.y);

            InputField inputField = template.GetComponent<InputField>();
            inputField.name = "Value";
            inputField.onEndEdit.RemoveAllListeners();

            RectTransform transform = template.GetComponent<RectTransform>();
            transform.offsetMax = new Vector2(-20f, -100f);
            transform.offsetMin = new Vector2(20f, -160f);

            GameObject textArea = template.transform.Find("Text Area").gameObject;

            RectTransform textAreaTransform = textArea.GetComponent<RectTransform>();
            textAreaTransform.offsetMin = new Vector2(350f, textAreaTransform.offsetMin.y);

            GameObject placeholder = textArea.transform.Find("Placeholder").gameObject;

            Text placeholderText = placeholder.GetComponent<Text>();
            placeholderText.text = "Set value...";
            
            GameObject title = new GameObject("Title");
            title.transform.SetParent(template.transform);
            title.transform.SetAsFirstSibling();

            RectTransform titleTransform = title.AddComponent<RectTransform>();
            titleTransform.sizeDelta = new Vector2(-358f, -13f);
            titleTransform.offsetMax = new Vector2(-150f, -10f);
            titleTransform.offsetMin = new Vector2(10f, 8f);
            titleTransform.anchorMax = new Vector2(1f, 1f);
            titleTransform.anchorMin = new Vector2(0f, 0f);

            Text titleText = title.AddComponent<Text>();
            titleText.font = placeholderText.font;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = "Title";
            titleText.color = Color.black;
            titleText.fontSize = 20;

            _inputFieldTemplate = template;
        }

        private static InputField CreateInputField(string name, string description, Transform parent, InputField.ContentType type, int index = 0, float rangeMin = 0, float rangeMax = 0)
        {
            float offset = 80f * index;
            if (index == 0)
            {
                offset = 0f;
            }

            GameObject field = Object.Instantiate(_inputFieldTemplate, parent);
            field.name = name;
            field.SetActive(true);

            RectTransform fieldTransform = field.GetComponent<RectTransform>();
            fieldTransform.offsetMax = new Vector2(fieldTransform.offsetMax.x, fieldTransform.offsetMax.y - offset);
            fieldTransform.offsetMin = new Vector2(fieldTransform.offsetMin.x, fieldTransform.offsetMin.y - offset);
            
            GameObject title = field.transform.Find("Title").gameObject;

            Text titleText = title.GetComponent<Text>();
            titleText.text = description;

            InputField inputField = field.GetComponent<InputField>();
            inputField.contentType = type;
            if (rangeMin != 0 || rangeMax != 0)
            {
                if (type == InputField.ContentType.IntegerNumber)
                {
                    inputField.onEndEdit.AddListener(new Action<string>(CheckIntRange));
                }
                else if (type == InputField.ContentType.DecimalNumber)
                {
                    inputField.onEndEdit.AddListener(new Action<string>(CheckFloatRange));
                }
            }           

            return inputField;

            void CheckIntRange(string input)
            {
                if (input != null || input != "")
                {
                    int value = int.Parse(input);

                    if (!(rangeMin <= value && value <= rangeMax))
                    {
                        inputField.text = SettingsPopup.GetOldValue(name);
                    }
                }
            }

            void CheckFloatRange(string input)
            {
                if (input != null || input != "")
                {
                    float value = float.Parse(input);

                    if (!(rangeMin <= value && value <= rangeMax))
                    {
                        inputField.text = SettingsPopup.GetOldValue(name);
                    }
                }
            }
        }
    }
}
