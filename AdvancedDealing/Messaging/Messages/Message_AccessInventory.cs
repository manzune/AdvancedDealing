using AdvancedDealing.Economy;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class Message_AccessInventory(DealerManager dealerManager) : MessageBase
    {
        private readonly DealerManager _dealerManager = dealerManager;

        public override string Text => "I need to access your inventory";

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
            // _dealerManager.ManagedDealer.Inventory.
        }
    }
}
