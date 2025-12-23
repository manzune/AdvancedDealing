using AdvancedDealing.Economy;
using AdvancedDealing.UI;


#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class Message_AdjustSettings(DealerManager dealerManager) : MessageBase
    {
        private readonly DealerManager _dealerManager = dealerManager;

        public override string Text => "Adjust Settings";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (ModConfig.RealisticMode)
            {
                return false;
            }
            return true;
        }

        public override void OnSelected()
        {
            MessagesAppModification.SettingsPopup.Open(conversation, dealerManager);
        }
    }
}
