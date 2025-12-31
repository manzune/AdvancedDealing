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

namespace AdvancedDealing.NPCs.Actions
{
    public class PickupProductsAction : ActionBase
    {
        private readonly DealerExtension _dealer;

        private DeadDropExtension _deadDrop;

        private object _pickupRoutine;

        private object _instantPickupRoutine;

        private bool _deadDropIsEmpty = false;

        protected override string ActionName => "Pickup Products";

        public override bool OverrideOriginalSchedule => true;

        public PickupProductsAction(DealerExtension dealerExtension)
        {
            _dealer = dealerExtension;
            Priority = 20;
        }

        public override void Start()
        {
            base.Start();

            _deadDrop = DeadDropExtension.GetDeadDrop(_dealer.DeadDrop);

            if (_deadDrop == null)
            {
                BeginInstantPickup();
            }
            else
            {
                if (ModConfig.SkipMovement)
                {
                    BeginPickup();
                }
                else
                {
                    SetDestination(_deadDrop.GetPosition());
                }
            }
        }

        public override void End()
        {
            base.End();

            StopRoutines();
        }

        public override void MinPassed()
        {
            base.MinPassed();

            if (!IsActive || _instantPickupRoutine != null) return;

            if (_deadDrop.DeadDrop.GUID.ToString() != _dealer.DeadDrop || !_dealer.PickupProducts || TimeManager.Instance.CurrentTime == 400)
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
                    SetDestination(_deadDrop.GetPosition());
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

                _dealer.Dealer.SetAnimationTrigger("GrabItem");

                if (_deadDrop.GetAllProducts().Count <= 0)
                {
                    _dealer.SendMessage($"Could not pickup products at dead drop {_deadDrop.DeadDrop.DeadDropName}. There are no products inside!", ModConfig.NotifyOnAction);
                    _deadDropIsEmpty = true;

                    Utils.Logger.Debug($"Product pickup for {_dealer.Dealer.fullName} failed: Dead drop is empty");
                }
                else
                {
                    Dictionary<ProductItemInstance, ItemSlot> products = _deadDrop.GetAllProducts();

                    foreach (KeyValuePair<ProductItemInstance, ItemSlot> product in products)
                    {
                        if (_dealer.IsInventoryFull(out var freeSlots) || freeSlots <= 1)
                        {
                            break;
                        }

                        _dealer.Dealer.Inventory.InsertItem(product.Key);
                        product.Value.ChangeQuantity(0 - product.Key.Quantity);
                    }

                    Utils.Logger.Debug($"Product pickup for {_dealer.Dealer.fullName} was successfull");
                }

                yield return new WaitForSeconds(2f);

                End();
            }
        }

        private void BeginInstantPickup()
        {
            _instantPickupRoutine ??= MelonCoroutines.Start(InstantPickupRoutine());

            IEnumerator InstantPickupRoutine()
            {
                if (_deadDrop.GetAllProducts().Count <= 0)
                {
                    _dealer.SendMessage($"Could not pickup products at dead drop {_deadDrop.DeadDrop.DeadDropName}. There are no products inside!", ModConfig.NotifyOnAction);
                    _deadDropIsEmpty = true;

                    Utils.Logger.Debug($"Product pickup for {_dealer.Dealer.fullName} failed: Dead drop is empty");
                }
                else
                {
                    Dictionary<ProductItemInstance, ItemSlot> products = _deadDrop.GetAllProducts();

                    foreach (KeyValuePair<ProductItemInstance, ItemSlot> product in products)
                    {
                        if (_dealer.IsInventoryFull(out var freeSlots) || freeSlots <= 1)
                        {
                            break;
                        }

                        _dealer.Dealer.Inventory.InsertItem(product.Key);
                        product.Value.ChangeQuantity(0 - product.Key.Quantity);
                    }

                    Utils.Logger.Debug($"Product pickup for {_dealer.Dealer.fullName} was successfull");
                }

                yield return new WaitForSecondsRealtime(2f);

                End();
            }
        }

        private bool IsAtDestination()
        {
            return Vector3.Distance(Movement.FootPosition, _deadDrop.GetPosition()) < 2f;
        }

        private void StopRoutines()
        {
            if (_pickupRoutine != null)
            {
                MelonCoroutines.Stop(_pickupRoutine);
                _pickupRoutine = null;
            }
        }

        public override bool ShouldStart()
        {
            if (!_dealer.Dealer.IsRecruited || !_dealer.PickupProducts || _dealer.IsInventoryFull(out var freeSlots) || freeSlots <= 1 || TimeManager.Instance.CurrentTime == 400)
            {
                return false;
            }

            _dealer.GetAllProducts(out var totalAmount);

            if (totalAmount > _dealer.ProductThreshold)
            {
                return false;
            }

            DeadDropExtension deadDrop = DeadDropExtension.GetDeadDrop(_dealer.DeadDrop);

            if (_deadDropIsEmpty && deadDrop.GetAllProducts().Count <= 0)
            {
                return false;
            }

            _deadDropIsEmpty = false;

            return base.ShouldStart();
        }
    }
}
