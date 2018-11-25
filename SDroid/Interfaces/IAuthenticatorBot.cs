using System.Threading.Tasks;
using SDroid.SteamMobile;

namespace SDroid.Interfaces
{
    public interface IAuthenticatorBot : ISteamBot
    {
        IAuthenticatorSettings BotAuthenticatorSettings { get; }
        Task OnAuthenticatorConfirmationAvailable(Confirmation confirmation);
        Task OnAuthenticatorMissing();
    }
}