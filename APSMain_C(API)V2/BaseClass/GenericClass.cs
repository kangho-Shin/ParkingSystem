using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;

namespace APSMain.BaseClass
{
    public enum EnumVanType : int
    {
        NONE = 0,
        SMATRO,
        KICC,
        KOCES
    }

    public class CarEntryInfo
    {
        public long ParkingSessionId { get; set; }
        public string? Carnum { get; set; }
        public DateTime InDate { get; set; } // 또는 DateTime
        public int Inhour { get; set; }
        public int Inmin { get; set; }
        public string? Type { get; set; }
    }

    public static class ConfigHelper
    {
        public static void SaveSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings[key] != null)
                config.AppSettings.Settings[key].Value = value;
            else
                config.AppSettings.Settings.Add(key, value);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }

    public interface IActiveForm
    {
        void MoveTo(Point screenLocation);

        string ZoomInOut();

        void ZoomMovedQuard(int pos);

        void OnConfirm();
        void OnGoHome();

        void OnCancel();

        void GiveFocus();

        void ContrastCall();

        void OnContrastChanged(bool on);

        void DoActiveButton();
        public Action<Form,Keys,int,int>? RouteArrow { get; set; }
    }
}
