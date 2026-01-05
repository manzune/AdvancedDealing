using MelonLoader;
using System;
using System.Collections;
using UnityEngine;

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

        public static void Build()
        {
            if (!HasBuild)
            {

                SettingsPopup ??= new();
                SliderPopup ??= new();
                DeadDropSelector ??= new();
                CustomersScrollView ??= new();

                MelonCoroutines.Start(CreateUI());

                static IEnumerator CreateUI()
                {
                    yield return new WaitUntil((Func<bool>)(() => !PersistentSingleton<LoadManager>.Instance.IsLoading && PersistentSingleton<LoadManager>.Instance.IsGameLoaded));

                    SettingsPopup.BuildUI();
                    SliderPopup.BuildUI();
                    DeadDropSelector.BuildUI();
                    CustomersScrollView.BuildUI();

                    Utils.Logger.Msg("UI elements created");

                    HasBuild = true;
                }
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
