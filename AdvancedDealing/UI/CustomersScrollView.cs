using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class CustomersScrollView
    {
        public const int MAX_ENTRIES = 24;

        public GameObject Container;

        public GameObject Viewport;

        public GameObject AssignButton;

        public RectTransform CustomerTitle;

        public List<GameObject> CustomerEntries = [];

        public Text TitleLabel;

        public bool UICreated { get; private set; }

        public void BuildUI()
        {
            Container = PlayerSingleton<DealerManagementApp>.Instance.transform.Find("Container/Background/Content").gameObject;
            float height = 620f;

            GameObject scroll = new("Scroll");
            RectTransform transform = scroll.AddComponent<RectTransform>();
            transform.SetParent(Container.transform, false);
            transform.anchorMin = new Vector2(0f, 0f);
            transform.anchorMax = new Vector2(1f, 0f);
            transform.pivot = new Vector2(0.5f, 0f);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(0, height);

            Viewport = new("Viewport");
            RectTransform transform2 = Viewport.AddComponent<RectTransform>();
            transform2.SetParent(transform, false);
            transform2.anchorMin = new Vector2(0f, 0f);
            transform2.anchorMax = new Vector2(1f, 0f);
            transform2.pivot = new Vector2(0.5f, 0f);
            transform2.anchoredPosition = Vector2.zero;
            transform2.sizeDelta = new Vector2(0, height);
            Viewport.AddComponent<Mask>().showMaskGraphic = false;
            Viewport.AddComponent<Image>();

            GameObject customers = PlayerSingleton<DealerManagementApp>.Instance.CustomerEntries.Last().parent.gameObject;
            RectTransform transform3 = customers.GetComponent<RectTransform>();
            transform3.SetParent(transform2, true);
            transform3.pivot = new Vector2(0.5f, 1f);

            ContentSizeFitter contentSizeFitter = customers.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            customers.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 20, 20);

            ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.viewport = transform2;
            scrollRect.content = transform3;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.elasticity = 0.1f;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.scrollSensitivity = 8f;

            // Move customer title down for more space
            CustomerTitle = Container.transform.Find("CustomerTitle").GetComponent<RectTransform>();
            CustomerTitle.sizeDelta = new Vector2(CustomerTitle.sizeDelta.x, CustomerTitle.sizeDelta.y - 15);
            CustomerTitle.offsetMax = new Vector2(CustomerTitle.offsetMax.x, CustomerTitle.offsetMax.y - 15);

            TitleLabel = CustomerTitle.GetComponent<Text>();
            AssignButton = PlayerSingleton<DealerManagementApp>.Instance.AssignCustomerButton.gameObject;

            CreateCustomerEntries();

            Utils.Logger.Debug("CustomersScrollView", "Customers scroll view UI created");

            UICreated = true;
        }

        private void CreateCustomerEntries()
        {
#if IL2CPP
            Il2CppReferenceArray<RectTransform> currentEntries = PlayerSingleton<DealerManagementApp>.Instance.CustomerEntries;
            Il2CppReferenceArray<RectTransform> entries = new(MAX_ENTRIES);
#elif MONO
            RectTransform[] currentEntries = PlayerSingleton<DealerManagementApp>.Instance.CustomerEntries;
            RectTransform[] entries = new RectTransform[MAX_ENTRIES];
#endif
            int currentCount = currentEntries.Length;

            if (currentCount == MAX_ENTRIES) return;

            for (int i = 0; i < MAX_ENTRIES; i++)
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

            PlayerSingleton<DealerManagementApp>.Instance.CustomerEntries = entries;
            PlayerSingleton<DealerManagementApp>.Instance.AssignCustomerButton.transform.SetAsLastSibling();
        }
    }
}
