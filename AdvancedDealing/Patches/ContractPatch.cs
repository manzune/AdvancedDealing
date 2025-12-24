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
        [HarmonyPrefix]
        [HarmonyPatch("Complete")]
        public static void CompletePrefix(Contract __instance)
        {
            if (ModConfig.RealisticMode && __instance.Dealer != null && DealerManager.DealerExists(__instance.Dealer))
            {
                DealerManager dealerManager = DealerManager.GetInstance(__instance.Dealer);
                dealerManager.LevelWatcher.AddXP(LevelWatcher.ContractCompleteXP);
            }
        }
    }
}
