using AdvancedDealing.Economy;
using MelonLoader;
using System.Collections;
using UnityEngine;
using System;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Quests;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.Quests;
#endif

namespace AdvancedDealing.NPCs.Actions
{
    public class DeliverCashAction : ActionBase
    {
        private readonly DealerExtension _dealer;

        private DeadDropExtension _deadDrop;

        private object _deliveryRoutine;

        private object _instantDeliveryRoutine;

        private bool _deadDropIsFull = false;

        protected override string ActionName => "Deliver Cash";

        public override bool OverrideOriginalSchedule => true;

        public DeliverCashAction(DealerExtension dealer)
        {
            _dealer = dealer;
            Priority = 9;
        }

        public override void Start()
        {
            base.Start();

            _deadDrop = DeadDropExtension.GetDeadDrop(_dealer.DeadDrop);

            if (_deadDrop == null)
            {
                BeginInstantDelivery();
            }
            else
            {
                if (ModConfig.SkipMovement)
                {
                    BeginDelivery();
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

            if (!IsActive || _instantDeliveryRoutine != null) return;

            if (_dealer.Dealer.Cash < _dealer.CashThreshold || _deadDrop.DeadDrop.GUID.ToString() != _dealer.DeadDrop || !_dealer.DeliverCash || TimeManager.Instance.CurrentTime == 400)
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
                    if (!_deadDrop.IsFull())
                    {
                        BeginDelivery();
                    }
                    else
                    {
                        End();
                    }
                }
                else
                {
                    SetDestination(_deadDrop.GetPosition());
                }
            }
        }

        private void BeginDelivery()
        {
            _deliveryRoutine ??= MelonCoroutines.Start(DeliveryRoutine());

            IEnumerator DeliveryRoutine()
            {
                float cash = _dealer.Dealer.Cash;

                Movement.FaceDirection(_deadDrop.GetPosition());

                yield return new WaitForSeconds(2f);

                _dealer.Dealer.SetAnimationTrigger("GrabItem");
                _deadDrop.DeadDrop.Storage.InsertItem(MoneyManager.Instance.GetCashInstance(cash));
                _dealer.SendMessage($"I've put ${cash:F0} inside the dead drop {_deadDrop.DeadDrop.name}.", ModConfig.NotifyOnAction);

                if (ModConfig.NotifyOnAction)
                {
                    DeaddropQuest quest = NetworkSingleton<QuestManager>.Instance.CreateDeaddropCollectionQuest(_deadDrop.DeadDrop.GUID.ToString());

                    if (quest != null)
                    {
                        quest.Description = $"Collect cash at {_deadDrop.DeadDrop.DeadDropDescription}";
                        quest.Entries[0].SetEntryTitle($"{_dealer.Dealer.name}'s cash delivery {_deadDrop.DeadDrop.DeadDropName}");
                    }
                }

                _dealer.Dealer.ChangeCash(-cash);

                Utils.Logger.Debug($"Cash from {_dealer.Dealer.fullName} delivered successfully");

                yield return new WaitUntil((Func<bool>)(() => _dealer.Dealer.Cash < _dealer.CashThreshold));

                End();
            }
        }

        private void BeginInstantDelivery()
        {
            _instantDeliveryRoutine ??= MelonCoroutines.Start(InstantDeliveryRoutine());

            IEnumerator InstantDeliveryRoutine()
            {
                float cash = _dealer.Dealer.Cash;

                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(+cash, true, true);
                _dealer.SendMessage($"Sent you ${cash:F0} from my earnings.", ModConfig.NotifyOnAction);
                _dealer.Dealer.ChangeCash(-cash);

                yield return new WaitUntil((Func<bool>)(() => _dealer.Dealer.Cash < _dealer.CashThreshold));

                End();
            }
        }

        private bool IsAtDestination()
        {
            if (_deadDrop == null)
            {
                return true;
            }

            return Vector3.Distance(Movement.FootPosition, _deadDrop.GetPosition()) < 2f;
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

        public override bool ShouldStart()
        {
            if (!_dealer.Dealer.IsRecruited || !_dealer.DeliverCash || _dealer.Dealer.Cash < _dealer.CashThreshold || TimeManager.Instance.CurrentTime == 400)
            {
                return false;
            }

            DeadDropExtension deadDrop = DeadDropExtension.GetDeadDrop(_dealer.DeadDrop);

            if (deadDrop != null && deadDrop.IsFull())
            {
                if (!_deadDropIsFull)
                {
                    _deadDropIsFull = true;
                    _dealer.SendMessage($"Could not deliver cash to dead drop {deadDrop.DeadDrop.DeadDropName}. There is no space inside!", ModConfig.NotifyOnAction);
                }
                return false;
            }

            _deadDropIsFull = false;

            return base.ShouldStart();
        }
    }
}
