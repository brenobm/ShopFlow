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
            var config = new AmazonCognitoIdentityProviderConfig
            {
                RegionEndpoint = AwsConfig.Region,
                Timeout = TimeSpan.FromSeconds(10),
                MaxErrorRetry = 3
            };
            _providerClient = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), config);
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
                FileLoggerService.LogStatic($"SignUp Error: {ex.Message}");
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
                FileLoggerService.LogStatic($"Confirmation Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            FileLoggerService.LogStatic($"[AuthService] LoginAsync called for user: {username}");
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
                    _idToken = authResponse.AuthenticationResult.IdToken;
                    
                    // Identify if RefreshToken is consistent (it might be null on subsequent auths if not requested, but usually returns on SRP)
                    if (!string.IsNullOrEmpty(authResponse.AuthenticationResult.RefreshToken))
                    {
                        await SecureStorage.SetAsync("auth_refresh_token", authResponse.AuthenticationResult.RefreshToken);
                    }
                    await SecureStorage.SetAsync("auth_username", username);

                    Console.WriteLine("[AuthService] Login successful.");
                    return true;
                }
                Console.WriteLine("[AuthService] Login failed: No AuthenticationResult.");
                return false;
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[AuthService] Login Error: {ex.Message}");
                FileLoggerService.LogStatic($"Login Error: {ex.Message}");
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
            SecureStorage.Remove("auth_refresh_token");
            SecureStorage.Remove("auth_username");
            await Task.CompletedTask;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_currentUser != null) return true;

            try 
            {
                FileLoggerService.LogStatic("[AuthService] IsAuthenticatedAsync: Accessing SecureStorage...");
                var refreshToken = await SecureStorage.GetAsync("auth_refresh_token").ConfigureAwait(false);
                var username = await SecureStorage.GetAsync("auth_username").ConfigureAwait(false);
                FileLoggerService.LogStatic($"[AuthService] IsAuthenticatedAsync: Token found: {!string.IsNullOrEmpty(refreshToken)}, User: {username}");

                if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(username))
                {
                     var user = new CognitoUser(username, AwsConfig.UserPoolClientId, _userPool, _providerClient);
                     
                     // Manually set the session tokens with the refresh token
                     user.SessionTokens = new CognitoUserSession(null, null, refreshToken, DateTime.Now, DateTime.Now.AddHours(1));

                     // Attempt to refresh
                     FileLoggerService.LogStatic("[AuthService] Starting Refresh Token Auth...");
                     var authRequest = new InitiateRefreshTokenAuthRequest { AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH };
                     var authResponse = await user.StartWithRefreshTokenAuthAsync(authRequest).ConfigureAwait(false);
                     FileLoggerService.LogStatic("[AuthService] Refresh Token Auth completed.");
                     
                     if (authResponse.AuthenticationResult != null)
                     {
                         _currentUser = user;
                         _idToken = authResponse.AuthenticationResult.IdToken;
                         
                         // Update Refresh Token if a new one is returned (Refresh Token Rotation)
                         if (!string.IsNullOrEmpty(authResponse.AuthenticationResult.RefreshToken))
                         {
                             await SecureStorage.SetAsync("auth_refresh_token", authResponse.AuthenticationResult.RefreshToken);
                         }
                         
                         return true;
                     }
                }
            } 
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[AuthService] Auto-login failed: {ex.Message}");
                
                // CRITICAL FIX: If offline, do NOT clear credentials. Assume valid.
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    FileLoggerService.LogStatic("[AuthService] Offline mode detected. Trusting local credentials.");
                    
                    // Reconstruct user object from local storage so the app works
                    var username = await SecureStorage.GetAsync("auth_username");
                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = new CognitoUser(username, AwsConfig.UserPoolClientId, _userPool, _providerClient);
                        _currentUser = user;
                        return true; 
                    }
                }

                // If online and failed, then it's a real auth error. Clear state.
                SecureStorage.Remove("auth_refresh_token");
                SecureStorage.Remove("auth_username");
            }

            return false; 
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
