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
        private static readonly List<DeadDropManager> cache = [];

        public readonly DeadDrop DeadDrop;

        public DeadDropManager(DeadDrop deadDrop)
        {
            DeadDrop = deadDrop;
        }

        public static List<DeadDropManager> GetAllInstances()
        {
            return cache;
        }

        public static List<DeadDrop> GetDeadDropsByDistance(Transform origin)
        {
            List<DeadDrop> deadDrops = [];

            foreach (DeadDropManager manager in cache)
            {
                deadDrops.Add(manager.DeadDrop);
            }

            deadDrops.Sort((x, y) => (x.transform.position - origin.position).sqrMagnitude.CompareTo((y.transform.position - origin.position).sqrMagnitude));

            return deadDrops;
        }

        public static DeadDropManager GetInstance(DeadDrop deadDrop) => GetInstance(deadDrop.GUID.ToString());

        public static DeadDropManager GetInstance(string guid)
        {
            if (guid == null)
            {
                return null;
            }

            return cache.Find(x => x.DeadDrop.GUID.ToString().Contains(guid));
        }

        public static void AddDeadDrop(DeadDrop deadDrop)
        {
            if (!DeadDropExists(deadDrop))
            {
                cache.Add(new(deadDrop));

                Utils.Logger.Debug("DeadDropManager", $"Dead drop added: {deadDrop.DeadDropName}");
            }
        }

        public static bool DeadDropExists(DeadDrop deadDrop) => DeadDropExists(deadDrop.GUID.ToString());

        public static bool DeadDropExists(string guid)
        {
            if (guid == null)
            {
                return false;
            }

            return cache.Any(x => x.DeadDrop.GUID.ToString().Contains(guid));
        }

        public static void Load()
        {
            for (int i = cache.Count - 1; i >= 0; i--)
            {
                cache[i].Destroy();
            }

            foreach (DeadDrop deadDrop in DeadDrop.DeadDrops)
            {
                AddDeadDrop(deadDrop);
            }
        }

        public void Destroy()
        {
            cache.Remove(this);
        }

        public bool IsFull()
        {
            return DeadDrop.Storage.ItemCount >= DeadDrop.Storage.SlotCount;
        }

        public Vector3 GetPosition()
        {
            return DeadDrop.transform.position;
        }
    }
}
