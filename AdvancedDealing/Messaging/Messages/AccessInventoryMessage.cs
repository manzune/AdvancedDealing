using AdvancedDealing.Economy;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI;
#elif MONO
using System.Reflection;
using ScheduleOne.Messaging;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.UI;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class AccessInventoryMessage(DealerExtension dealerExtension) : MessageBase
    {
        private readonly DealerExtension _dealer = dealerExtension;

        public override string Text => "Access inventory";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealer.Dealer.IsRecruited && ModConfig.AccessInventory)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            Singleton<GameplayMenu>.Instance.SetIsOpen(false);
#if IL2CPP
            _dealer.Dealer.TradeItems();
#elif MONO
            typeof(Dealer).GetMethod("TradeItems", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_dealer.Dealer, []);
#endif
        }
    }
}
