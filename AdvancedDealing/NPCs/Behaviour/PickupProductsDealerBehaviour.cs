using AdvancedDealing.Economy;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
#elif MONO
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;
#endif

namespace AdvancedDealing.NPCs.Behaviour
{
    public class PickupProductsDealerBehaviour : DealerBehaviour
    {
        private DeadDropExtension _deadDrop;

        private object _pickupRoutine;

        private object _instantPickupRoutine;

        private bool _deadDropIsEmpty = false;

        public override string Name => "Pickup products";

        public PickupProductsDealerBehaviour(DealerExtension dealer) : base(dealer)
        {
            Priority = 2;
        }

        public override void Start()
        {
            _deadDrop = DeadDropExtension.GetDeadDrop(Dealer.DeadDrop);

            if (_deadDropIsEmpty && _deadDrop != null && _deadDrop.GetAllProducts().Count <= 0)
            {
                return;
            }

            _deadDropIsEmpty = false;

            base.Start();
        }

        public override void End()
        {
            base.End();
            StopRoutines();
        }

        public override void OnActiveTick()
        {
            if (!IsActive || _instantPickupRoutine != null) return;

            base.OnActiveTick();

            if (_deadDrop == null)
            {
                End();
                return;
            }
            else if (ModConfig.SkipMovement)
            {
                BeginPickup();
            }
            else
            {
                if ((_deadDrop != null && _deadDrop.DeadDrop.GUID.ToString() != Dealer.DeadDrop) || !Dealer.PickupProducts || TimeManager.Instance.CurrentTime == 400)
                {
                    End();
                }
                else
                {
                    if (_pickupRoutine != null || Movement.IsMoving)
                    {
                        return;
                    }

                    if (IsAtDestination())
                    {
                        BeginPickup();
                    }
                    else
                    {
                        SetDestination(_deadDrop.GetPosition(), true, 2f);
                    }
                }
            }
        }

        private void BeginPickup()
        {
            _pickupRoutine ??= MelonCoroutines.Start(PickupRoutine());

            IEnumerator PickupRoutine()
            {
                Movement.FaceDirection(_deadDrop.GetPosition());

                yield return new WaitForSeconds(2f);

                Dealer.Dealer.SetAnimationTrigger("GrabItem");

                bool shouldNotify = true;

                if (_deadDrop.GetAllProducts().Count > 0)
                {
                    Dictionary<ProductItemInstance, ItemSlot> products = _deadDrop.GetAllProducts();

                    foreach (KeyValuePair<ProductItemInstance, ItemSlot> product in products)
                    {
                        if (Dealer.IsInventoryFull(out var freeSlots) || freeSlots <= 1)
                        {
                            break;
                        }

                        Dealer.Dealer.Inventory.InsertItem(product.Key);
                        product.Value.ChangeQuantity(0 - product.Key.Quantity);
                    }

                    Dealer.GetAllProducts(out var totalAmount);

                    if (totalAmount <= Dealer.ProductThreshold && !Dealer.IsInventoryFull(out var freeSlots2) && freeSlots2 > 1)
                    {
                        shouldNotify = true;
                    }
                    else
                    {
                        shouldNotify = false;

                        Utils.Logger.Debug($"Product pickup for {Dealer.Dealer.fullName} was successfull");
                    }
                }

                if (shouldNotify)
                {
                    Dealer.SendMessage($"Could not pickup products at dead drop {_deadDrop.DeadDrop.DeadDropName}. There are no products inside!", ModConfig.NotifyOnAction);
                    _deadDropIsEmpty = true;

                    if (ModConfig.NotifyOnAction)
                    {
                        // TODO: Create quest
                    }

                    Utils.Logger.Debug($"Product pickup for {Dealer.Dealer.fullName} failed: Dead drop is empty");
                }

                yield return new WaitForSeconds(2f);

                End();
            }
        }

        private void StopRoutines()
        {
            if (_pickupRoutine != null)
            {
                MelonCoroutines.Stop(_pickupRoutine);
                _pickupRoutine = null;
            }
        }
    }
}
