using AdvancedDealing.Economy;
using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.Economy;
#elif MONO
using ScheduleOne.Economy;
using System.Reflection;
#endif

namespace AdvancedDealing.Patches
{
    [HarmonyPatch(typeof(Dealer))]
    public class DealerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnTick")]
        public static bool OnTickPrefix(Dealer __instance)
        {
            if (DealerExtension.DealerExists(__instance))
            {
                DealerExtension dealer = DealerExtension.GetDealer(__instance);
                
                if (dealer.HasActiveBehaviour)
                {
#if IL2CPP
                    dealer.Dealer.UpdatePotentialDealerPoI();
#elif MONO
                    typeof(Dealer).GetMethod("UpdatePotentialDealerPoI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, []);
#endif
                    return false;
                }
            }

            return true;
        }
    }
}