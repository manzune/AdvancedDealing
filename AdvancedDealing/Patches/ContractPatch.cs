using AdvancedDealing.Economy;
using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.Quests;
#elif MONO
using ScheduleOne.Quests;
#endif

namespace AdvancedDealing.Patches
{
    [HarmonyPatch(typeof(Contract))]
    public class ContractPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Complete")]
        public static void CompletePrefix(Contract __instance)
        {
            Utils.Logger.Debug("Contract completed: " + __instance.Dealer);

            if (__instance.Dealer != null && DealerManager.DealerExists(__instance.Dealer))
            {
                DealerManager dealerManager = DealerManager.GetInstance(__instance.Dealer);
                dealerManager.LevelWatcher.AddXP(LevelWatcher.ContractCompleteXP);
            }
        }
    }
}
