using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APSMain.Tcpip
{
    public static class ImageDownloadHelper
    {
        private static string _serverBaseUrl = "";
        private static string _localBaseUrl = "";

        private static int _imgCount = 1;
        private static readonly object _lock = new object();

        public static void Init(string serverBaseUrl)
        {
            _serverBaseUrl = serverBaseUrl;
            _localBaseUrl = Path.Combine(Application.StartupPath, "IMGDATA");

            if (!Directory.Exists(_localBaseUrl))
                Directory.CreateDirectory(_localBaseUrl);
        }

        private static string GetLocalPath()
        {
            lock (_lock) {
                if (!Directory.Exists(_localBaseUrl))
                    Directory.CreateDirectory(_localBaseUrl);

                string fileName = $"Temp_{_imgCount:D3}.jpg";

                _imgCount++;
                if (_imgCount >= 20)
                    _imgCount = 1;

                return Path.Combine(_localBaseUrl, fileName);
            }
        }

        public static async Task<string?> DownloadImageAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            string localPath = GetLocalPath();

            try {
                if (File.Exists(localPath))
                    File.Delete(localPath);
            }
            catch { }

            string url = _serverBaseUrl + "/api/image/download?fileName=" + Uri.EscapeDataString(fileName);

            try {
                using var client = new HttpClient();
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                byte[] data = await response.Content.ReadAsByteArrayAsync();

                if (data == null || data.Length == 0)
                    return null;

                await File.WriteAllBytesAsync(localPath, data);

                return localPath;
            }
            catch {
                return null;
            }
        }

        public static async Task<bool> ShowImageAsync(PictureBox pictureBox, string fileName)
        {
            string? localPath = await DownloadImageAsync(fileName);

            if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                return false;

            try {
                using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var image = System.Drawing.Image.FromStream(fs);

                var oldImage = pictureBox.Image;
                pictureBox.Image = (System.Drawing.Image)image.Clone();
                oldImage?.Dispose();

                return true;
            }
            catch {
                return false;
            }
        }
    }
}
/*
  ImageDownloadHelper.Init("http://192.168.0.190:5284");
  await _imageDown.ShowImageAsync(pictureBox1, "1I40012026042305523800001121_986너9554.JPG");

*/