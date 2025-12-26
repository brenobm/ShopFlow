using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; } // For signup
        public string VerificationCode { get; set; } // For confirmation

        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand VerifyCommand { get; }

        bool _isSignUp;
        public bool IsSignUp
        {
            get => _isSignUp;
            set
            {
                _isSignUp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLogin));
                Title = _isSignUp ? "Sign Up" : "Login";
            }
        }


        bool _isVerification;
        public bool IsVerification
        {
            get => _isVerification;
            set
            {
                _isVerification = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLogin));
                OnPropertyChanged(nameof(IsSignUp));
                if (_isVerification) Title = "Verify Account";
            }
        }

        public bool IsLogin => !IsSignUp && !IsVerification;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Login";
            LoginCommand = new Command(async () => await LoginAsync());
            SignUpCommand = new Command(async () => await SignUpAsync());

            VerifyCommand = new Command(async () => await VerifyAsync());
            ToggleModeCommand = new Command(() => 
            {
                IsSignUp = !IsSignUp;
                IsVerification = false; // Reset verification when toggling manual mode
            });
        }

        async Task LoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            Console.WriteLine("[LoginViewModel] LoginAsync started.");

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter username and password", "OK");
                IsBusy = false;
                return;
            }

            try
            {
                Console.WriteLine($"[LoginViewModel] Attempting login for user: {Username}");
                bool success = await _authService.LoginAsync(Username, Password);
                Console.WriteLine($"[LoginViewModel] Service returned success: {success}");

                if (success)
                {
                    // Navigate to Main App (Swap MainPage)
                    Console.WriteLine("[LoginViewModel] Swapping MainPage to AppShell.");
                    Application.Current.MainPage = new AppShell(_authService);
                }
                else
                {
                    Console.WriteLine("[LoginViewModel] Login failed (success=false). Showing alert.");
                    await Application.Current.MainPage.DisplayAlert("Error", "Login failed. Check credentials.", "OK");
                }
            }
            catch (Amazon.CognitoIdentityProvider.Model.UserNotConfirmedException)
            {
                Console.WriteLine("[LoginViewModel] UserNotConfirmedException caught.");
                await Application.Current.MainPage.DisplayAlert("Verify", "Account exists but is not confirmed. Please check your email for the code.", "OK");
                IsVerification = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoginViewModel] Exception caught: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                Console.WriteLine("[LoginViewModel] IsBusy set to false.");
            }
        }

        async Task SignUpAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "All fields are required", "OK");
                IsBusy = false;
                return;
            }

            try
            {
                bool success = await _authService.SignUpAsync(Username, Password, Email);
                if (success)
                {

                    await Application.Current.MainPage.DisplayAlert("Success", "Account created! Please check your email for the verification code.", "OK");
                    IsVerification = true; 
                    IsSignUp = false;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        async Task VerifyAsync()
        {
             if (IsBusy) return;
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(VerificationCode))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Username and Code are required", "OK");
                IsBusy = false;
                return;
            }

            try
            {
                bool success = await _authService.ConfirmSignUpAsync(Username, VerificationCode);
                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "Account verified! You can now login.", "OK");
                    IsVerification = false;
                    IsSignUp = false; // Go to Login
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
