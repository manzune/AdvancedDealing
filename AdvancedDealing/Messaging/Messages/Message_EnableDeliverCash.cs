using AdvancedDealing.Economy;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class Message_EnableDeliverCash(DealerManager dealerManager) : MessageBase
    {
        private readonly DealerManager _dealerManager = dealerManager;

        public override string Text => "Please deliver cash";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            DealerManager dealerManager = DealerManager.GetManager(npc.GUID.ToString());
            if (!dealerManager.DealerData.DeliverCash)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            _dealerManager.DealerData.DeliverCash = true;
            _dealerManager.SendPlayerMessage($"Yoo, could you deliver your cash to the dead drop? Keep ${"1500"} at max.");
            _dealerManager.SendMessage($"Sure thing boss!", false, true, 3f);
        }
    }
}
