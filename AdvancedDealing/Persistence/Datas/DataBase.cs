using System;
using System.Reflection;

namespace AdvancedDealing.Persistence.Datas
{
    public abstract class DataBase
    {
        public string DataType;

        public string ModVersion;

        public string Identifier;

        public DataBase(string identifier)
        {
            DataType = GetType().Name;
            ModVersion = ModInfo.Version;
            Identifier = identifier;
        }

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

        public virtual void Merge(DataBase other)
        {
            FieldInfo[] fields = GetType().GetFields();

            foreach (FieldInfo field in fields)
            {
                if (field.GetValue(other) == null || field.GetValue(this) != field.GetValue(other))
                {
                    field.SetValue(this, field.GetValue(other));
                }
            }
        }
    }
}
