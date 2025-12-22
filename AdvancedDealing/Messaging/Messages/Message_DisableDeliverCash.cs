using AdvancedDealing.Economy;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class Message_DisableDeliverCash(DealerManager dealerManager) : MessageBase
    {
        private readonly DealerManager _dealerManager = dealerManager;

        public override string Text => "Stop Cash Delivery";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            DealerManager dealerManager = DealerManager.GetManager(npc.GUID.ToString());
            if (dealerManager.DealerData.DeliverCash)
            {
                return true;
            }
            return base.ShouldShowCheck(sMsg);
        }

        public override void OnSelected()
        {
            _dealerManager.DealerData.DeliverCash = false;

            _dealerManager.SendMessage($"I will no longer deliver my cash.", false, true);
        }
    }
}
