using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace APSMain.Tcpip
{
    public class FTPUtil
    {
        public string ftpHost { get; set; }
        public string ftpUser { get; set; }
        public string ftpPass { get; set; }

        public FTPUtil(string Host, string UserId, string UserPW)
        {
            ftpHost = Host;
            ftpUser = UserId;
            ftpPass = UserPW;
        }

        public async Task ftpImageUpload(string filename, string desfile)
        {
            string remoteDir;
            string remotePath;
            DateTime now = DateTime.Now;
            if (!File.Exists(filename))
            {
                //Console.WriteLine("로컬 파일 없음: " + filename);
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    FtpClient client = new FtpClient(ftpHost);
                    client.Credentials = new NetworkCredential(ftpUser, ftpPass);
                    client.Config.DataConnectionType = FtpDataConnectionType.AutoActive;
                    client.Config.ConnectTimeout = 4000; // 10초
                    client.Connect();
                    if (client.IsConnected)
                    {
                        remoteDir = $"{now.Year}";
                        client.CreateDirectory(remoteDir, true);

                        remoteDir = $"{remoteDir}/{now.Month:D2}";
                        client.CreateDirectory(remoteDir, true);

                        remoteDir = $"{remoteDir}/{now.Day:D2}";
                        client.CreateDirectory(remoteDir, true);

                        remotePath = $"{remoteDir}/" + desfile;

                        // FluentFTP는 자동 디렉토리 생성 지원
                        client.UploadFile(filename, remotePath, FtpRemoteExists.Overwrite, false);
                        client.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTP Upload Error: {ex.Message}");
                }
            });
        }

        public async Task<bool> ftpImageDownload(string imgName, string localname)
        { // 1I40022025071610270200000001_서울1가1001
            string remoteDir = $"{imgName.Substring(6, 4)}/{imgName.Substring(10, 2)}/{imgName.Substring(12, 2)}";
            string remotePath = $"{remoteDir}/{imgName}";
            bool ret = false;
            try {
                File.Delete(localname);
            }
            catch { }
            await Task.Run(() =>
            {
                try
                {
                    FtpClient client = new FtpClient(ftpHost);
                    client.Credentials = new NetworkCredential(ftpUser, ftpPass);
                    client.Config.DataConnectionType = FtpDataConnectionType.AutoActive;
                    client.Config.ConnectTimeout = 4000; // 10초
                    client.Connect();
                    if (client.IsConnected)
                    {
                        client.DownloadFile(localname, remotePath,FtpLocalExists.Overwrite);
                        client.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FTP Download Error: {ex.Message}");
                }
                finally
                {
                    if ( File.Exists(localname) )
                    {
                        ret = true;
                    }
                    else
                    {
                        ret = false;
                    }
                }
            });

            return ret;
        }
    }
}
