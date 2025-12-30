using AdvancedDealing.Economy;
using System;
using AdvancedDealing.Persistence;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.UI.Phone.Messages;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Messaging;
using ScheduleOne.UI.Phone.Messages;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class FiredMessage(DealerExtension dealerExtension) : MessageBase
    {
        private readonly DealerExtension _dealer = dealerExtension;

        public override string Text => "You are fired";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealer.Dealer.IsRecruited)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            PlayerSingleton<MessagesApp>.Instance.ConfirmationPopup.Open("Are you sure?", $"Calling off the cooperation could make a dealer really mad.\n\nHe maybe will become hostile.\n(In future updates)", S1Conversation, new Action<ConfirmationPopup.EResponse>(OnConfirmationResponse));
        }

        private void OnConfirmationResponse(ConfirmationPopup.EResponse response)
        {
            if (response == ConfirmationPopup.EResponse.Confirm)
            {
                _dealer.Fire();
            }
        }
    }
}
