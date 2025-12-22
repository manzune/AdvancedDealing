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
    public abstract class NPCAction
    {
        public const int MAX_CONSECUTIVE_PATHING_FAILURES = 5;

        protected int priority;

        public int StartTime;

        protected NPC npc;

        protected ScheduleManager s1Schedule;

        protected NPCScheduleManager schedule;

        public Action onEnded;

        protected int consecutivePathingFailures;

        protected virtual string ActionName =>
            "ActionName";

        protected virtual string ActionType =>
            "NPCAction";

        public bool IsActive { get; protected set; }

        public bool HasStarted { get; protected set; }

        public virtual int Priority =>
            priority;

        protected NPCMovement Movement =>
            npc.Movement;

        public NPCAction()
        {
            Awake();
        }

        protected virtual void Awake()
        {
        }

        public virtual void SetReferences(NPC npc, ScheduleManager schedule, NPCScheduleManager originalSchedule, int StartTime = 0)
        {
            this.npc = npc;
            this.s1Schedule = schedule;
            this.schedule = originalSchedule;

            if (StartTime != 0)
            {
                this.StartTime = StartTime;
            }
        }

        public virtual void Start()
        {
            schedule.DisableSchedule();

            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(MinPassed);

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {npc.name} started.");

            IsActive = true;
            s1Schedule.ActiveAction = this;
            HasStarted = true;
        }

        public void Destroy()
        {
            if (s1Schedule.PendingActions.Contains(this))
            {
                s1Schedule.PendingActions.Remove(this);
            }

            if (HasStarted)
            {
                IsActive = false;

                if (s1Schedule.ActiveAction == this)
                {
                    s1Schedule.ActiveAction = null;
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
            schedule.EnableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {npc.name} ended.");

            IsActive = false;
            s1Schedule.ActiveAction = null;
            HasStarted = false;

            onEnded?.Invoke();
        }

        public virtual void Interrupt()
        {
            schedule.EnableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {npc.name} interrupted.");

            IsActive = false;
            s1Schedule.ActiveAction = null;

            if (!s1Schedule.PendingActions.Contains(this))
            {
                s1Schedule.PendingActions.Add(this);
            }
        }

        public virtual void Resume()
        {
            schedule.DisableSchedule();

            Utils.Logger.Debug("ScheduleManager", $"{ActionType} \"{ActionName}\" for {npc.name} resumed.");

            IsActive = true;
            s1Schedule.ActiveAction = this;

            if (s1Schedule.PendingActions.Contains(this))
            {
                s1Schedule.PendingActions.Remove(this);
            }
        }

        public virtual void MinPassed() { }

        public virtual bool ShouldStart() 
        { 
            return true;
        }

        public void SetDestination(Vector3 pos, bool teleportIfFail = true)
        {
            if (teleportIfFail && consecutivePathingFailures >= MAX_CONSECUTIVE_PATHING_FAILURES && !Movement.CanGetTo(pos))
            {
                Utils.Logger.Debug("ScheduleManager", $"Too many pathing failures for {npc.name}. Warping to {pos}.");

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
                    consecutivePathingFailures++;
                }
                else
                {
                    consecutivePathingFailures = 0;
                }
            }
        }
    }
}
