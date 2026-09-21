using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JPXLpr.Novitec.NovaeyeWrapper;

namespace JPXLpr.Novitec
{
    public class LPREngine : IDisposable
    {
        public class LPRROISettingsStore
        {
            public Dictionary<string, LPRROISettings> Cameras { get; set; } = new Dictionary<string, LPRROISettings>(StringComparer.OrdinalIgnoreCase);
        }

        private NovaeyeWrapper m_novaeyeLpr;
        private bool m_isInitialized = false;
        private readonly object m_inferSync = new object();
        private const string LPR_ROI_SETTINGS_FILE = "lpr_roi_settings.json";
        private string m_cameraKey;

        public bool IsInitialized => m_isInitialized;

        //private Regex licensePlatePattern = new Regex(@"^\d{2,3}[가-힣]{1}\d{4}$");
        //private Regex licensePlatePatternWithReg = new Regex(@"^[가-힣]{2}\d{2,3}[가-힣]\d{4}$");

        public bool InitializeLpr(string cameraKey = null, bool useRoi = false)
        {
            try {
                if (m_isInitialized && m_novaeyeLpr != null) {
                    return true;
                }
                m_cameraKey = cameraKey;

                m_novaeyeLpr = new NovaeyeWrapper();

                int result = m_novaeyeLpr.Initialize();
                if (result == 0) {
                    m_isInitialized = true;
                    if (useRoi) {
                        LoadLPRROISettings();
                    }
                    else {
                        ClearROISettings();
                    }
                    Console.WriteLine("LPR engine initialized successfully.");
                    return true;
                }
                else {
                    Program.SaveLogString($"LPR initialize failed: wrapper returned error code {result}", true);
                    Cleanup();
                    return false;
                }
            }
            catch (Exception ex) {
                Program.SaveLogString( "failed to initialize LPR engine: " + ex.Message);
                Cleanup();
                return false;
            }
        }

        public List<string> RecognizedLP(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) {
                Program.SaveLogString("LPR recognize failed: input image data is null or empty", true);
                return new List<string>();
            }

            try {
                lock (m_inferSync) {
                    if (!m_isInitialized || m_novaeyeLpr == null) {
                        Program.SaveLogString("LPR recognize failed: engine is not initialized", true);
                        return new List<string>();
                    }

                    int numPlate = 0;
                    int recognizedResult = m_novaeyeLpr.Recognize(imageData, imageData.Length, ref numPlate);
                    return ProcessLprResult(recognizedResult, numPlate);
                }
            }
            catch (Exception ex) {
                Program.SaveLogString("failed to recognize LPR: " + ex.Message);
                return new List<string>();
            }
        }


        public List<string> RecognizedLP(string path)
        {
            if (string.IsNullOrEmpty(path)) {
                Program.SaveLogString("LPR recognize failed: input image path is null or empty", true);
                return new List<string>();
            }

            try {
                lock (m_inferSync) {
                    if (!m_isInitialized || m_novaeyeLpr == null) {
                        Program.SaveLogString("LPR recognize failed: engine is not initialized", true);
                        return new List<string>();
                    }

                    int numPlate = 0;
                    int recognizedResult = m_novaeyeLpr.Recognize(path, ref numPlate);
                    return ProcessLprResult(recognizedResult, numPlate);
                }
            }
            catch (Exception ex) {
                Program.SaveLogString( "failed to recognize LPR: " + ex.Message);
                return new List<string>();
            }
        }

        private List<string> ProcessLprResult(int recognizedResult, int numPlate)
        {
            List<string> lprResults = new List<string>();
            if (recognizedResult != 0) {
                Program.SaveLogString($"LPR recognize failed: recognize API returned error code {recognizedResult}", true);
                return lprResults;
            }

            if (numPlate == 0) {
                Program.SaveLogString( "LPR recognize failed: no plate detected (numPlate=0)", true);
                return lprResults;
            }
            else {
                NovaeyeWrapper.ALPRResult lprResult = new NovaeyeWrapper.ALPRResult();
                List<string> selectedLprResult = new List<string>();
                int retrieveResult;

                for (int i = 0; i < numPlate; i++) {
                    retrieveResult = m_novaeyeLpr.RetrieveResult(i, ref lprResult);

                    if (retrieveResult != 0) {
                        Program.SaveLogString( $"LPR recognize failed: RetrieveResult({i}) returned error code {retrieveResult}", true);
                        continue;
                    }
                    else if (string.IsNullOrEmpty(lprResult.text)) {
                        Program.SaveLogString( $"LPR recognize failed: RetrieveResult({i}) returned empty plate text", true);
                        continue;
                    }
                    else {
                        lprResults.Add(lprResult.text);
                    }
                }

                return lprResults;
            }
        }

