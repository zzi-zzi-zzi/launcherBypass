using bnsmultiwindow.BnS;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bnsmultiwindow
{
    class Program
    {
        private static byte[] _encryptionKey = Utils.Byte.GetBytes("bns_obt_kr_2014#");

        public static string DataPath { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("BnS MultiWindow");

            Settings s = Settings.Get();
            
            if (s.CurrentHash == null || s.CurrentHash.Length == 0)
                BuildSettings(s);

            var InstallPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\NCWest\BnS", "BaseDir", RegistryValueKind.String);
            string DataPath = null;
            if (InstallPath == null && File.Exists(@".\Client.exe"))
            {
                DataPath = @"..\contents\Local\NCWEST\data\";
            }
            else if (InstallPath == null && DataPath == null)
            {
                Console.WriteLine("failed to find client.exe please stick this in the same folde.\r\n<enter> to exit");
                Console.ReadLine();
                return;
            }
            else
            {
                DataPath = Path.Combine(InstallPath, "contents", "Local", "NCWEST", "data");
            }
            //Launch(InstallPath, s);

            Console.WriteLine(String.Format("Using install directory: {0}", InstallPath));

            var FileHash = getFileHash(DataPath);

            if (s.CurrentHash != null && FileHash == s.CurrentHash)
            {
                Launch(InstallPath, s);
                return;
            }
            
            if (!Directory.Exists(Path.Combine(DataPath, "backup")))
            {
                Directory.CreateDirectory(Path.Combine(DataPath, "backup"));
                File.Copy(Path.Combine(DataPath, "config.dat"), Path.Combine(DataPath, "backup", "config.dat"));
            }

            UpdateCFG(DataPath);
            
            s.CurrentHash = getFileHash(DataPath);
            s.Save();

            Launch(InstallPath, s);
            return;
        }

        /// <summary>
        /// This is here for scope. want the gc to clean up the cfg for me.
        /// </summary>
        /// <param name="dataPath"></param>
        private static void UpdateCFG(string DataPath)
        {
            var CFG = new uosedalb(Path.Combine(DataPath, "config.dat"), _encryptionKey);
            CFG.ReplaceSetting("system.config2.xml", "use-web-launching", "false");
            CFG.Save();
        }

        private static void BuildSettings(Settings s)
        {
            Console.WriteLine("This appears to be your first time running multiwindow.");
            Console.WriteLine("We need to setup a few things.");
            String inlang = "";
            while (!Settings.ValidateLanguage(inlang.Trim()))
            {
                Console.Write("What language should the game run: English, German, French.\r\nType answer and press <enter>: ");
                inlang = Console.ReadLine();
            }
            s.lang = inlang.Trim();
            int value = -1;
            while(!Settings.ValidateLocalization(value))
            {
                Console.Write("What region should we use? 0 = NA 1 = EU.\r\nType answer and press <enter>: ");
                if (!int.TryParse(Console.ReadLine().Trim(), out value))
                {
                    value = -1;
                }
            }
            s.Save();
        }

        private static void Launch(string installPath, Settings s)
        {
            if (installPath == null)
                installPath = @".\Client.exe";
            else
                installPath = Path.Combine(installPath, "bin", "Client.exe");
            Process proc = new Process();
            proc.StartInfo.FileName = installPath;
            proc.StartInfo.Arguments = "-lang:" + s.lang + " -lite:2 -region:" + s.region + " /sesskey /launchbylauncher  /CompanyID:12 /ChannelGroupIndex:-1 -USEALLAVAILABLECORES";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            try
            {
                proc.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error: Could not start Client.exe!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Please post error in forums for help. <enter> to close.");
                Console.ReadLine();
            }
        }

        private static string getFileHash(string dataPath)
        {
            using (FileStream stream = File.OpenRead(Path.Combine(dataPath, "config.dat")))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }
    }
}