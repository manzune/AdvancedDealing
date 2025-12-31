using AdvancedDealing.Economy;
using System.Collections.Generic;

#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
#elif MONO
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;
#endif

namespace AdvancedDealing.NPCs.Actions
{
    public class StealProductsAction : ActionBase
    {
        private readonly DealerExtension _dealer;

        private readonly int _range;

        protected override string ActionName => "Steal Products";

        protected override bool RemoveOnEnd => true;

        public StealProductsAction(DealerExtension dealerExtension, int minRange, int maxRange)
        {
            _dealer = dealerExtension;
            _range = UnityEngine.Random.Range(minRange, maxRange + 1);
        }

        public override void Start()
        {
            base.Start();

            StealProducts();
        }

        private void StealProducts()
        {
            List<ItemSlot> validSlots = [];

            foreach (ItemSlot slot in NPC.Inventory.ItemSlots)
            {
                if (slot.ItemInstance != null && slot.ItemInstance.Category == EItemCategory.Product)
                {
                    validSlots.Add(slot);
                }
            }

            if (validSlots.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, validSlots.Count);
                int amountToSteal = validSlots[i].Quantity * _range / 100;
                string productName = validSlots[i].ItemInstance.Name;

                if (amountToSteal <= 0)
                {
                    amountToSteal = 1;
                }

                validSlots[i].ChangeQuantity(0 - amountToSteal);

                Utils.Logger.Debug($"{_dealer.Dealer.fullName} has stolen some products: {amountToSteal} {productName}");
            }

            End();
        }
    }
}
