using AdvancedDealing.Economy;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    internal class DisableProductPickupMessage(DealerExtension dealerExtension) : MessageBase
    {
        private readonly DealerExtension _dealer = dealerExtension;

        public override string Text => "Stop picking up products";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealer.Dealer.IsRecruited && _dealer.PickupProducts)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            _dealer.PickupProducts = false;

            _dealer.SendPlayerMessage("We can't use the dead drops for some time. I will meet you to bring new products!");
            _dealer.SendMessage($"Waiting for you", false, true, 2f);
        }
    }
}
