using SKYPE4COMLib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Rogue_Legacy
{
    class Methods
    {
        public static string GetSkypePath()
        {
            foreach (var process in Process.GetProcessesByName("skype"))
            {
                return process.MainModule.FileName;
            }

            return "Error";
        }

        public static string RemoveRecordSeparators(string str) 
        {
            return Regex.Replace(str, "[^\x0d\x0a\x20-\x7e\t]", "");
        }

        public static void ProcessLine(string line)
        {
            try
            {
                string a = line.Substring(61); ;
                string b = a.Substring(0, a.IndexOf("0x"));

                string xa = Regex.Match(line, @"-r\d").Value;
                string x = line.Replace(xa, "^" + Regex.Match(xa, @"\d+").Value);
                string y = x.Substring(x.IndexOf('^') + 1);
                string z = "";
                int index = y.IndexOf(":");
                if (index > 0)
                    z = y.Substring(0, index);
                y = y.Split(':', '-')[1];

                string xa2 = Regex.Match(line, @"-l\d").Value;
                string x2 = line.Replace(xa2, ">" + Regex.Match(xa2, @"\d+").Value);
                string y2 = x2.Substring(x2.IndexOf('>') + 1);
                string z2 = string.Empty;
                int index2 = y2.IndexOf(":");
                if (index2 > 0)
                    z2 = y2.Substring(0, index2);

                AddToUserList(b, z, z2, y);
            }
            catch
            {

            }
        }

        public static void LoadActiveLog(RogueForm f)
        {
            foreach (string line in File.ReadAllLines(RogueForm.deobTempFilePath)) //loop all lines in file
            {
                if (line.Contains("-r") && line.Contains("PresenceManager") && !line.Contains("initial ping"))
                {
                    ProcessLine(line);
                }
            }
            f.AddToListView();
            f.UpdateAttributes();
        }

        public static void LoadInactiveLogs(RogueForm f)
        {
            foreach (string file in RogueForm.filesToRead) //loop all files
            {
                foreach (string line in File.ReadAllLines(file)) //loop all lines in file
                {
                    if (line.Contains("-r") && line.Contains("PresenceManager") && !line.Contains("initial ping"))
                    {
                        ProcessLine(line);
                    }
                }
            }
            f.AddToListView();
            f.UpdateAttributesMulti();
        }

        public static void AddToUserList(string w, string x, string y, string z)
        {
            if (!IsDuplicate(x) || x == "0.0.0.0")
            {
                RogueForm.dupCheck.Add(x);
                RogueForm.userList.Add(new SkypeUser() { SkypeHandle = RemoveRecordSeparators(w), RemoteIPAddress = x, LocalIPAddress = y, SkypePort = z });
            }
        }

        public static void CleanLog(string logPath)
        {
            string[] currentFile = File.ReadAllLines(logPath);
            for (int i = 0; i < currentFile.Length - 1; i++)
            {
                if (!currentFile[i].Contains(" PresenceManager: "))
                {
                    currentFile[i] = "";
                }

                else if (!currentFile[i].Contains("-s") || currentFile[i].Contains("nodeinfo") || currentFile[i].Contains("noticing"))
                {
                    currentFile[i] = "";
                }
            }

            File.WriteAllLines(logPath, currentFile);

            File.WriteAllLines(logPath, File.ReadAllLines(logPath).Where(l => !string.IsNullOrWhiteSpace(l))); //Remove Empty Lines

        }

        public static void MoveLogToTempDir()
        {
            foreach (string f in Directory.GetFiles(RogueForm.activeLogDirectory, "debug*.log"))
            {
                try
                {
                    File.Move(f, Path.Combine(RogueForm.savedLogsDirectory, Path.GetFileName(f)));
                }
                catch
                {
                    if (File.Exists(RogueForm.deobTempFilePath))
                    {
                        File.Delete(RogueForm.deobTempFilePath);
                    }
                    File.Copy(f, RogueForm.deobTempFilePath);
                }
                RogueForm.originalDeobFilePath = f;
            }
        } //so it can be edited

        public static bool IsDuplicate(string ip)
        {
            foreach (string a in RogueForm.dupCheck)
            {
                if (ip == a)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool KillAllSkypes()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("skype"))
                {
                    process.Kill();
                }

                return true;
            }
            catch
            {
                return false;
            }

        } //KILL ALL SKYPES

        public static bool SkypeIsRunning()
        {
            Process[] pname = Process.GetProcessesByName("skype");
            if (pname.Length == 0)
            {
                return false;
            }

            return true;
        }

        public static int GetNumberContacts() //I should have used a for loop, I know. I made this over a year ago. Cut me some slack.
        {
            int i = 0;
            foreach (User u in RogueForm.s.Friends)
            {
                i++;
            }
            return i;
        }

        public static Bitmap GetUserImage(string SkypeID)
        {
            dynamic command = new Command();
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }

            command.Command = string.Format("GET USER {0} AVATAR 1 {1}", SkypeID, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + SkypeID.Replace(":", "") + ".jpg");
            RogueForm.s.SendCommand(command);
            Thread.Sleep(100);
            return new Bitmap(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + SkypeID.Replace(":", "") + ".jpg");
        }
    }
}