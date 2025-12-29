using AdvancedDealing.Economy;
using System.Collections.Generic;

#if IL2CPP
using Il2CppScheduleOne.ItemFramework;
#elif MONO
using ScheduleOne.ItemFramework;
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
            _range = UnityEngine.Random.Range(minRange, maxRange);
        }

        public override void Start()
        {
            base.Start();

            StealProducts();
        }

        private void StealProducts()
        {
            List<ItemInstance> products = [];

            foreach (ItemSlot slot in NPC.Inventory.ItemSlots)
            {
                if (slot.ItemInstance != null && slot.ItemInstance?.Category == EItemCategory.Product && slot.ItemInstance.Quantity > 0)
                {
                    products.Add(slot.ItemInstance);
                }
            }

            if (products.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, products.Count);
                ItemInstance product = products[i];

                product.SetQuantity(product.Quantity - (product.Quantity * _range / 100));
            }

            End();
        }

        public override bool ShouldOverrideOriginalSchedule()
        {
            return false;
        }
    }
}
