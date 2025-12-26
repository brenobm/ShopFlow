namespace BrenoMachado.ShopFlow.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task<bool> SignUpAsync(string username, string password, string email);
    Task SignOutAsync();
    Task<bool> IsAuthenticatedAsync();
    string GetCurrentUserId();
    string GetIdToken();
}
