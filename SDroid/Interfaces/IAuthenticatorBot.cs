using System.Threading.Tasks;
using SDroid.SteamMobile;

namespace SDroid.Interfaces
{
    public interface IAuthenticatorBot
    {
        IAuthenticatorSettings BotAuthenticatorSettings { get; }
        Task OnAuthenticatorConfirmationAvailable(Confirmation confirmation);
        Task OnAuthenticatorMissing();
    }
}