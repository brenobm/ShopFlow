using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IPushService
    {
        Task RegisterForPushNotificationsAsync();
    }
}
