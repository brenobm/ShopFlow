using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    using AppResources = BrenoMachado.ShopFlow.Resources.Strings.AppResources;
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IPushService _pushService;
        private readonly IDialogService _dialogService;

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

        public LoginViewModel(IAuthService authService, IPushService pushService, IDialogService dialogService)
        {
            _authService = authService;
            _pushService = pushService;
            _dialogService = dialogService;
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
            FileLoggerService.LogStatic("[LoginViewModel] LoginAsync started.");

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await _dialogService.DisplayAlertAsync(AppResources.Error, AppResources.Msg_EnterCredentials, "OK");
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
                    Console.WriteLine("[LoginViewModel] Swapping to AppShell.");
                    Application.Current!.Windows[0].Page = new AppShell(_authService, _pushService);
                }
                else
                {
                    Console.WriteLine("[LoginViewModel] Login failed (success=false). Showing alert.");
                    await _dialogService.DisplayAlertAsync(AppResources.Error, AppResources.Msg_LoginFailed, "OK");
                }
            }
            catch (Amazon.CognitoIdentityProvider.Model.UserNotConfirmedException)
            {
                FileLoggerService.LogStatic("[LoginViewModel] UserNotConfirmedException caught.");
                await _dialogService.DisplayAlertAsync(AppResources.Btn_Verify, AppResources.Msg_AccountUnconfirmed, "OK");
                IsVerification = true;
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[LoginViewModel] Exception caught: {ex.Message}");
                await _dialogService.DisplayAlertAsync(AppResources.Error, ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                FileLoggerService.LogStatic("[LoginViewModel] IsBusy set to false.");
            }
        }

        async Task SignUpAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email))
            {
                await _dialogService.DisplayAlertAsync(AppResources.Error, AppResources.Msg_FieldsRequired, "OK");
                IsBusy = false;
                return;
            }

            try
            {
                bool success = await _authService.SignUpAsync(Username, Password, Email);
                if (success)
                {

                    await _dialogService.DisplayAlertAsync(AppResources.Success, AppResources.Msg_AccountCreated, "OK");
                    IsVerification = true; 
                    IsSignUp = false;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync(AppResources.Error, ex.Message, "OK");
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
                await _dialogService.DisplayAlertAsync(AppResources.Error, AppResources.Msg_UsernameCodeRequired, "OK");
                IsBusy = false;
                return;
            }

            try
            {
                bool success = await _authService.ConfirmSignUpAsync(Username, VerificationCode);
                if (success)
                {
                    await _dialogService.DisplayAlertAsync(AppResources.Success, AppResources.Msg_AccountVerified, "OK");
                    IsVerification = false;
                    IsSignUp = false; // Go to Login
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync(AppResources.Error, ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
