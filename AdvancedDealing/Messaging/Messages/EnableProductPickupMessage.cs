using AdvancedDealing.Economy;
using AdvancedDealing.UI;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class EnableProductPickupMessage(DealerExtension dealerExtension) : MessageBase
    {
        private readonly DealerExtension _dealer = dealerExtension;

        public override string Text => "Pickup products";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealer.Dealer.IsRecruited && !_dealer.PickupProducts)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            UIBuilder.SliderPopup.Open($"Product Threshold ({_dealer.Dealer.name})", null, _dealer.ProductThreshold, 0f, 1000f, 10f, 0, OnSend, null, "{0:0} pcs");
        }

        private void OnSend(float value)
        {
            _dealer.PickupProducts = true;
            _dealer.ProductThreshold = (int)value;

            _dealer.SendPlayerMessage($"I will deposit products at the dead drop. Come there if you got less than {value}pcs left.");
            _dealer.SendMessage($"Ok. Will pick them up soon!", false, true, 2f);
        }
    }
}
