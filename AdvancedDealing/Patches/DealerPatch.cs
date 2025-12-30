using AdvancedDealing.Economy;
using HarmonyLib;
using Il2CppScheduleOne.Economy;

namespace AdvancedDealing.Patches
{
    [HarmonyPatch(typeof(Dealer))]
    public class DealerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CustomerContractEnded")]
        public static void CustomerContractEndedPostfix(Dealer __instance)
        {
            if (DealerExtension.DealerExists(__instance))
            {
                DealerExtension dealer = DealerExtension.GetDealer(__instance);
                dealer.DailyContractCount++;

                if (dealer.DailyContractCount < 6)
                {
                    dealer.ChangeLoyality(-10f);
                }
            }
        }
    }
}
