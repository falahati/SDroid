using System.IO;
using ConsoleUtilities;
using Newtonsoft.Json;
using SDroid.Interfaces;

namespace SDroidTest
{
    internal static class SettingsExtension
    {
        public static bool Clear<T>() where T : IBotSettings
        {
            if (!File.Exists(typeof(T).Name + ".json"))
            {
                return true;
            }

            try
            {
                File.Delete(typeof(T).Name + ".json");

                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public static bool Exist<T>() where T : IBotSettings
        {
            return File.Exists(typeof(T).Name + ".json");
        }

        public static T Load<T>() where T : IBotSettings, new()
        {
            if (File.Exists(typeof(T).Name + ".json"))
            {
                try
                {
                    var json = File.ReadAllText(typeof(T).Name + ".json");

                    var retVal = JsonConvert.DeserializeObject<T>(json);

                    if (retVal != null)
                    {
                        return retVal;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return new T
            {
                Username = ConsoleWriter.Default.PrintQuestion("Username")
            };
        }

        public static void Save<T>(this T settings) where T : IBotSettings
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(typeof(T).Name + ".json", json);
            }
            catch
            {
                // ignored
            }
        }
    }
}