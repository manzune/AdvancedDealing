using AdvancedDealing.NPCs.Actions;
using System;
using System.Collections.Generic;

#if IL2CPP
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.NPCs;
#elif MONO
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Networking;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.NPCs
{
    public class ScheduleManager
    {
        public readonly NPC NPC;

        private static readonly List<ScheduleManager> cache = [];

        private readonly List<ActionBase> _actionList = [];

        private readonly NPCScheduleManager _originalSchedule;

        public bool IsEnabled { get; protected set; }

        public ActionBase ActiveAction { get; set; }

        public List<ActionBase> PendingActions { get; set; } = [];

        private List<ActionBase> ActionsAwaitingStart { get; set; } = [];

        public ScheduleManager(NPC npc)
        {
            NPC = npc;
            _originalSchedule = npc.GetComponentInChildren<NPCScheduleManager>();

            Utils.Logger.Debug("ScheduleManager", $"Schedule created: {npc.fullName}");

            cache.Add(this);
        }

        public void Start()
        {
            Enable();

            NetworkSingleton<TimeManager>.Instance.onMinutePass -= new Action(MinPassed);
            NetworkSingleton<TimeManager>.Instance.onMinutePass += new Action(MinPassed);
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
        }

        protected void MinPassed()
        {
            if ((!InstanceFinder.IsServer && !NetworkSingleton<ReplicationQueue>.Instance.ReplicationDoneForLocalPlayer) || !NPC.IsSpawned)  return;

            if (!IsEnabled)
            {
                ActiveAction?.Interrupt();

                return;
            }

            List<ActionBase> actionsToStart = GetActionsToStart();

            if (actionsToStart.Count > 0)
            {
                ActionBase nPCAction = actionsToStart[0];
                if (ActiveAction != nPCAction)
                {
                    if (ActiveAction != null && nPCAction.Priority > ActiveAction.Priority)
                    {
                        ActiveAction.Interrupt();
                    }

                    if (ActiveAction == null)
                    {
                        if (_originalSchedule.ActiveAction == null || (_originalSchedule.ActiveAction != null && nPCAction.Priority > _originalSchedule.ActiveAction.Priority))
                        {
                            StartAction(nPCAction);
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

            action.SetReferences(NPC, this, _originalSchedule, StartTime);
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

        public static ScheduleManager GetManager(string npcGuid)
        {
            ScheduleManager manager = cache.Find(x => x.NPC.GUID.ToString().Contains(npcGuid));

            if (manager == null)
            {
                Utils.Logger.Debug("ScheduleManager", $"Could not find schedule for: {npcGuid}");
                return null;
            }

            return manager;
        }

        public static void ClearAll()
        {
            foreach (ScheduleManager schedule in cache)
            {
                foreach (ActionBase action in schedule._actionList)
                {
                    action.Destroy();
                }
                schedule.Destroy();
            }

            cache.Clear();

            Utils.Logger.Debug("ScheduleManager", "Schedules deinitialized");
        }

        public static bool ScheduleExists(string npcName)
        {
            ScheduleManager instance = cache.Find(x => x.NPC.name.Contains(npcName));

            return instance != null;
        }
    }
}
