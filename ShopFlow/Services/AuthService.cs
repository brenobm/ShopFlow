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
        private string _idToken; // Cache the ID token

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



        public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            try
            {
                var request = new ConfirmSignUpRequest
                {
                    ClientId = AwsConfig.UserPoolClientId,
                    Username = username,
                    ConfirmationCode = confirmationCode
                };

                await _providerClient.ConfirmSignUpAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Confirmation Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            Console.WriteLine($"[AuthService] LoginAsync called for user: {username}");
            try
            {
                var user = new CognitoUser(username, AwsConfig.UserPoolClientId, _userPool, _providerClient);
                var authRequest = new InitiateSrpAuthRequest()
                {
                    Password = password
                };

                Console.WriteLine("[AuthService] Starting SRP Auth...");
                var authResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);
                Console.WriteLine($"[AuthService] SRP Auth response received. Result: {authResponse?.AuthenticationResult != null}");

                if (authResponse.AuthenticationResult != null)
                {
                    _currentUser = user;
                    // Store the ID token for use in Identity Pool (SyncService)
                    _idToken = authResponse.AuthenticationResult.IdToken;
                    Console.WriteLine("[AuthService] Login successful.");
                    return true;
                }
                Console.WriteLine("[AuthService] Login failed: No AuthenticationResult.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Login Error: {ex.Message}");
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

        public string GetIdToken()
        {
            return _idToken;
        }
    }
}
