using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Service.ServiceImplementaion
{
    public class InitializeDownload: IInitializeDonwload
    {
        public ResponseBase<ConfigurationSetting> InitConfigData()
        {
            int maxTries;
            int retryDelay;
            int numberOfSegments;
            int timeout;
            long minSizeSegment;
            string path;

            var conversion = int.TryParse(ConfigurationManager.AppSettings["NumberOfTrial"], out maxTries);
            if (!conversion)
            {
                Serilog.Log.Error( "Could not Fetch max trial from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial from config");
            }

            conversion = int.TryParse(ConfigurationManager.AppSettings["RetrialDelay"], out retryDelay);
            if (!conversion)
            {
                Serilog.Log.Error( "Could not Fetch RetrialDelay from config ");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch RetrialDelay from config ");
            }

            conversion = int.TryParse(ConfigurationManager.AppSettings["NumberOfSegments"], out numberOfSegments);
            if (!conversion)
            {
                Serilog.Log.Error( "Could not Fetch number of segments from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch number of segments from config");
            }


            conversion = int.TryParse(ConfigurationManager.AppSettings["Timeout"], out timeout);
            if (!conversion)
            {
                Serilog.Log.Error("Could not Fetch timeout from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch timeout from config");
            }


            conversion = long.TryParse(ConfigurationManager.AppSettings["MinSizeSegment"], out minSizeSegment);
            if (!conversion)
            {
                Serilog.Log.Error( "Could not Fetch timeout from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial ");
            }

            path = ConfigurationManager.AppSettings["LocalFilePath"];
            if (string.IsNullOrEmpty(path))
            {
                Serilog.Log.Error("Could not Fetch path from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch path from config");
            }
            var configurationSetting = new ConfigurationSetting()
            {
                NumberOfTrial = maxTries,
                LocalFilePath = path,
                MinSizeSegment = minSizeSegment,
                NumberOfSegments = numberOfSegments,
                RetrialDelay = retryDelay
            };
            
            return new ResponseBase<ConfigurationSetting>() { Denied = false,ReturnedValue = configurationSetting };
        }

        public ResponseBase<string> AllocateSpace(string url, string path)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = Path.GetFileName(uri.LocalPath);
                var localFile = path + "/" + filename;
                FileInfo fileInfo = new FileInfo(localFile);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    if (fileInfo.DirectoryName != null) Directory.CreateDirectory(fileInfo.DirectoryName);
                }

                if (fileInfo.Exists)
                {
                    // auto rename the file...
                    int count = 1;

                    string fileExitWithoutExt = Path.GetFileNameWithoutExtension(localFile);
                    string ext = Path.GetExtension(localFile);

                    string newFileName;

                    do
                    {
                        newFileName = PathHelper.GetWithBackslash(fileInfo.DirectoryName) + fileExitWithoutExt + $"_{count++}" + ext;
                    } while (File.Exists(newFileName));

                    localFile = newFileName;
                }

                using (FileStream fs = new FileStream(localFile, FileMode.Create, FileAccess.Write))
                {
                    fs.SetLength(0);
                }
                return new ResponseBase<string>() { Denied = false,ReturnedValue = localFile };
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                Serilog.Log.Error(ex, "List of URLs requiring authentication passed down");
                return new ResponseBase<string>() { Denied = true };
            }
        }
    }
}
