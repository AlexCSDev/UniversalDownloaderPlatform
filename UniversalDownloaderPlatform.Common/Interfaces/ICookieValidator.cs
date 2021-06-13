using System.Net;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICookieValidator
    {
        Task ValidateCookies(CookieContainer cookieContainer);
    }
}
