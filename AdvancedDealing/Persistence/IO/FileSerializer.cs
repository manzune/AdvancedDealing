using Newtonsoft.Json;
using System.IO;
using System;



#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
#endif

namespace AdvancedDealing.Persistence.IO
{
    public static class FileSerializer
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static Type LastLoadedDataType { get; private set; }

        public static string LastLoadedDataString { get; private set; }

        public static bool LoadFromFile<T>(string filePath, out T data) where T : struct
        {
            if (File.Exists(filePath))
            {
                string text = File.ReadAllText(filePath);
                data = JsonConvert.DeserializeObject<T>(text, JsonSerializerSettings);

                LastLoadedDataType = data.GetType();
                LastLoadedDataString = text;

                Utils.Logger.Debug($"Loaded from file: {filePath}");

                return true;
            }

            data = default;

            return false;
        }

        public static void SaveToFile<T>(string filePath, T data) where T : struct
        {
            string text = JsonConvert.SerializeObject(data, JsonSerializerSettings);
            File.WriteAllText(filePath, text);

            Utils.Logger.Debug($"Saved to file: {filePath}");
        }
    }
}
