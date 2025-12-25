using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon;

namespace BrenoMachado.ShopFlow.Services
{
    public class AuthService : IAuthService
    {
        private readonly AmazonCognitoIdentityProviderClient _providerClient;
        private readonly CognitoUserPool _userPool;
        private CognitoUser _currentUser;

        public AuthService()
        {
            _providerClient = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), AwsConfig.Region);
            _userPool = new CognitoUserPool(AwsConfig.UserPoolId, AwsConfig.UserPoolClientId, _providerClient);
        }

        public async Task<bool> SignUpAsync(string username, string password, string email)
        {
            try
            {
                var attrs = new Dictionary<string, string>
                {
                    { "email", email }
                };
                
                await _userPool.SignUpAsync(username, password, attrs, new Dictionary<string, string>());
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignUp Error: {ex.Message}");
                throw; // Rethrow to show message in ViewModel
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var user = new CognitoUser(username, AwsConfig.UserPoolClientId, _userPool, _providerClient);
                var authRequest = new InitiateSrpAuthRequest()
                {
                    Password = password
                };

                var authResponse = await user.StartWithSrpAuthAsync(authRequest);
                if (authResponse.AuthenticationResult != null)
                {
                    _currentUser = user;
                    // TODO: Securely store tokens if needed, but CognitoUser handles session usually
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
                throw;
            }
        }

        public async Task SignOutAsync()
        {
            if (_currentUser != null)
            {
                _currentUser.SignOut();
                _currentUser = null;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // Simple check: do we have a cached session or current user?
            // For a robust implementation, we'd check SecureStorage for a refresh token and try to refresh.
            // For this phase, we assume session is volatile or managed by CognitoUser memory for simplicity,
            // but normally we check if _currentUser.SessionTokensIsValid()
            
            return _currentUser != null; // Simplification
        }

        public string GetCurrentUserId()
        {
             return _currentUser?.Username;
        }
    }
}