        public ROI latestROI = new ROI();
        public void UpdateSettings(LPRROISettings settings)
        {
            if (settings == null) {
                Program.SaveLogString( $"LPR ROI apply failed: settings is null for camera '{m_cameraKey ?? "unknown"}'", true);
                return;
            }

            lock (m_inferSync) {
                if (m_novaeyeLpr == null) {
                    Program.SaveLogString( $"LPR ROI apply failed: engine is null for camera '{m_cameraKey ?? "unknown"}'", true);
                    return;
                }

                ALPRConfig aLPRConfig = new ALPRConfig
                {
                    mode = 0,
                    area = new ROI
                    {
                        x = settings.X,
                        y = settings.Y,
                        width = settings.Width,
                        height = settings.Height
                    },
                    max_detection = 100
                };
                int result = m_novaeyeLpr.SetROI(aLPRConfig.area);
                Console.WriteLine("Completed setting ROI");

                if (result != 0) {
                    Program.SaveLogString( $"LPR ROI apply failed: SetROI returned error code {result} for camera '{m_cameraKey ?? "unknown"}'", true);
                    Cleanup();
                    return;
                }

                int result2 = m_novaeyeLpr.GetROI(ref aLPRConfig.area);
                Console.WriteLine("ROI settings: " +
                    $"X={aLPRConfig.area.x}, Y={aLPRConfig.area.y}, Width={aLPRConfig.area.width}, Height={aLPRConfig.area.height}");
                if (result2 != 0) {
                    Program.SaveLogString( $"LPR ROI verify failed: GetROI returned error code {result2} for camera '{m_cameraKey ?? "unknown"}'", true);
                    return;
                }

                m_isInitialized = true;
                latestROI = aLPRConfig.area;
            }
        }

        public void ClearROISettings()
        {
            lock (m_inferSync) {
                if (m_novaeyeLpr == null) {
                    Program.SaveLogString( $"LPR ROI clear failed: engine is null for camera '{m_cameraKey ?? "unknown"}'", true);
                    return;
                }

                ROI area = new ROI
                {
                    x = 0,
                    y = 0,
                    width = 0,
                    height = 0
                };

                int result = m_novaeyeLpr.SetROI(area);
                if (result != 0) {
                    Program.SaveLogString( $"LPR ROI clear failed: SetROI returned error code {result} for camera '{m_cameraKey ?? "unknown"}'", true);
                    return;
                }

                latestROI = area;
            }
        }

        public ROI GetROI()
        {
            return latestROI;
        }


        public static LPRROISettings LoadSettingsForCamera(string cameraKey)
        {
            if (string.IsNullOrWhiteSpace(cameraKey)) {
                Program.SaveLogString( "LPR ROI load failed: camera key is null or empty, using default settings", true);
                return new LPRROISettings();
            }

            var store = LoadSettingsStore();
            if (store.Cameras != null &&
                store.Cameras.TryGetValue(cameraKey, out var roi)) {
                return roi;
            }

            Program.SaveLogString( $"LPR ROI settings not found: camera='{cameraKey}', using default settings", true);
            return new LPRROISettings();
        }

        public static bool SaveSettingsForCamera(string cameraKey, LPRROISettings settings)
        {
            if (settings == null) {
                Program.SaveLogString("LPR ROI save failed: settings is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(cameraKey)) {
                Program.SaveLogString("LPR ROI save failed: camera key is null or empty");
                return false;
            }

            var store = LoadSettingsStore();
            store.Cameras[cameraKey] = settings;

            try {
                string json = JsonConvert.SerializeObject(store, Formatting.Indented);
                File.WriteAllText(LPR_ROI_SETTINGS_FILE, json);
                Program.SaveLogString(
                    $"LPR ROI settings saved: camera='{cameraKey}', X={settings.X}, Y={settings.Y}, Width={settings.Width}, Height={settings.Height}");
                return true;
            }
            catch (Exception ex) {
                Program.SaveLogString( "Failed to save LPR ROI settings: " + ex.Message);
                return false;
            }
        }

        private static LPRROISettingsStore LoadSettingsStore()
        {
            var defaultStore = new LPRROISettingsStore();
            try {
                if (!File.Exists(LPR_ROI_SETTINGS_FILE)) {
                    return defaultStore;
                }

                string json = File.ReadAllText(LPR_ROI_SETTINGS_FILE);
                if (string.IsNullOrWhiteSpace(json)) {
                    return defaultStore;
                }

                var store = JsonConvert.DeserializeObject<LPRROISettingsStore>(json);
                if (store != null && store.Cameras != null) {
                    return store;
                }

            }
            catch (Exception ex) {
                Program.SaveLogString("Failed to load LPR ROI settings: " + ex.Message);
            }

            return defaultStore;
        }

        private void LoadLPRROISettings()
        {
            try {
                LPRROISettings roiSettings = LoadSettingsForCamera(m_cameraKey);
                UpdateSettings(roiSettings);
            }
            catch (Exception ex) {
                Program.SaveLogString("Failed to load LPR ROI settings in engine: " + ex.Message);
            }
        }

        private void Cleanup()
        {
            lock (m_inferSync) {
                if (m_novaeyeLpr == null) {
                    return;
                }

                try {
                    m_novaeyeLpr.Dispose();
                }
                catch (Exception ex) {
                    Program.SaveLogString("failed to dispose LPR engine: " + ex.Message);
                    return;
                }
                finally {
                    m_novaeyeLpr = null;
                    m_isInitialized = false;
                }
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
