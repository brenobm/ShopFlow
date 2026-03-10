namespace BrenoMachado.ShopFlow.Services
{
    public class DialogService : IDialogService
    {
        public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            return Shell.Current.DisplayAlert(title, message, accept, cancel);
        }

        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return Shell.Current.DisplayAlert(title, message, cancel);
        }

        public Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, Keyboard keyboard = null, string initialValue = "")
        {
            return Shell.Current.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);
        }

        public Task<string> DisplayActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            return Shell.Current.DisplayActionSheet(title, cancel, destruction, buttons);
        }
    }
}
