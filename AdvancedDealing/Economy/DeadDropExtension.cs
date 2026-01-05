using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AdvancedDealing.Persistence.Datas;
using System.Reflection;
using AdvancedDealing.Persistence;

#if IL2CPP
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
#elif MONO
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Product;
#endif

namespace AdvancedDealing.Economy
{
    public class DeadDropExtension
    {
        private static readonly List<DeadDropExtension> cache = [];

        public readonly DeadDrop DeadDrop;

        public string CashCollectionQuest;

        public string RefillProductsQuest;

        public DeadDropExtension(DeadDrop deadDrop)
        {
            DeadDrop = deadDrop;
            DeadDropData deadDropData = SaveModifier.Instance.SaveData.DeadDrops.Find(x => x.Identifier.Contains(deadDrop.GUID.ToString()));

            if (deadDropData == null)
            {
                deadDropData = new(deadDrop.GUID.ToString());
                deadDropData.LoadDefaults();
                SaveModifier.Instance.SaveData.DeadDrops.Add(deadDropData);
            }
            else if (deadDropData.ModVersion != ModInfo.VERSION)
            {
                DeadDropData oldDeadDropData = deadDropData;
                deadDropData = new(oldDeadDropData.Identifier);
                deadDropData.LoadDefaults();
                deadDropData.Merge(oldDeadDropData);

                Utils.Logger.Debug("DealerExtension", $"Data for {DeadDrop.DeadDropName} merged to newer Version");
            }

            PatchData(deadDropData);
        }

        public static List<DeadDropExtension> GetAllDeadDrops()
        {
            return cache;
        }

        public static List<DeadDrop> GetDeadDropsByDistance(Transform origin)
        {
            List<DeadDrop> deadDrops = [];

            foreach (DeadDropExtension deadDrop in cache)
            {
                deadDrops.Add(deadDrop.DeadDrop);
            }

            deadDrops.Sort((x, y) => (x.transform.position - origin.position).sqrMagnitude.CompareTo((y.transform.position - origin.position).sqrMagnitude));

            return deadDrops;
        }

        public static DeadDropExtension GetDeadDrop(DeadDrop deadDrop) => GetDeadDrop(deadDrop.GUID.ToString());

        public static DeadDropExtension GetDeadDrop(string guid)
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

                Utils.Logger.Debug("DeadDropExtension", $"Extension for dead drop created: {deadDrop.DeadDropName}");
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

        public static void Initialize()
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

        public static List<DeadDropData> FetchAllDeadDropDatas()
        {
            List<DeadDropData> dataCollection = [];

            foreach (DeadDropExtension deadDrop in cache)
            {
                dataCollection.Add(deadDrop.FetchData());
            }

            return dataCollection;
        }

        public void Destroy()
        {
            cache.Remove(this);
        }

        public DeadDropData FetchData()
        {
            DeadDropData data = new(DeadDrop.GUID.ToString());
            FieldInfo[] fields = typeof(DeadDropData).GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo localField = GetType().GetField(fields[i].Name);
                if (localField != null)
                {
                    fields[i].SetValue(data, localField.GetValue(this));
                }
            }

            return data;
        }

        public void PatchData(DeadDropData data)
        {
            DeadDropData oldData = FetchData();

            if (!oldData.IsEqual(data))
            {
                FieldInfo[] fields = typeof(DeadDropData).GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo localField = GetType().GetField(fields[i].Name);
                    localField?.SetValue(this, fields[i].GetValue(data));
                }

                Utils.Logger.Debug("DeadDropExtension", $"Data for {DeadDrop.DeadDropName} patched");
            }
        }

        public Dictionary<ProductItemInstance, ItemSlot> GetAllProducts()
        {
            Dictionary<ProductItemInstance, ItemSlot> products = [];

            foreach (ItemSlot slot in DeadDrop.Storage.ItemSlots)
            {
                if (slot.ItemInstance != null && slot.ItemInstance.Category == EItemCategory.Product)
                {
#if IL2CPP
                    ProductItemInstance product = slot.ItemInstance.Cast<ProductItemInstance>();
#elif MONO
                    ProductItemInstance product = slot.ItemInstance as ProductItemInstance;
#endif
                    products.Add(product, slot);
                }
            }

            return products;
        }

        public float GetCashAmount()
        {
            float amount = 0;

            foreach (ItemSlot slot in DeadDrop.Storage.ItemSlots)
            {
                if (slot.ItemInstance != null && slot.ItemInstance.Category == EItemCategory.Cash)
                {
#if IL2CPP
                    CashInstance cash = slot.ItemInstance.Cast<CashInstance>();
#elif MONO
                    CashInstance cash = slot.ItemInstance as CashInstance;
#endif
                    amount += cash.Balance;
                }
            }

            return amount;
        }

        public bool IsFull()
        {
            bool isFull = true;

            foreach (ItemSlot slot in DeadDrop.Storage.ItemSlots)
            {
                if (slot.Quantity <= 0)
                {
                    isFull = false;
                    break;
                }
            }

            return isFull;
        }

        public Vector3 GetPosition()
        {
            return DeadDrop.transform.position;
        }
    }
}
