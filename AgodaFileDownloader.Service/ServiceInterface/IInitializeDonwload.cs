using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface IInitializeDonwload
    {
        ResponseBase<ConfigurationSetting> InitConfigData();
        ResponseBase<string> AllocateSpace(string url,string path);

    }
}