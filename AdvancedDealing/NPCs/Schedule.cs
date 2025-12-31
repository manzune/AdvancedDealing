using AdvancedDealing.NPCs.Actions;
using System;
using System.Collections.Generic;
using AdvancedDealing.Persistence;
using MelonLoader;
using UnityEngine;
using System.Collections;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.NPCs;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Networking;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.NPCs
{
    public class Schedule
    {
        public readonly NPC NPC;

        private static readonly List<Schedule> cache = [];

        private readonly List<ActionBase> _actionList = [];

        public bool IsEnabled { get; protected set; }

        public ActionBase ActiveAction { get; set; }

        public List<ActionBase> PendingActions { get; set; } = [];

        public NPCScheduleManager S1Schedule => NPC?.GetComponentInChildren<NPCScheduleManager>();

        private List<ActionBase> ActionsAwaitingStart { get; set; } = [];

        public Schedule(NPC npc)
        {
            NPC = npc;

            Utils.Logger.Debug("Schedule", $"Schedule created: {npc.fullName}");

            cache.Add(this);
        }

        public void Start()
        {
            MelonCoroutines.Start(StartRoutine());

            IEnumerator StartRoutine()
            {
                yield return new WaitForSecondsRealtime(2f);

                Enable();

                NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
                NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(MinPassed);
            }
        }

        public void Enable()
        {
            IsEnabled = true;

            MinPassed();
        }

        public void Disable()
        {
            IsEnabled = false;

            MinPassed();

            if (NPC.Movement.IsMoving)
            {
                NPC.Movement.Stop();
            }
        }

        public void Destroy()
        {
            if (IsEnabled)
            {
                IsEnabled = false;
            }

            if (NetworkSingleton<TimeManager>.InstanceExists)
            {
                NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            }

            if (NPC != null && S1Schedule != null)
            {
                ActiveAction?.Interrupt();
            }
        }

        protected void MinPassed()
        {
            if (!NetworkSynchronizer.IsNoSyncOrHost || !NetworkSingleton<ReplicationQueue>.Instance.ReplicationDoneForLocalPlayer || !NPC.IsSpawned)  return;

            if (!IsEnabled)
            {
                ActiveAction?.Interrupt();

                return;
            }

            List<ActionBase> actionsToStart = GetActionsToStart();

            if (actionsToStart.Count > 0)
            {
                ActionBase actionToStart = actionsToStart[0];
                if (ActiveAction != actionToStart)
                {
                    if (ActiveAction != null && actionToStart.Priority > ActiveAction.Priority)
                    {
                        ActiveAction.Interrupt();
                    }

                    if (ActiveAction == null)
                    {
                        if (!actionToStart.OverrideOriginalSchedule || S1Schedule.ActiveAction == null || (S1Schedule.ActiveAction != null && actionToStart.Priority > S1Schedule.ActiveAction.Priority))
                        {
                            StartAction(actionToStart);
                        }
                    }
                }

                foreach (ActionBase action in actionsToStart)
                {
                    if (!action.HasStarted && !ActionsAwaitingStart.Contains(action))
                    {
                        ActionsAwaitingStart.Add(action);
                    }
                }
            }
        }

        private List<ActionBase> GetActionsToStart()
        {
            List<ActionBase> list = [];

            foreach (ActionBase action in _actionList)
            {
                if (!(action == null) && action.ShouldStart())
                {
                    list.Add(action);
                }
            }

            list.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            return list;
        }

        private void StartAction(ActionBase action)
        {
            if (ActiveAction != null) return;

            if (ActionsAwaitingStart.Contains(action))
            {
                ActionsAwaitingStart.Remove(action);
            }

            if (action.HasStarted)
            {
                action.Resume();
            }
            else
            {
                action.Start();
            }
        }

        public void AddAction(ActionBase action, int StartTime = 0)
        {
            Type type = action.GetType();

            if (_actionList.Exists(a => a.GetType() == type)) return;

            action.SetReferences(NPC, this, StartTime);
            _actionList.Add(action);
        }

        public void RemoveAction(ActionBase action)
        {
            Type type = action.GetType();

            if (_actionList.Exists(a => a.GetType() == type))
            {
                _actionList.Remove(action);
            }
        }

        public static Schedule GetSchedule(string npcGuid)
        {
            Schedule schedule = cache.Find(x => x.NPC.GUID.ToString().Contains(npcGuid));

            if (schedule == null)
            {
                Utils.Logger.Debug("Schedule", $"Could not find schedule for: {npcGuid}");
                return null;
            }

            return schedule;
        }

        public static void ClearAllSchedules()
        {
            foreach (Schedule schedule in cache)
            {
                foreach (ActionBase action in schedule._actionList)
                {
                    action.Destroy();
                }
                schedule.Destroy();
            }

            cache.Clear();

            Utils.Logger.Debug("Schedule", "Schedules deinitialized");
        }

        public static bool ScheduleExists(string npcName)
        {
            Schedule instance = cache.Find(x => x.NPC.name.Contains(npcName));

            return instance != null;
        }
    }
}
