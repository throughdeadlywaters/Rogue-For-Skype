using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SKYPE4COMLib;
using System.IO;
using System.Diagnostics;
using System.Net;
using Microsoft.Win32;
using System.Configuration;
using System.Xml;
using System.Threading;
using DevExpress.XtraEditors;

namespace Rogue_Legacy
{
    public partial class RogueForm : XtraForm
    {
        public static string deobTempFilePath = Path.Combine(Path.GetTempPath() + "skypedeob", "tempLog.log");

        public static string deobTempDirectory = Path.GetTempPath() + @"skypedeob";

        public static string activeLogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Deob");

        public static string savedLogsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saved Logs");

        public static string dumpDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dumps");

        public static string activeLogPath = Path.Combine(deobTempDirectory, "tempLog.log");

        public static string originalDeobFilePath = string.Empty;

        public static List<SkypeUser> userList = new List<SkypeUser>();

        public static List<string> dupCheck = new List<string>();

        public static List<string> filesToRead = new List<string>();

        public static Skype s = new Skype();

        public static WebClient g = new WebClient();

        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public RogueForm()
        {
            InitializeComponent();
        }

        private void RogueForm_Load(object sender, EventArgs e)
        {
            deobTempDirectory = Path.GetTempPath() + "skypedeob";

            if (!Directory.Exists(savedLogsDirectory))
                Directory.CreateDirectory(savedLogsDirectory);

            if (!Directory.Exists(dumpDirectory))
                Directory.CreateDirectory(dumpDirectory);

            if (!Directory.Exists(deobTempDirectory))
                Directory.CreateDirectory(deobTempDirectory);

            File.Delete(deobTempFilePath);

            try
            {
                RegistryKey rkey = Registry.CurrentUser.OpenSubKey(@"Software\Skype\Phone\UI\General", true);
                if ((string)rkey.GetValue("Logging") != "SkypeDebug2003")
                {
                    rkey.SetValue("Logging", "SkypeDebug2003");
                }
            }

            catch
            {
                RegistryKey rkey = Registry.CurrentUser.CreateSubKey(@"Software\Skype\Phone\UI\General");
                rkey.SetValue("Logging", "SkypeDebug2003");
            }


            DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(config.AppSettings.Settings["currentTheme"].Value);

            Methods.MoveLogToTempDir();

            foreach (string s in Directory.GetFiles(activeLogDirectory, "*.log"))
            {
                if (s.Contains("abch") || s.Contains("eas"))
                {
                    File.Delete(s);
                }   
            }

            if (config.AppSettings.Settings["alwaysTop"].Value == "true")
            {
                TopMost = true;
                onTopSwitch.IsOn = true;
            }

            if (config.AppSettings.Settings["fullName"].Value == "true")
            {
                fullNameToggle.IsOn = true;
            }

            if (config.AppSettings.Settings["ipInNames"].Value == "true")
            {
                autoIPSwitch.IsOn = true;
            }

            comboBoxEdit2.Text = config.AppSettings.Settings["currentTheme"].Value;
        }

