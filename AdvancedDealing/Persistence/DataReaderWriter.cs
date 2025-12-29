using AdvancedDealing.Persistence.Datas;
using Newtonsoft.Json;
using System.IO;

#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
#elif MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
#endif

namespace AdvancedDealing.Persistence
{
    public static class DataReaderWriter
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private static string FilePath => Path.Combine(Singleton<LoadManager>.Instance.ActiveSaveInfo.SavePath, $"{ModInfo.Name}.json");

        public static SaveDataContainer LastLoadedData { get; private set; }

        public static string LastLoadedDataString { get; private set; }

        public static SaveDataContainer LoadFromFile()
        {
            SaveDataContainer data;
            string text;
            
            if (!File.Exists(FilePath))
            {
                string id = $"Savegame_{Singleton<LoadManager>.Instance.ActiveSaveInfo.SaveSlotNumber}";

                data = new SaveDataContainer(id);

                text = JsonConvert.SerializeObject(data, JsonSerializerSettings);
            }
            else
            {
                text = File.ReadAllText(FilePath);
                data = JsonConvert.DeserializeObject<SaveDataContainer>(text, JsonSerializerSettings);
            }

            LastLoadedData = data;
            LastLoadedDataString = text;

            Utils.Logger.Msg($"Data for {data.Identifier} loaded");

            return data;
        }

        public static void SaveToFile(SaveDataContainer data)
        {
            data ??= LoadFromFile();

            string text = JsonConvert.SerializeObject(data, JsonSerializerSettings);
            File.WriteAllText(FilePath, text);

            Utils.Logger.Msg($"Data for {data.Identifier} saved");
        }
    }
}
