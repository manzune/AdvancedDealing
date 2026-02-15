using AdvancedDealing.Economy;
using MelonLoader;
using System;
using System.Collections;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Money;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
#endif

namespace AdvancedDealing.NPCs.Behaviour
{
    public class DeliverCashDealerBehaviour : DealerBehaviour
    {
        private DeadDropExtension _deadDrop;

        private object _deliveryRoutine;

        private object _instantDeliveryRoutine;

        private bool _deadDropIsFull = false;

        public override string Name => "Deliver cash";

        public DeliverCashDealerBehaviour(DealerExtension dealer) : base(dealer)
        {
            Priority = 1;
        }

        public override void Start()
        {
            _deadDrop = DeadDropExtension.GetDeadDrop(Dealer.CashDeadDrop);

            if (_deadDropIsFull && _deadDrop != null && _deadDrop.IsFull())
            {
                return;
            }

            _deadDropIsFull = false;

            base.Start();
        }

        public override void End()
        {
            base.End();
            StopRoutines();
        }

        public override void OnActiveTick()
        {
            if (!IsActive || _instantDeliveryRoutine != null) return;

            base.OnActiveTick();

            if (_deadDrop == null)
            {
                BeginInstantDelivery();
            }
            else if (ModConfig.SkipMovement)
            {
                BeginDelivery();
            }
            else
            {
                if (Dealer.Dealer.Cash < Dealer.CashThreshold || (_deadDrop != null && _deadDrop.DeadDrop.GUID.ToString() != Dealer.CashDeadDrop) || !Dealer.DeliverCash)
                {
                    End();
                }
                else
                {
                    if (_deliveryRoutine != null || Movement.IsMoving)
                    {
                        return;
                    }

                    if (IsAtDestination())
                    {
                        BeginDelivery();
                    }
                    else
                    {
                        SetDestination(_deadDrop.GetPosition(), true, 2f);
                    }
                }
            }
        }

        private void BeginDelivery()
        {
            _deliveryRoutine ??= MelonCoroutines.Start(DeliveryRoutine());

            IEnumerator DeliveryRoutine()
            {
                float cash = Dealer.Dealer.Cash;

                Movement.FaceDirection(_deadDrop.GetPosition());

                yield return new WaitForSeconds(2f);

                Dealer.Dealer.SetAnimationTrigger("GrabItem");

                if (_deadDrop.IsFull())
                {
                    _deadDropIsFull = true;
                    Dealer.SendMessage($"Could not deliver cash to dead drop {_deadDrop.DeadDrop.DeadDropName}. There is no space inside!", ModConfig.NotifyOnAction);

                    Utils.Logger.Debug($"Cash delivery for {Dealer.Dealer.fullName} failed: Dead drop is full");
                }
                else
                {
                    _deadDrop.DeadDrop.Storage.InsertItem(MoneyManager.Instance.GetCashInstance(cash));
                    Dealer.SendMessage($"I've put ${cash:F0} inside the dead drop {_deadDrop.DeadDrop.name}.", ModConfig.NotifyOnAction);

                    if (ModConfig.NotifyOnAction)
                    {
                        // TODO: Create quest
                    }

                    Dealer.Dealer.ChangeCash(0f - cash);

                    Utils.Logger.Debug($"Cash from {Dealer.Dealer.fullName} delivered successfully");

                    yield return new WaitUntil((Func<bool>)(() => Dealer.Dealer.Cash < Dealer.CashThreshold));
                }

                End();
            }
        }

        private void BeginInstantDelivery()
        {
            _instantDeliveryRoutine ??= MelonCoroutines.Start(InstantDeliveryRoutine());

            IEnumerator InstantDeliveryRoutine()
            {
                float cash = Dealer.Dealer.Cash;

                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(cash, true, true);
                Dealer.SendMessage($"Sent you ${cash:F0} from my earnings.", ModConfig.NotifyOnAction);
                Dealer.Dealer.ChangeCash(0f - cash);

                yield return new WaitUntil((Func<bool>)(() => Dealer.Dealer.Cash < Dealer.CashThreshold));

                End();
            }
        }

        private void StopRoutines()
        {
            if (_deliveryRoutine != null)
            {
                MelonCoroutines.Stop(_deliveryRoutine);
                _deliveryRoutine = null;
            }

            if (_instantDeliveryRoutine != null)
            {
                MelonCoroutines.Stop(_instantDeliveryRoutine);
                _instantDeliveryRoutine = null;
            }
        }
    }
}
