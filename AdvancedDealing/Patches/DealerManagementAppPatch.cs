using AdvancedDealing.Economy;
using AdvancedDealing.Persistence;
using AdvancedDealing.UI;
using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.Economy;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.Patches
{
    [HarmonyPatch(typeof(DealerManagementApp))]
    public class DealerManagementAppPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetDisplayedDealer")]
        public static void SetDisplayedDealerPostfix(DealerManagementApp __instance, Dealer dealer)
        {
            if (SaveManager.Instance.SavegameLoaded)
            {
                DealerManager dealerManager = DealerManager.GetInstance(dealer);

                if (dealerManager == null) return;

                string deadDropName = "None";
                string guid = dealerManager.DeadDrop;

                if (guid != null)
                {
                    DeadDropManager deadDropManager = DeadDropManager.GetInstance(dealerManager.DeadDrop);
                    deadDropName = deadDropManager.DeadDrop.DeadDropName;
                }

                UIModification.DeadDropSelector.ButtonLabel.text = deadDropName;
                UIModification.CustomersScrollView.TitleLabel.text = $"Assigned Customers ({dealerManager.Dealer.AssignedCustomers.Count}/{dealerManager.MaxCustomers})";

                if (!(dealerManager.Dealer.AssignedCustomers.Count >= dealerManager.MaxCustomers))
                {
                    UIModification.CustomersScrollView.AssignButton.SetActive(true);
                }
                else
                {
                    UIModification.CustomersScrollView.AssignButton.SetActive(false);
                }
            }
        }
    }
}