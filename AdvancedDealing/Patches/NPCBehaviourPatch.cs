using AdvancedDealing.Economy;
using HarmonyLib;

#if IL2CPP
using Il2CppScheduleOne.NPCs.Behaviour;
#elif MONO
using ScheduleOne.NPCs.Behaviour;
#endif

namespace AdvancedDealing.Patches
{
    [HarmonyPatch(typeof(NPCBehaviour))]
    public class NPCBehaviourPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool UpdatePrefix(NPCBehaviour __instance)
        {
            if (DealerExtension.DealerExists(__instance.Npc.GUID.ToString()))
            {
                DealerExtension dealer = DealerExtension.GetDealer(__instance.Npc.GUID.ToString());

                if (dealer.HasActiveBehaviour)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
