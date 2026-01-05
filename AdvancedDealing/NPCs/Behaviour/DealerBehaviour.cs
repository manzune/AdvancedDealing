using AdvancedDealing.Economy;
using System;
using UnityEngine;
using UnityEngine.Events;

#if IL2CPP
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.NPCs;
using S1Behaviour = Il2CppScheduleOne.NPCs.Behaviour.Behaviour;
#elif MONO
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.NPCs;
using S1Behaviour = ScheduleOne.NPCs.Behaviour.Behaviour;
#endif

namespace AdvancedDealing.NPCs.Behaviour
{
    public abstract class DealerBehaviour
    {
        public const int MAX_CONSECUTIVE_PATHING_FAILURES = 5;

        public int Priority;

        protected int ConsecutivePathingFailures;

        protected DealerExtension Dealer;

        private Vector3 _lastDestination;

        private bool _enableScheduleOnEnd = false;

        public virtual string Name => "Behaviour";

        public virtual bool IsEnabled { get; protected set; }

        public bool IsActive { get; protected set; }

        public bool HasStarted { get; protected set; }

        protected NPCMovement Movement => Dealer.Dealer.Movement;

        private S1Behaviour activeBehaviour => Dealer.Dealer.Behaviour?.activeBehaviour;

        private NPCScheduleManager schedule => Dealer.Dealer.GetComponentInChildren<NPCScheduleManager>();

        public DealerBehaviour(DealerExtension dealer)
        {
            Dealer = dealer;

            dealer.Dealer.Health.onKnockedOut.AddListener((UnityAction)OnKnockOutOrDie);
            dealer.Dealer.Health.onDie.AddListener((UnityAction)OnKnockOutOrDie);
            dealer.Dealer.Health.onRevive.AddListener((UnityAction)OnRevive);
        }

        public virtual void Enable()
        {
            IsEnabled = true;

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} enabled: {Name}");
        }

        public void Disable()
        {
            IsEnabled = false;

            if (HasStarted)
            {
                End();
            }

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} disabled: {Name}");
        }

        public virtual void Start()
        {
            HasStarted = true;
            IsActive = true;

            Dealer.SetActiveBehaviour(this);

            activeBehaviour?.Pause();

            if (schedule != null && schedule.ScheduleEnabled)
            {
                schedule.DisableSchedule();
                _enableScheduleOnEnd = true;
            }

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} started: {Name}");
        }

        public virtual void End()
        {
            HasStarted = false;
            IsActive = false;

            Disable();
            Dealer.SetActiveBehaviour(null);

            if (_enableScheduleOnEnd && schedule != null && !schedule.ScheduleEnabled)
            {
                schedule.EnableSchedule();
            }

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} ended: {Name}");
        }

        public virtual void Pause()
        {
            IsActive = false;

            if (_enableScheduleOnEnd && schedule != null && !schedule.ScheduleEnabled)
            {
                schedule.EnableSchedule();
            }

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} paused: {Name}");
        }

        public virtual void Resume()
        {
            IsActive = true;

            if (schedule != null && schedule.ScheduleEnabled)
            {
                schedule.DisableSchedule();
                _enableScheduleOnEnd = true;
            }

            Utils.Logger.Debug("DealerBehaviour", $"Behaviour for {Dealer.Dealer.fullName} resumed: {Name}");
        }

        public virtual void OnActiveTick()
        {
            activeBehaviour?.Pause();
        }

        protected virtual void SetDestination(Vector3 position, bool teleportIfFail = true, float successThreshold = 1f)
        {
            if (InstanceFinder.IsServer)
            {
                if (teleportIfFail && ConsecutivePathingFailures >= MAX_CONSECUTIVE_PATHING_FAILURES && !Movement.CanGetTo(position))
                {
                    Utils.Logger.Debug("DealerBehaviour", $"Too many pathing failures for {Dealer.Dealer.fullName}. Warping to {position}.");

                    NavMeshUtility.SamplePosition(position, out var hit, 5f, -1);
                    position = hit.position;
                    Movement.Warp(position);
                    WalkCallback(NPCMovement.WalkResult.Success);
                }
                else
                {
                    _lastDestination = position;
                    Movement.SetDestination(position, new Action<NPCMovement.WalkResult>(WalkCallback), successThreshold, 0.1f);
                }
            }
        }

        protected bool IsAtDestination()
        {
            return Vector3.Distance(Movement.FootPosition, _lastDestination) < 2f;
        }

        protected virtual void WalkCallback(NPCMovement.WalkResult res)
        {
            if (IsActive)
            {
                if (res == NPCMovement.WalkResult.Failed)
                {
                    ConsecutivePathingFailures++;
                }
                else
                {
                    ConsecutivePathingFailures = 0;
                }
            }
        }

        private void OnKnockOutOrDie()
        {
            if (IsEnabled && HasStarted)
            {
                Pause();
            }
        }

        private void OnRevive()
        {
            if (IsEnabled && HasStarted)
            {
                Resume();
            }
        }
    }
}
