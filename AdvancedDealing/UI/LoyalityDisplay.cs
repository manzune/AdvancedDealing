using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.UI
{
    public class LoyalityDisplay
    {
        public GameObject Container;

        public Text ValueLabel;

        public void BuildUI()
        {
            GameObject target = PlayerSingleton<DealerManagementApp>.Instance.transform.Find("Container/Background/Content/Home").gameObject;

            Container = Object.Instantiate(target, target.transform.parent);
            Container.SetActive(true);
            Container.name = "Loyality";
            Container.transform.Find("Title").GetComponent<Text>().text = "Loyality";

            RectTransform transform = Container.GetComponent<RectTransform>();
            transform.offsetMax = new Vector2(transform.offsetMax.x, -550f);
            transform.offsetMin = new Vector2(transform.offsetMin.x, -650f);

            ValueLabel = Container.transform.Find("Value").GetComponent<Text>();
            ValueLabel.text = "None";
            ValueLabel.color = new Color(0.3f, 0.6f, 0.5f, 1f);
        }
    }
}