        private void LadBTN_Click(object sender, EventArgs e)
        {
            ladBTN.Enabled = false;
            Methods.MoveLogToTempDir();
            if (File.Exists(deobTempFilePath))
            {
                Methods.CleanLog(deobTempFilePath);
                ResetAll();
                Methods.LoadActiveLog(this);
            }

            else
            {
                XtraMessageBox.Show("No active deob log available. Try re-opening Skype, or load a saved log instead", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ladBTN.Enabled = true;
        }

        public void UpdateAttributes()
        {
            fileSizeLabel.Text = "Log Size(After Cleaning): " + (new FileInfo(deobTempFilePath)).Length / 1024 + "kb";
            fileCreationDateLabel.Text = "Log Creation Date: " + File.GetCreationTime(originalDeobFilePath).ToString();
            ipLabel.Text = "No. Of IP's Matched: " + userList.Count;
        }

        public void UpdateAttributesMulti()
        {
            fileSizeLabel.Text = "Combined Log Size(After Cleaning): " + MultiFileLength() + "kb";
            ipLabel.Text = "No. Of IP's Matched: " + userList.Count;
            if (filesToRead.Count > 1)
            {
                fileCreationDateLabel.Text = "Log Creation Date: N/A(Multiple Files)";
            }

            else
            {
                fileCreationDateLabel.Text = "Log Creation Date: " + File.GetCreationTime(GetListItem()).ToString();
            }
        }

        public void ResetAll()
        {
            filesToRead.Clear();
            gridView1.Columns.Clear();
            dupCheck.Clear();
            userList.Clear();
            gridControl1.DataSource = null;
            fileSizeLabel.Text = "Log Size: N/A";
            fileCreationDateLabel.Text = "Log Creation Date: N/A";
            ipLabel.Text = "No. Of IP's Matched: N/A";
        }

        public void AddToListView()
        {
            if (config.AppSettings.Settings["fullName"].Value == "true" && Methods.SkypeIsRunning())
            {
                foreach (SkypeUser u in userList)
                {
                    foreach (User x in s.Friends)
                    {
                        if (u.SkypeHandle == x.Handle)
                        {
                            u.SkypeFullName = x.FullName;
                        }
                    }
                }
            }

            gridControl1.DataSource = userList;

            dupCheck.Clear();

            if (autoIPSwitch.IsOn)
            {
                foreach (User u in s.Friends)
                {
                    foreach (SkypeUser a in userList)
                    {
                        if (a.SkypeHandle == u.Handle)
                        {
                            if (u.DisplayName == string.Empty)
                            {
                                u.DisplayName = u.Handle + " | " + a.RemoteIPAddress;
                            }
                            else
                            {
                                u.DisplayName = u.FullName + " | " + a.RemoteIPAddress;
                            }
                        }
                    }
                }
            }
        }

        public string GetListItem()
        {
            foreach (string s in filesToRead)
            {
                return s;
            }

            return "";
        }

        public long MultiFileLength()
        {
            long total = 0;
            foreach (string f in filesToRead)
            {
                total += new FileInfo(f).Length / 1024;
            }
            return total;
        }

        private void CustomLog_Click(object sender, EventArgs e)
        {
            lmlBTN.Enabled = false;
            Methods.MoveLogToTempDir();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Title = "Select logs file to be loaded";
            ofd.Filter = "Log Files (.log)|*.log";
            ofd.InitialDirectory = savedLogsDirectory;
            DialogResult d = ofd.ShowDialog();
            if (d == DialogResult.OK)
            {
                ResetAll();
                foreach (string file in ofd.FileNames)
                {
                    Methods.CleanLog(file);
                    filesToRead.Add(file);

                }
                Methods.LoadInactiveLogs(this);
            }
            lmlBTN.Enabled = true;
        }

        private void OpenDeobSkype_Click(object sender, EventArgs e)
        {
            simpleButton2.Enabled = false;
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Methods.KillAllSkypes();
                Process.Start(activeLogDirectory + @"\skype.exe");
            }).Start();
            simpleButton2.Enabled = true;
        }

        private void OpenNormalSkype_Click(object sender, EventArgs e)
        {
            simpleButton1.Enabled = false;
            Methods.KillAllSkypes();
            File.Delete(deobTempFilePath);
            Process.Start(@"C:\Program Files (x86)\Skype\Phone\skype.exe");
            simpleButton1.Enabled = true;
        }

        private void HyperlinkLabelControl1_Click(object sender, EventArgs e)
        {
            Process.Start(savedLogsDirectory);
        }

        private void HyperlinkLabelControl2_Click(object sender, EventArgs e)
        {
            Process.Start(activeLogDirectory);
        }

        private void SimpleButton3_Click(object sender, EventArgs e)
        {
            foreach (User u in s.Friends)
            {
                u.DisplayName = "";
            }
        }

        private void ToggleSwitch1_Toggled(object sender, EventArgs e)
        {
            if (onTopSwitch.IsOn)
            {
                TopMost = true;
            }

            else
            {
                TopMost = false;
            }
        }

        private void HyperlinkLabelControl5_Click(object sender, EventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void SaveDump_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Dump";
            sfd.Filter = "Text Files (.txt)|*.txt";
            sfd.InitialDirectory = dumpDirectory;
            DialogResult d = sfd.ShowDialog();
            if (d == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.WriteLine("Format: Skype Handle, Remote IP Address, Local IP Address, Port #");
                    foreach (SkypeUser u in userList)
                    {
                        sw.WriteLine(u.SkypeHandle + ", " + u.RemoteIPAddress + ", " + u.LocalIPAddress + ", " + u.SkypePort);
                    }

                    sw.Flush();
                    sw.Close();
                }

                Process.Start(sfd.FileName);
            }
        }

