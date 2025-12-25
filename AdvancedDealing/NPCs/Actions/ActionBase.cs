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

        public int Priority;

        public int StartTime;

        public Action OnEnded;

        protected NPC NPC;

        protected ScheduleManager S1Schedule;

        protected NPCScheduleManager Schedule;

        protected int ConsecutivePathingFailures;

        protected virtual string ActionName => "ActionName";

        protected virtual string ActionType => "Action";

        public bool IsActive { get; protected set; }

        public bool HasStarted { get; protected set; }

        protected NPCMovement Movement =>
            NPC.Movement;

        public ActionBase()
        {
            Awake();
        }

        protected virtual void Awake()
        {
        }

        public virtual void SetReferences(NPC npc, ScheduleManager schedule, NPCScheduleManager originalSchedule, int StartTime = 0)
        {
            this.NPC = npc;
            this.S1Schedule = schedule;
            this.Schedule = originalSchedule;

            if (StartTime != 0)
            {
                this.StartTime = StartTime;
            }
        }

        public virtual void Start()
        {
            Schedule.DisableSchedule();

            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(MinPassed);

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {NPC.name} started.");

            IsActive = true;
            S1Schedule.ActiveAction = this;
            HasStarted = true;
        }

        public void Destroy()
        {
            if (S1Schedule.PendingActions.Contains(this))
            {
                S1Schedule.PendingActions.Remove(this);
            }

            if (HasStarted)
            {
                IsActive = false;

                if (S1Schedule.ActiveAction == this)
                {
                    S1Schedule.ActiveAction = null;
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
            Schedule.EnableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {NPC.name} ended.");

            IsActive = false;
            S1Schedule.ActiveAction = null;
            HasStarted = false;

            OnEnded?.Invoke();
        }

        public virtual void Interrupt()
        {
            Schedule.EnableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {NPC.name} interrupted.");

            IsActive = false;
            S1Schedule.ActiveAction = null;

            if (!S1Schedule.PendingActions.Contains(this))
            {
                S1Schedule.PendingActions.Add(this);
            }
        }

        public virtual void Resume()
        {
            Schedule.DisableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {NPC.name} resumed.");

            IsActive = true;
            S1Schedule.ActiveAction = this;

            if (S1Schedule.PendingActions.Contains(this))
            {
                S1Schedule.PendingActions.Remove(this);
            }
        }

        public virtual void MinPassed() { }

        public virtual bool ShouldStart() 
        { 
            return true;
        }

        public void SetDestination(Vector3 pos, bool teleportIfFail = true)
        {
            if (teleportIfFail && ConsecutivePathingFailures >= MaxConsecutivePathingFailures && !Movement.CanGetTo(pos))
            {
                Utils.Logger.Debug("ScheduleManager", $"Too many pathing failures for {NPC.name}. Warping to {pos}.");

                Movement.Warp(pos);
                WalkCallback(NPCMovement.WalkResult.Success);
            }
            else
            {
                Movement.SetDestination(pos, (Action<NPCMovement.WalkResult>)(res => WalkCallback(res)), maximumDistanceForSuccess: 1f);
            }
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
