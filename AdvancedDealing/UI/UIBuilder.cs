using MelonLoader;
using System;
using System.Collections;
using UnityEngine;
using System.ComponentModel;


#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
#endif

namespace AdvancedDealing.UI
{
    public static class UIBuilder
    {
        public static bool HasBuild { get; private set; }

        public static SettingsPopup SettingsPopup { get; private set; }

        public static SliderPopup SliderPopup { get; private set; }

        public static DeadDropSelector DeadDropSelector { get; private set; }

        public static CustomersScrollView CustomersScrollView { get; private set; }

        public static LoyalityDisplay LoyalityDisplay { get; private set; }

        public static void Build()
        {
            if (!HasBuild)
            {

                SettingsPopup ??= new();
                SliderPopup ??= new();
                DeadDropSelector ??= new();
                CustomersScrollView ??= new();
                LoyalityDisplay ??= new();

                MelonCoroutines.Start(CreateUI());

                static IEnumerator CreateUI()
                {
                    yield return new WaitUntil((Func<bool>)(() => !PersistentSingleton<LoadManager>.Instance.IsLoading && PersistentSingleton<LoadManager>.Instance.IsGameLoaded));

                    SettingsPopup.BuildUI();
                    SliderPopup.BuildUI();
                    DeadDropSelector.BuildUI();
                    CustomersScrollView.BuildUI();
                    LoyalityDisplay.BuildUI();

                    ChangeLoyalityModeUI();

                    Utils.Logger.Msg("UI elements created");

                    HasBuild = true;
                }
            }
        }

        public static void ChangeLoyalityModeUI()
        {
            RectTransform transform = CustomersScrollView.Viewport.GetComponent<RectTransform>();
            RectTransform transform2 = CustomersScrollView.CustomerTitle;

            if (ModConfig.LoyalityMode)
            {
                LoyalityDisplay.Container.SetActive(true);

                transform.offsetMax = new Vector2(0f, 490f);

                transform2.offsetMax = new Vector2(-50f, -662.3004f);
                transform2.offsetMin = new Vector2(65f, -712.3004f);
            }
            else
            {
                LoyalityDisplay.Container.SetActive(false);

                transform.offsetMax = new Vector2(0f, 620f);

                transform2.offsetMax = new Vector2(-50f, -542.3004f);
                transform2.offsetMin = new Vector2(65f, -592.3004f);
            }
        }

        public static void Reset()
        {
            SettingsPopup = null;
            SliderPopup = null;
            DeadDropSelector = null;
            CustomersScrollView = null;

            HasBuild = false;
        }
    }
}
