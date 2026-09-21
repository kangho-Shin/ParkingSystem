using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;


namespace APSMain
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        public static void BackupConfig()
        {
            string configPath = Application.ExecutablePath + ".config";
            string backupPath = configPath + ".bak";

            if (File.Exists(configPath))
                File.Copy(configPath, backupPath, true);
        }

        public static void RestoreConfig()
        {
            string configPath = Application.ExecutablePath + ".config";
            string backupPath = configPath + ".bak";

            if (File.Exists(backupPath))
                File.Copy(backupPath, configPath, true);
        }

        public static void RestartApp()
        {
            Process.Start(Application.ExecutablePath);
            Application.Exit();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            BackupConfig();
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            RestoreConfig();
        }
    }
}
