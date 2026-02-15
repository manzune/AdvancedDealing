using AdvancedDealing.Economy;
using AdvancedDealing.Persistence;
using AdvancedDealing.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class DeadDropSelector
    {
        public GameObject Container;

        public Text TitleLabel;

        public Transform Content;

        public GameObject Details;

        public GameObject DeadDropButton;

        public Text ButtonLabel;

        private readonly List<GameObject> _selectables = [];

        private DealerExtension _dealer;
        private bool _isCash;

        private GameObject _selectableTemplate;

        public bool UICreated { get; private set; }

        public bool IsOpen { get; private set; }

        public void Open(DealerExtension dealerExtension, bool isCash)
        {
            IsOpen = true;
            _dealer = dealerExtension;
            _isCash = isCash;

            foreach (DeadDrop deadDropTarget in DeadDropExtension.GetDeadDropsByDistance(Player.Local.transform))
            {
                GameObject selectable = _selectables.Find(x => x.transform.Find("Name").GetComponent<Text>().text == deadDropTarget.DeadDropName);
                selectable.transform.SetAsLastSibling();
            }

            Container.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            Container.SetActive(false);
        }

        private void OnSelected(string guid, string name)
        {
            if (_isCash)
            {
                _dealer.CashDeadDrop = guid;
            }
            else
            {
                _dealer.ProductDeadDrop = guid;
            }
            ButtonLabel.text = name;

            Utils.Logger.Debug("DeadDropSelector", $"Dead drop for {_dealer.Dealer.fullName} selected: {guid}");

            if (NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendData(_dealer.FetchData());
            }

            Close();
        }

        public void BuildUI(bool isCash)
        {
            if (UICreated) return;

            GameObject target = PlayerSingleton<DealerManagementApp>.Instance.CustomerSelector.gameObject;

            Container = Object.Instantiate(target, target.transform.parent);
            Container.name = "DeadDropSelector";
            Container.SetActive(true);

            RectTransform oldContent = Container.transform.Find("Shade/Content/Scroll View/Viewport/Content").gameObject.GetComponent<RectTransform>();

            GameObject content = new("Content");
            RectTransform transform = content.AddComponent<RectTransform>();
            transform.SetParent(oldContent.parent, false);
            transform.anchorMin = oldContent.anchorMin;
            transform.anchorMax = oldContent.anchorMax;
            transform.pivot = oldContent.pivot;
            transform.anchoredPosition = oldContent.anchoredPosition;
            transform.sizeDelta = oldContent.sizeDelta;

            TitleLabel = Container.transform.Find("Shade/Content/Title").GetComponent<Text>();
            TitleLabel.text = "Select Dead Drop";

            Content = transform;

            Object.Destroy(oldContent.gameObject);

            Container.transform.Find("Shade/Content/Scroll View").gameObject.GetComponent<ScrollRect>().content = transform;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childScaleHeight = false;
            layoutGroup.childScaleWidth = false;

            CreateSelectable(null, "None");

            for (int i = 0; i <= DeadDrop.DeadDrops.Count - 1; i++)
            {
                CreateSelectable(DeadDrop.DeadDrops[i].GUID.ToString(), DeadDrop.DeadDrops[i].DeadDropName);
            }

            CreateButtons(isCash);

            Utils.Logger.Debug("DeadDropSelector", "Dead drop selector UI created");

            UICreated = true;
        }

        private void CreateButtons(bool isCash)
        {
            GameObject target = PlayerSingleton<DealerManagementApp>.Instance.transform.Find("Container/Background/Content/Scroll/Viewport/Container/Details").gameObject;

            Details = Object.Instantiate(target, target.transform.parent);
            Details.SetActive(true);
            Details.name = "Details (Advanced Dealing)";
            Details.transform.SetSiblingIndex(5);

            GameObject container = Details.transform.Find("Container").gameObject;

            for (int i = container.transform.childCount - 1; i > 0; i --)
            {
                Object.Destroy(container.transform.GetChild(i).gameObject);
            }

            string label = isCash ? "Cash Dead Drop" : "Product Dead Drop";
            DeadDropButton = container.transform.GetChild(0).gameObject;
            DeadDropButton.name = label;
            DeadDropButton.transform.Find("Title").GetComponent<Text>().text = label;

            RectTransform transform = DeadDropButton.transform.Find("Value").GetComponent<RectTransform>();
            transform.offsetMax = new Vector2(-30f, -45f);

            ButtonLabel = transform.GetComponent<Text>();
            ButtonLabel.text = "None";
            ButtonLabel.color = new Color(0.6f, 1f, 1f, 1f);
            ButtonLabel.resizeTextForBestFit = true;
            ButtonLabel.resizeTextMaxSize = 28;

            Details.AddComponent<Button>().onClick.AddListener((UnityAction)OpenSelector);
            
            void OpenSelector()
            {
                Open(DealerExtension.GetDealer(PlayerSingleton<DealerManagementApp>.Instance.SelectedDealer), isCash);
            }
        }

        private void CreateSelectableTemplate()
        {
            _selectableTemplate = new("DeadDropEntry");
            _selectableTemplate.SetActive(false);

            RectTransform transform = _selectableTemplate.AddComponent<RectTransform>();
            transform.anchorMin = new Vector2(0f, 1f);
            transform.anchorMax = new Vector2(0f, 1f);
            transform.pivot = new Vector2(0.5f, 0.5f);
            transform.anchoredPosition = new Vector2(243f, -40f);
            transform.sizeDelta = new Vector2(486f, 80f);

            Image image = _selectableTemplate.AddComponent<Image>();

            Button button = _selectableTemplate.AddComponent<Button>();
            button.colors = new ColorBlock()
            {
                normalColor = new Color(0f, 0f, 0f, 0f),
                highlightedColor = new Color(0f, 0f, 0f, 0.2353f),
                pressedColor = new Color(0f, 0f, 0f, 0.3922f),
                selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1f),
                disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.image = image;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            GameObject name = new("Name");

            RectTransform transform2 = name.AddComponent<RectTransform>();
            transform2.SetParent(transform, false);
            transform2.anchorMin = new Vector2(0f, 0f);
            transform2.anchorMax = new Vector2(1f, 1f);
            transform2.pivot = new Vector2(0.5f, 0.5f);
            transform2.anchoredPosition = new Vector2(0f, 0f);
            transform2.sizeDelta = new Vector2(0f, 0f);
            transform2.offsetMax = new Vector2(-20f, 0f);
            transform2.offsetMin = new Vector2(20f, 0f);

            Text text = name.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.font = TitleLabel.font;
            text.fontSize = 28;
            text.fontStyle = FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.lineSpacing = 1f;
            text.resizeTextMaxSize = 76;
            text.resizeTextMinSize = 1;
            text.text = "Name";
        }

        private void CreateSelectable(string deadDropGuid, string name)
        {
            if (_selectableTemplate == null)
            {
                CreateSelectableTemplate();
            }

            GameObject selectable = Object.Instantiate(_selectableTemplate, Content);
            selectable.SetActive(true);

            selectable.transform.Find("Name").GetComponent<Text>().text = name;
            selectable.GetComponent<Button>().onClick.AddListener((UnityAction)Selected);

            void Selected()
            {
                OnSelected(deadDropGuid, name);
            }

            _selectables.Add(selectable);
        }
    }
}
