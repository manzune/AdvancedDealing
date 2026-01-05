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
            if (SaveModifier.Instance.SavegameLoaded && UIBuilder.HasBuild)
            {
                DealerExtension dealerExtension = DealerExtension.GetDealer(dealer);

                if (dealerExtension == null) return;

                string deadDropName = "None";
                string guid = dealerExtension.DeadDrop;

                if (guid != null)
                {
                    DeadDropExtension deadDrop = DeadDropExtension.GetDeadDrop(dealerExtension.DeadDrop);
                    deadDropName = deadDrop.DeadDrop.DeadDropName;
                }

                UIBuilder.DeadDropSelector.ButtonLabel.text = deadDropName;
                UIBuilder.CustomersScrollView.TitleLabel.text = $"Assigned Customers ({dealerExtension.Dealer.AssignedCustomers.Count}/{dealerExtension.MaxCustomers})";

                if (!(dealerExtension.Dealer.AssignedCustomers.Count >= dealerExtension.MaxCustomers))
                {
                    UIBuilder.CustomersScrollView.AssignButton.SetActive(true);
                }
                else
                {
                    UIBuilder.CustomersScrollView.AssignButton.SetActive(false);
                }
            }
        }
    }
}