using UnityEngine;
using System;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.NPCs;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.NPCs.Actions
{
    public abstract class ActionBase
    {
        public const int MaxConsecutivePathingFailures = 5;

        public int Priority = 0;

        public int StartTime;

        public Action OnEnded;

        protected NPC NPC;

        protected Schedule Schedule;

        protected NPCScheduleManager S1Schedule => Schedule.S1Schedule;

        protected int ConsecutivePathingFailures;

        public virtual bool OverrideOriginalSchedule => false;

        public bool IsActive { get; protected set; }

        public bool HasStarted { get; protected set; }

        protected NPCMovement Movement => NPC.Movement;

        protected virtual string ActionName => "ActionName";

        protected virtual string ActionType => "Action";

        protected virtual bool RemoveOnEnd => false;

        private Vector3 _lastDestination;

        public virtual void SetReferences(NPC npc, Schedule schedule, int startTime = 0)
        {
            NPC = npc;
            Schedule = schedule;

            if (StartTime != 0)
            {
                StartTime = startTime;
            }
        }

        public virtual void Start()
        {
            if (OverrideOriginalSchedule)
            {
                S1Schedule.DisableSchedule();
            }

            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(MinPassed);

            IsActive = true;
            Schedule.ActiveAction = this;
            HasStarted = true;

            Utils.Logger.Debug($"{ActionType} \"{ActionName}\" for {NPC.name} started.");
        }

        public void Destroy()
        {
            if (Schedule.PendingActions.Contains(this))
            {
                Schedule.PendingActions.Remove(this);
            }

            if (HasStarted)
            {
                IsActive = false;

                if (Schedule.ActiveAction == this)
                {
                    Schedule.ActiveAction = null;
                }

                HasStarted = false;
            }

            if (NetworkSingleton<TimeManager>.InstanceExists)
            {
                NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            }
        }

        public virtual void End()
        {
            IsActive = false;
            Schedule.ActiveAction = null;
            HasStarted = false;

            Utils.Logger.Debug($"{ActionType} \"{ActionName}\" for {NPC.name} ended.");

            if (OverrideOriginalSchedule)
            {
                S1Schedule.EnableSchedule();
            }

            if (RemoveOnEnd)
            {
                Schedule.RemoveAction(this);
            }

            OnEnded?.Invoke();
        }

        public virtual void Interrupt()
        {
            IsActive = false;
            Schedule.ActiveAction = null;

            Utils.Logger.Debug($"{ActionType} \"{ActionName}\" for {NPC.name} interrupted.");

            if (OverrideOriginalSchedule)
            {
                S1Schedule.EnableSchedule();
            }

            if (!Schedule.PendingActions.Contains(this))
            {
                Schedule.PendingActions.Add(this);
            }
        }

        public virtual void Resume()
        {
            if (OverrideOriginalSchedule)
            {
                S1Schedule.DisableSchedule();
            }

            IsActive = true;
            Schedule.ActiveAction = this;

            Utils.Logger.Debug($"{ActionType} \"{ActionName}\" for {NPC.name} resumed.");

            if (Schedule.PendingActions.Contains(this))
            {
                Schedule.PendingActions.Remove(this);
            }
        }

        public virtual void MinPassed() 
        {
            if (OverrideOriginalSchedule && IsActive && S1Schedule.ScheduleEnabled)
            {
                S1Schedule.DisableSchedule();
            }
        }

        public virtual bool ShouldStart() 
        { 
            return true;
        }

        public void SetDestination(Vector3 pos, bool teleportIfFail = true)
        {
            if (teleportIfFail && ConsecutivePathingFailures >= MaxConsecutivePathingFailures && !Movement.CanGetTo(pos))
            {
                Utils.Logger.Debug($"Too many pathing failures for {NPC.name}. Warping to {pos}.");

                Movement.Warp(pos);
                WalkCallback(NPCMovement.WalkResult.Success);
            }
            else
            {
                _lastDestination = pos;

                Movement.SetDestination(pos, new Action<NPCMovement.WalkResult>(WalkCallback), maximumDistanceForSuccess: 1f);
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
    }
}