        private void SaveProfile_Click(object sender, EventArgs e)
        {
            s.CurrentUserProfile.PhoneMobile = mobilePhoneTextBox.Text;
            s.CurrentUserProfile.PhoneOffice = officePhoneTextBox.Text;
            s.CurrentUserProfile.PhoneHome = homePhoneTextBox.Text;
            s.CurrentUserProfile.FullName = displayNameTextBox.Text;
            s.CurrentUserProfile.Province = stateTextBox.Text;
            s.CurrentUserProfile.City = cityTextBox.Text;
            s.CurrentUserProfile.About = aboutMeTextBox.Text;
            if (checkEdit1.Checked)
            {
                s.CurrentUserProfile.RichMoodText = "<blink>" + moodTextBox.Text + "</blink>";
            }

            else
            {
                s.CurrentUserProfile.RichMoodText = moodTextBox.Text;
            }
            var cmd = new Command();
            if (!File.Exists(@"C:\savedimg.bmp"))
            {
                pictureEdit1.Image.Save(@"C:\savedimg.bmp");
            }
            else
            {
                File.Delete(@"C:\savedimg.bmp");
                pictureEdit1.Image.Save(@"C:\savedimg.bmp");
            }
            cmd.Command = string.Format("SET AVATAR 1 {0}", @"C:\savedimg.bmp");
            s.SendCommand(cmd);
        }

        private void LoadProfile2_Click(object sender, EventArgs e)
        {
            saveProfileBTN.Enabled = true;
            mobilePhoneTextBox.Text = s.CurrentUserProfile.PhoneMobile;
            officePhoneTextBox.Text = s.CurrentUserProfile.PhoneOffice;
            homePhoneTextBox.Text = s.CurrentUserProfile.PhoneHome;
            displayNameTextBox.Text = s.CurrentUser.FullName;
            stateTextBox.Text = s.CurrentUserProfile.Province;
            cityTextBox.Text = s.CurrentUserProfile.City;
            aboutMeTextBox.Text = s.CurrentUserProfile.About;
            moodTextBox.Text = s.CurrentUserProfile.RichMoodText;
            moodTextBox.Text = moodTextBox.Text.Replace("<blink>", "");
            moodTextBox.Text = moodTextBox.Text.Replace("</blink>", "");
            moodTextBox.Text = moodTextBox.Text.Replace("&apos;", "'");
            pictureEdit1.Image = Methods.GetUserImage(s.CurrentUserHandle);
        }

        private void OpenDumpDir_Click(object sender, EventArgs e)
        {
            Process.Start(dumpDirectory);
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            if (onTopSwitch.IsOn)
            {
                config.AppSettings.Settings["alwaysTop"].Value = "true";
            }

            else
            {
                config.AppSettings.Settings["alwaysTop"].Value = "false";
            }

            if (autoIPSwitch.IsOn)
            {
                config.AppSettings.Settings["ipInNames"].Value = "true";
            }

            else
            {
                config.AppSettings.Settings["ipInNames"].Value = "false";
            }

            if (fullNameToggle.IsOn)
            {
                config.AppSettings.Settings["fullName"].Value = "true";
            }

            else
            {
                config.AppSettings.Settings["fullName"].Value = "false";
            }

            config.AppSettings.Settings["currentTheme"].Value = comboBoxEdit2.Text;
            DevExpress.LookAndFeel.UserLookAndFeel.Default.SetSkinStyle(comboBoxEdit2.Text);
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void LoadProfile_Click(object sender, EventArgs e)
        {
            saveProfileBTN.Enabled = true;
            foreach (User u in s.Friends)
            {
                if (u.Handle == textEdit2.Text)
                {
                    mobilePhoneTextBox.Text = u.PhoneMobile;
                    officePhoneTextBox.Text = u.PhoneOffice;
                    homePhoneTextBox.Text = u.PhoneHome;
                    displayNameTextBox.Text = u.FullName;
                    stateTextBox.Text = u.Province;
                    cityTextBox.Text = u.City;
                    aboutMeTextBox.Text = u.About;
                    moodTextBox.Text = u.RichMoodText;
                    moodTextBox.Text = moodTextBox.Text.Replace("<blink>", "");
                    moodTextBox.Text = moodTextBox.Text.Replace("</blink>", "");
                    pictureEdit1.Image = Methods.GetUserImage(u.Handle);
                }
            }
        }

        private void GridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                Clipboard.SetText(gridView1.GetFocusedDisplayText());
                e.Handled = true;
            }
        }

        private void GridView1_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            if (e.Column.FieldName == "RemoteIPAddress")
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(g.DownloadString("http://ip-api.com/xml/" + e.CellValue.ToString()));
                    ctrylbl.Text = "Country: " + doc.DocumentElement.SelectSingleNode("country").InnerText;
                    rgnlabel.Text = "Region: " + doc.DocumentElement.SelectSingleNode("regionName").InnerText;
                    ziplbl.Text = "ZIP Code : " + doc.DocumentElement.SelectSingleNode("zip").InnerText;
                    isplbl.Text = "ISP: " + doc.DocumentElement.SelectSingleNode("isp").InnerText;
                }
                catch
                {

                }

                ipadlbl.Text = "IP Address: " + e.CellValue.ToString();
            }
        }

        private void HyperlinkLabelControl3_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/throughdeadlywaters");
        }
    }
}
