using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace bnsmultiwindow
{
    [Serializable]
    class Settings
    {
        [DefaultValue("English")]
        internal string lang { get; set; }

        [DefaultValue(0)]
        internal int region { get; set; }

        [DefaultValue("")]
        public string CurrentHash { get; internal set; }

        internal static Settings Get()
        {
            try
            {
                using (FileStream r = new FileStream("settings.bin", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    IFormatter formatter = new BinaryFormatter();
                    Settings s = (Settings)formatter.Deserialize(r);
                    return s;

                }
            }
            catch
            {
                return new Settings();
            }
        }

        private static string[] ValidLanguages = { "English", "German", "French" };

        /// <summary>
        /// Make sure that our selected language is valid
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static bool ValidateLanguage(string input)
        {
            return input.Length > 0 && ValidLanguages.Contains(
                input.First().ToString().ToUpper() + String.Join("", input.Skip(1)) //uc first character
                );
        }

        /// <summary>
        /// make sure that our selected localization is valid
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static bool ValidateLocalization(int input)
        {
            return (input == 0 || input == 2);
        }

        /// <summary>
        /// Saves the current instance to settings.json
        /// </summary>
        internal void Save()
        {
            try
            {
                using (FileStream r = new FileStream("settings.bin", FileMode.Create, FileAccess.Write))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(r, this);

                }
            }
            catch
            {
                Console.WriteLine("[WARNING] We were not able to save the settings file.");
            }
        }
    }
}
