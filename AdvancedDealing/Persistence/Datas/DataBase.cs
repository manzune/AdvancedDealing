using System;
using System.Reflection;

#if IL2CPP
using Il2CppFishNet.Broadcast;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
#elif MONO
using FishNet.Broadcast;
#endif

namespace AdvancedDealing.Persistence.Datas
{
    public abstract class DataBase(string identifier)
    {
        public virtual string DataType => GetType().Name;

        public string ModVersion = ModInfo.Version;

        public string Identifier = identifier;

        public virtual bool IsEqual(object other)
        {
            if (!GetType().Equals(other.GetType()))
            {
                throw new Exception($"Tried to compare {GetType()} with {other.GetType()}");
            }

            bool isEqual = true;

            FieldInfo[] fields = GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.GetValue(this) != field.GetValue(other))
                {
                    isEqual = false;
                    break;
                }
            }

            return isEqual;
        }
    }
}
