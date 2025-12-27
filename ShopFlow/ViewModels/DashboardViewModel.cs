using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IGraphQLService _graphQLService;

        public ObservableCollection<ShoppingList> Lists { get; } = new();

        public ICommand LoadListsCommand { get; }
        public ICommand AddListCommand { get; }
        public ICommand OpenListCommand { get; }
        public ICommand EditListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand ShareListCommand { get; }
        public ICommand GoToSettingsCommand { get; }

        public DashboardViewModel(IGraphQLService graphQLService)
        {
            Title = "My Lists (Cloud)";
            _graphQLService = graphQLService;

            LoadListsCommand = new Command(async () => await LoadListsAsync());
            AddListCommand = new Command(async () => await AddListAsync());
            OpenListCommand = new Command<ShoppingList>(async (list) => await OpenListAsync(list));
             OpenListCommand = new Command<ShoppingList>(async (list) => await OpenListAsync(list));
            EditListCommand = new Command<ShoppingList>(async (list) => await EditListAsync(list));
            DeleteListCommand = new Command<ShoppingList>(async (list) => await DeleteListAsync(list));
            ShareListCommand = new Command<ShoppingList>(async (list) => await ShareListAsync(list));
            GoToSettingsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(Views.SettingsPage)));
        }

        async Task LoadListsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                // Fetch from Cloud
                var lists = await _graphQLService.GetListsAsync();
                Lists.Clear();
                foreach (var list in lists)
                {
                    Lists.Add(list);
                }
            }
            catch (Exception ex)
            {
                 await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load lists: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        async Task AddListAsync()
        {
             string name = await Shell.Current.DisplayPromptAsync("New List", "Enter list name");
             if (string.IsNullOrWhiteSpace(name))
                 return;

             try 
             {
                 var newList = await _graphQLService.CreateListAsync(name.Trim());
                 await LoadListsAsync();
             }
             catch (Exception ex)
             {
                 await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create list: {ex.Message}", "OK");
             }
        }

        async Task EditListAsync(ShoppingList list)
        {
            if (list == null) return;
            string name = await Shell.Current.DisplayPromptAsync("Edit List", "Enter new list name", initialValue: list.Name);
            if (string.IsNullOrWhiteSpace(name) || name == list.Name) return;

            try
            {
                await _graphQLService.UpdateListAsync(list.Id, name.Trim());
                await LoadListsAsync();
            }
            catch (Exception ex)
            {
                 await Application.Current.MainPage.DisplayAlert("Error", $"Failed to update list: {ex.Message}", "OK");
            }
        }

        async Task DeleteListAsync(ShoppingList list)
        {
            if (list == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Delete", $"Delete list '{list.Name}'?", "Yes", "No");
            if (confirm)
            {
                try
                {
                    await _graphQLService.DeleteListAsync(list.Id);
                    await LoadListsAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete list: {ex.Message}", "OK");
                }
            }
        }

        async Task OpenListAsync(ShoppingList list)
        {
            if (list == null) return;
            
            // Navigate to ActiveListPage passing the list ID
            await Shell.Current.GoToAsync($"{nameof(ActiveListPage)}?Id={list.Id}");
        }
        
        async Task ShareListAsync(ShoppingList list)
        {
            if (list == null) return;
            string email = await Shell.Current.DisplayPromptAsync("Share List", $"Enter email to share '{list.Name}' with:");
            if (string.IsNullOrWhiteSpace(email)) return;

            try
            {
                await _graphQLService.ShareListAsync(list.Id, email.Trim());
                await Application.Current.MainPage.DisplayAlert("Success", $"Invited {email} to list.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to share list: {ex.Message}", "OK");
            }
        }
        
        public void OnAppearing()
        {
            IsBusy = false;
            LoadListsCommand.Execute(null);
        }
    }
}
