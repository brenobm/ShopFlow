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
        public ICommand SetDefaultListCommand { get; }

        public DashboardViewModel(IGraphQLService graphQLService)
        {
            Title = "My Lists (Cloud)";
            _graphQLService = graphQLService;

            LoadListsCommand = new Command(async () => await LoadListsAsync());
            AddListCommand = new Command(async () => await AddListAsync());
            OpenListCommand = new Command<ShoppingList>(async (list) => await OpenListAsync(list));
            EditListCommand = new Command<ShoppingList>(async (list) => await EditListAsync(list));
            DeleteListCommand = new Command<ShoppingList>(async (list) => await DeleteListAsync(list));
            ShareListCommand = new Command<ShoppingList>(async (list) => await ShareListAsync(list));
            GoToSettingsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(Views.SettingsPage)));
            SetDefaultListCommand = new Command<ShoppingList>(async (list) => await SetDefaultListAsync(list));
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
                foreach (var list in lists.OrderByDescending(l => l.IsDefault).ThenBy(l => l.Name))
                {
                    Lists.Add(list);
                }
                
                // Consistency Check: Ensure exactly one default exists if lists exist
                if (Lists.Any() && !Lists.Any(l => l.IsDefault))
                {
                    await SetDefaultListAsync(Lists.First());
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
                 bool isFirst = !Lists.Any();
                 var newList = await _graphQLService.CreateListAsync(name.Trim(), isFirst);
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
                await _graphQLService.UpdateListAsync(list.Id, name.Trim(), list.IsDefault);
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
                    
                    // Logic: If verify default was deleted, promote another
                    if (list.IsDefault)
                    {
                        var remaining = Lists.Where(l => l.Id != list.Id).ToList();
                        if (remaining.Any())
                        {
                            await SetDefaultListAsync(remaining.First());
                        }
                    }
                    
                    await LoadListsAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete list: {ex.Message}", "OK");
                }
            }
        }

        async Task SetDefaultListAsync(ShoppingList newDefault)
        {
            if (newDefault == null) return;
            if (newDefault.IsDefault) return; // Already default

            try
            {
                // 1. Unset old default(s)
                var oldDefaults = Lists.Where(l => l.IsDefault && l.Id != newDefault.Id).ToList();
                foreach (var old in oldDefaults)
                {
                    old.IsDefault = false;
                    // Fire and forget update to avoid blocking UI too much, or await?
                    // Better await to ensure consistency
                    await _graphQLService.UpdateListAsync(old.Id, old.Name, false);
                }

                // 2. Set new default
                newDefault.IsDefault = true;
                await _graphQLService.UpdateListAsync(newDefault.Id, newDefault.Name, true);

                await LoadListsAsync();
            }
            catch (Exception ex)
            {
                 await Application.Current.MainPage.DisplayAlert("Error", $"Failed to set default: {ex.Message}", "OK");
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
