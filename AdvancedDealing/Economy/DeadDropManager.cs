using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if IL2CPP
using Il2CppScheduleOne.Economy;
#elif MONO
using ScheduleOne.Economy;
#endif

namespace AdvancedDealing.Economy
{
    public class DeadDropManager
    {
        private readonly DeadDrop _deadDrop;

        private static readonly List<DeadDropManager> _cache = [];

        public DeadDropManager(DeadDrop deadDrop)
        {
            _deadDrop = deadDrop;
        }

        public static DeadDropManager GetManager(DeadDrop deadDrop)
        {
            return _cache.Find(x => x._deadDrop == deadDrop);
        }

        public static DeadDropManager GetManager(string deadDropGuid)
        {
            return _cache.Find(x => x._deadDrop.GUID.ToString().Contains(deadDropGuid));
        }

        public static void AddDeadDrop(DeadDrop deadDrop)
        {
            if (DeadDropExists(deadDrop)) return;

            DeadDropManager manager = new(deadDrop);
            _cache.Add(manager);

            Utils.Logger.Debug("DeadDropManager", $"Dead drop added: {deadDrop.GUID}");
        }

        public static DeadDrop GetDeadDrop(string deadDropGuid)
        {
            DeadDropManager manager = _cache.Find(x => x._deadDrop.GUID.ToString().Contains(deadDropGuid));
            if (manager == null)
            {
                Utils.Logger.Debug("DeadDropManager", $"Could not find dead drop: {deadDropGuid}");

                return null;
            }

            return manager._deadDrop;
        }

        public static List<DeadDrop> GetAllDeadDrops()
        {
            List<DeadDrop> deadDrops = [];
            foreach (DeadDropManager manager in _cache)
            {
                deadDrops.Add(manager._deadDrop);
            }

            return deadDrops;
        }

        public static bool DeadDropExists(DeadDrop deadDrop)
        {
            return _cache.Any(x => x._deadDrop == deadDrop);
        }

        public static List<DeadDrop> GetAllByDistance(Transform origin)
        {
            List<DeadDrop> deadDrops = GetAllDeadDrops();
            deadDrops.Sort((x, y) => (x.transform.position - origin.position).sqrMagnitude.CompareTo((y.transform.position - origin.position).sqrMagnitude));

            return deadDrops;
        }

        public static bool IsFull(DeadDrop deadDrop)
        {
            return (deadDrop.Storage.ItemCount >= deadDrop.Storage.SlotCount);
        }

        public static Vector3 GetPosition(DeadDrop deadDrop)
        {
            return deadDrop.transform.position;
        }

        public static void Load()
        {
            _cache.Clear();

            foreach (DeadDrop deadDrop in DeadDrop.DeadDrops)
            {
                AddDeadDrop(deadDrop);
            }
        }
    }
}
