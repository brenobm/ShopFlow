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

        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand ToggleModeCommand { get; }

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
        public bool IsLogin => !IsSignUp;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Login";
            LoginCommand = new Command(async () => await LoginAsync());
            SignUpCommand = new Command(async () => await SignUpAsync());
            ToggleModeCommand = new Command(() => IsSignUp = !IsSignUp);
        }

        async Task LoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please enter username and password", "OK");
                IsBusy = false;
                return;
            }

            try
            {
                bool success = await _authService.LoginAsync(Username, Password);
                if (success)
                {
                    // Navigate to Main App
                    // Navigate to Main App (Swap MainPage)
                    Application.Current.MainPage = new AppShell(_authService);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Login failed. Check credentials.", "OK");
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
                    await Application.Current.MainPage.DisplayAlert("Success", "Account created! Please check your email for verification code (not implemented in UI yet) or just Login if confirmed.", "OK");
                    IsSignUp = false; // Switch to login
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
