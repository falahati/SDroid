using System.Threading.Tasks;

namespace SDroid
{
    public interface IBotLogger
    {
        Task Debug(string scope, string message, params object[] formatParams);
        Task Error(string scope, string message, params object[] formatParams);
        Task Info(string scope, string message, params object[] formatParams);
        Task Success(string scope, string message, params object[] formatParams);
        Task Warning(string scope, string message, params object[] formatParams);
    }
}