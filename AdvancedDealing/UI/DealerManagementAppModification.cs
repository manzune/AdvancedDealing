using AdvancedDealing.Economy;
using AdvancedDealing.Persistence;
using AdvancedDealing.Persistence.Datas;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;

#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.Economy;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class DealerManagementAppModification
    {
        public static readonly List<GameObject> DeadDropEntries = [];

        public static readonly List<GameObject> CustomerEntries = [];

        public static GameObject CustomersScrollView { get; private set; }

        public static GameObject DeadDropSelector { get; private set; }

        public static GameObject DeadDropSelectorButton { get; private set; }

        public static void Create(DealerManagementApp app)
        {
            if (CustomersScrollView == null)
            {
                CreateCustomersScrollView(app);
            }

            if (DeadDropSelector == null)
            {
                CreateDeadDropSelector(app);
            }

            if (DeadDropSelectorButton == null)
            {
                CreateDeadDropSelectorButton(app);
            }

            if (CustomerEntries.Count == 0)
            {
                CreateCustomerEntries(app);
            }
        }

        public static void Clear()
        {
            DeadDropEntries.Clear();
            CustomerEntries.Clear();

            CustomersScrollView = null;
            DeadDropSelector = null;
            DeadDropSelectorButton = null;

            Utils.Logger.Debug("DealerManagementAppModification", "DealerManagementAppModification cleared");
        }

        private static void CreateCustomerEntries(DealerManagementApp app)
        {
            int count = 24;

#if IL2CPP
            Il2CppReferenceArray<RectTransform> currentEntries = app.CustomerEntries;
            Il2CppReferenceArray<RectTransform> entries = new((long)count);
#elif MONO
            RectTransform[] currentEntries = app.CustomerEntries;
            RectTransform[] entries = new RectTransform[count];
#endif
            int currentCount = currentEntries.Length;

            if (currentCount == count) return;

            for (int i = 0; i < count; i++)
            {
                if (i < currentCount)
                {
                    entries[i] = currentEntries[i];
                    CustomerEntries.Add(currentEntries[i].gameObject);
                }
                else
                {
                    RectTransform last = currentEntries.Last<RectTransform>();

                    RectTransform newEntry = Object.Instantiate<RectTransform>(last, last.parent);
                    newEntry.name = $"CustomerEntry ({i})";
                    newEntry.gameObject.SetActive(false);

                    entries[i] = newEntry;
                    CustomerEntries.Add(newEntry.gameObject);
                }
            }

            app.CustomerEntries = entries;
            app.AssignCustomerButton.gameObject.transform.SetAsLastSibling();

            Utils.Logger.Debug("DealerManagementAppModification", $"Customer entries created: {count}");
        }

        private static void CreateCustomersScrollView(DealerManagementApp app)
        {
            RectTransform content = app.transform.Find("Container/Background/Content")?.GetComponent<RectTransform>();
            RectTransform customers = app.CustomerEntries.Last<RectTransform>()?.parent?.GetComponent<RectTransform>();

            if (content == null || customers == null) return;

            float height = 620f;

            GameObject scrollObject = new("Scroll");
            RectTransform scroll = scrollObject.AddComponent<RectTransform>();
            scroll.SetParent(content, false);
            scroll.anchorMin = new Vector2(0f, 0f);
            scroll.anchorMax = new Vector2(1f, 0f);
            scroll.pivot = new Vector2(0.5f, 0f);
            scroll.anchoredPosition = Vector2.zero;
            scroll.sizeDelta = new Vector2(0, height);

            GameObject viewportObject = new("Viewport");
            RectTransform viewport = viewportObject.AddComponent<RectTransform>();
            viewport.SetParent(content, false);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 0f);
            viewport.pivot = new Vector2(0.5f, 0f);
            viewport.anchoredPosition = Vector2.zero;
            viewport.sizeDelta = new Vector2(0, height);

            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            viewportObject.AddComponent<Image>();

            customers.SetParent(viewport, true);
            viewport.SetParent(scroll, false);

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = customers;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.elasticity = 0.1f;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.scrollSensitivity = 8f;

            ContentSizeFitter contentSizeFitter = customers.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup verticalLayoutGroup = customers.gameObject.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.padding = new RectOffset(0, 0, 20, 20);

            customers.pivot = new Vector2(0.5f, 1f);

            // Move customer title down for more space
            GameObject customerTitle = app.transform.Find("Container/Background/Content/CustomerTitle")?.gameObject;
            RectTransform customerTitleRectTransform = customerTitle.GetComponent<RectTransform>();
            customerTitleRectTransform.sizeDelta = new Vector2(customerTitleRectTransform.sizeDelta.x, customerTitleRectTransform.sizeDelta.y - 15);
            customerTitleRectTransform.offsetMax = new Vector2(customerTitleRectTransform.offsetMax.x, customerTitleRectTransform.offsetMax.y - 15);

            CustomersScrollView = scrollObject;

            Utils.Logger.Debug("DealerManagementAppModification", "Customers made scrollable");
        }

        public static void ShowAssignButton(DealerManagementApp app, DealerManager dealerManager)
        {
            if (dealerManager.ManagedDealer.AssignedCustomers.Count >= dealerManager.DealerData.MaxCustomers) return;

            GameObject assignCustomerButton = app.AssignCustomerButton.gameObject;
            assignCustomerButton.SetActive(true);
        }

        public static void UpdateCustomerTitle(DealerManagementApp app, DealerManager dealerManager)
        {
            GameObject customerTitle = app.transform.Find("Container/Background/Content/CustomerTitle")?.gameObject;

            if (customerTitle == null) return;

            Text text = customerTitle.GetComponent<Text>();
            text.text = $"Assigned Customers ({dealerManager.ManagedDealer.AssignedCustomers.Count}/{dealerManager.DealerData.MaxCustomers})";
        }

        private static void CreateDeadDropSelector(DealerManagementApp app)
        {
            GameObject customerSelector = app.CustomerSelector.gameObject;
            GameObject deadDropSelector = Object.Instantiate(customerSelector, customerSelector.transform.parent);

            deadDropSelector.name = "DeadDropSelector";
            deadDropSelector.SetActive(false);

            RectTransform oldContent = deadDropSelector.transform.Find("Shade/Content/Scroll View/Viewport/Content").gameObject.GetComponent<RectTransform>();
            GameObject title = deadDropSelector.transform.Find("Shade/Content/Title").gameObject;

            Text titleText = title.GetComponent<Text>();
            titleText.text = "Select Dead Drop";

            // Create template for entries
            GameObject entryTemplate = oldContent.transform.GetChild(0).gameObject;
            entryTemplate.name = "DeadDropEntry";

            GameObject mugshot = entryTemplate.transform.Find("Mugshot")?.gameObject;

            if (mugshot != null)
            {
                Object.Destroy(mugshot);
            }

            GameObject name = entryTemplate.transform.Find("Name").gameObject;
            RectTransform nameRectTransform = name.GetComponent<RectTransform>();
            nameRectTransform.sizeDelta = new Vector2(0f, 0f);

            // Create new Content wrapper
            GameObject contentObject = new("Content");

            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.SetParent(oldContent.parent, false);
            content.anchorMin = oldContent.anchorMin;
            content.anchorMax = oldContent.anchorMax;
            content.pivot = oldContent.pivot;
            content.anchoredPosition = oldContent.anchoredPosition;
            content.sizeDelta = oldContent.sizeDelta;

            ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup verticalLayoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childScaleHeight = false;
            verticalLayoutGroup.childScaleWidth = false;

            ScrollRect scrollRect = deadDropSelector.transform.Find("Shade/Content/Scroll View").gameObject.GetComponent<ScrollRect>();
            scrollRect.content = content;

            // Create DeadDropEntries
            List<DeadDrop> deadDrops = DeadDropManager.GetAllDeadDrops();

            GameObject noneDeadDropEntry = Object.Instantiate(entryTemplate, content);

            GameObject noneEntryName = noneDeadDropEntry.transform.Find("Name").gameObject;
            Text noneEntryNameText = noneEntryName.GetComponent<Text>();
            noneEntryNameText.text = "None";

            Button noneEntryButton = noneDeadDropEntry.GetComponent<Button>();

            noneEntryButton.onClick.AddListener((UnityAction)noneAction);

            void noneAction()
            {
                SelectDeadDrop(app, null, "None");
                deadDropSelector.SetActive(false);
            }

            for (int i = 0; i <= deadDrops.Count - 1; i++)
            {
                DeadDrop deadDrop = deadDrops[i];

                GameObject deadDropEntry = Object.Instantiate(entryTemplate, content);

                GameObject entryName = deadDropEntry.transform.Find("Name").gameObject;
                Text entryNameText = entryName.GetComponent<Text>();
                entryNameText.text = deadDrop.DeadDropName;

                Button entryButton = deadDropEntry.GetComponent<Button>();

                entryButton.onClick.AddListener((UnityAction)action);

                void action()
                {
                    SelectDeadDrop(app, deadDrop.GUID.ToString(), deadDrop.name);
                    deadDropSelector.SetActive(false);
                }

                DeadDropEntries.Add(deadDropEntry);
            }

            Object.Destroy(oldContent.gameObject);

            DeadDropSelector = deadDropSelector;

            Utils.Logger.Debug("DealerManagementAppModification", "DeadDropSelector created");
        }

        private static void CreateDeadDropSelectorButton(DealerManagementApp app)
        {
            GameObject contentObject = app.transform.Find("Container/Background/Content")?.gameObject;
            RectTransform content = contentObject.GetComponent<RectTransform>();

            GameObject homeObject = content.Find("Home")?.gameObject;
            GameObject buttonObject = Object.Instantiate(homeObject, homeObject.transform.parent);

            buttonObject.SetActive(true);
            buttonObject.name = "DeadDropSelectorButton";

            GameObject titleObject = buttonObject.transform.Find("Title")?.gameObject;
            Text titleText = titleObject.GetComponent<Text>();
            titleText.text = "Dead Drop";

            GameObject valueObject = buttonObject.transform.Find("Value")?.gameObject;

            RectTransform value = valueObject.GetComponent<RectTransform>();
            value.offsetMax = new Vector2(value.offsetMax.x, 60f);
            value.offsetMin = new Vector2(value.offsetMin.x, 15f);
            value.sizeDelta = new Vector2(value.sizeDelta.x, 40f);

            Text valueText = valueObject.GetComponent<Text>();
            valueText.text = "None";
            valueText.color = new Color(0.6f, 1f, 1f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener((UnityAction)action);

            static void action()
            {
                SortDeadDropEntries();
                DeadDropSelector.SetActive(true);
            }

            DeadDropSelectorButton = buttonObject;

            Utils.Logger.Debug("DealerManagementAppModification", "DeadDropSelectorButton created");
        }

        private static void SelectDeadDrop(DealerManagementApp app, string guid, string name)
        {
            Dealer dealer = app.SelectedDealer;

            DealerManager.SetDeadDrop(dealer, guid);
            SetDeadDropSelectorButtonValue(name);

            if (SyncManager.IsActive)
            {
                SyncManager.Instance.PushUpdate();
            }
        }

        private static void SortDeadDropEntries()
        {
            if (DeadDropSelector == null) return;

            GameObject player = Player.Local?.gameObject;
            Transform content = DeadDropSelector.transform.Find("Shade/Content/Scroll View/Viewport/Content");

            if (player == null || content == null) return;

            List<DeadDrop> deadDrops = DeadDropManager.GetAllByDistance(player.transform);

            foreach (DeadDrop deadDrop in deadDrops)
            {
                GameObject deadDropEntry = DeadDropEntries.Find(x =>
                {
                    GameObject entryName = x.transform.Find("Name").gameObject;
                    Text entryNameText = entryName.GetComponent<Text>();

                    return entryNameText.text == deadDrop.DeadDropName;
                });

                deadDropEntry.transform.SetAsLastSibling();
            }
        }

        public static void SetDeadDropSelectorButtonValue(string value)
        {
            if (DeadDropSelectorButton == null) return;

            GameObject gameObject = DeadDropSelectorButton.transform.Find("Value")?.gameObject;

            if (gameObject == null) return;

            Text text = gameObject.GetComponent<Text>();
            text.text = value;
        }
    }
}
