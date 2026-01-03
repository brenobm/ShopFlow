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
        private readonly IDatabaseService _databaseService;

        public ObservableCollection<ShoppingList> Lists { get; } = new();

        public ICommand LoadListsCommand { get; }
        public ICommand AddListCommand { get; }
        public ICommand OpenListCommand { get; }
        public ICommand EditListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand ShareListCommand { get; }
        public ICommand GoToSettingsCommand { get; }
        public ICommand GoToLogsCommand { get; } // New Command
        public ICommand SetDefaultListCommand { get; }

        public DashboardViewModel(IGraphQLService graphQLService, IDatabaseService databaseService)
        {
            Title = "My Lists";
            _graphQLService = graphQLService;
            _databaseService = databaseService;

            LoadListsCommand = new Command(async () => await LoadListsAsync());
            AddListCommand = new Command(async () => await AddListAsync());
            OpenListCommand = new Command<ShoppingList>(async (list) => await OpenListAsync(list));
            EditListCommand = new Command<ShoppingList>(async (list) => await EditListAsync(list));
            DeleteListCommand = new Command<ShoppingList>(async (list) => await DeleteListAsync(list));
            ShareListCommand = new Command<ShoppingList>(async (list) => await ShareListAsync(list));
            GoToSettingsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(Views.SettingsPage)));
            GoToLogsCommand = new Command(async () => await Shell.Current.Navigation.PushModalAsync(new LogsPage(new LogsViewModel(Services.FileLoggerService.Instance))));
            SetDefaultListCommand = new Command<ShoppingList>(async (list) => await SetDefaultListAsync(list));
        }

        async Task LoadListsAsync()
        {
            FileLoggerService.LogStatic("[DashboardVM] LoadListsAsync: Started.");
            // Delay to allow navigation/UI transitions to complete (Fixes iOS Back Navigation Crash)
            await Task.Delay(300);

            if (IsBusy) 
            {
                FileLoggerService.LogStatic("[DashboardVM] LoadListsAsync: Skipped (Busy).");
                return;
            }

            try
            {
                IsBusy = true;
                
                // 1. Load Local First (Always works offline)
                FileLoggerService.LogStatic("[DashboardVM] LoadListsAsync: Fetching from DB...");
                var localLists = await _databaseService.GetListsAsync();
                FileLoggerService.LogStatic($"[DashboardVM] LoadListsAsync: Found {localLists.Count} lists locally.");
                UpdateListsCollection(localLists);

                // 2. If Online, Sync with Cloud
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    // ... (sync logic)
                }
            }
            catch (Exception ex)
            {
                 FileLoggerService.LogStatic($"[DashboardVM] LoadListsAsync Error: {ex}");
                 await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load lists: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                FileLoggerService.LogStatic("[DashboardVM] LoadListsAsync: Finished.");
            }
        }

        private void UpdateListsCollection(List<ShoppingList> source)
        {
            FileLoggerService.LogStatic("[DashboardVM] UpdateListsCollection: Queueing on MainThread...");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    FileLoggerService.LogStatic("[DashboardVM] UpdateListsCollection: Executing on MainThread.");
                    if (source == null) return;
                    
                    Lists.Clear(); // <-- SUSPECT: Clearing CollectionView source on iOS can crash if during animation
                    int index = 0;

                    var ordered = source.OrderByDescending(l => l.IsDefault.GetValueOrDefault()).ThenBy(l => l.Name);
                    
                    foreach (var list in ordered)
                    {
                        // Initials Logic
                        string initials = "SF";
                        if (!string.IsNullOrEmpty(list.Name))
                        {
                            var parts = list.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                                initials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
                            else if (parts.Length == 1)
                                initials = $"{parts[0][0]}".ToUpper();
                        }
                        list.Initials = initials;

                        // Color Logic
                        int cycle = index % 4;
                        switch (cycle)
                        {
                            case 0: // Green
                                list.PrimaryColor = Color.FromArgb("#34D399");
                                list.SecondaryColor = Color.FromArgb("#059669");
                                list.TotalItemsCount = 32; list.BoughtItemsCount = 17; // Mock
                                break;
                            case 1: // Orange
                                list.PrimaryColor = Color.FromArgb("#FBBF24");
                                list.SecondaryColor = Color.FromArgb("#D97706");
                                list.TotalItemsCount = 10; list.BoughtItemsCount = 3; // Mock
                                break;
                            case 2: // Blue
                                list.PrimaryColor = Color.FromArgb("#60A5FA");
                                list.SecondaryColor = Color.FromArgb("#2563EB");
                                list.TotalItemsCount = 24; list.BoughtItemsCount = 8; // Mock
                                break;
                            case 3: // Pink
                                list.PrimaryColor = Color.FromArgb("#F472B6");
                                list.SecondaryColor = Color.FromArgb("#DB2777");
                                list.TotalItemsCount = 5; list.BoughtItemsCount = 5; // Mock
                                break;
                        }

                        Lists.Add(list);
                        index++;
                    }
                }
                catch (Exception ex)
                {
                    FileLoggerService.LogStatic($"[DashboardViewModel] UI Update Failed: {ex}");
                }
            });
        }
        
        async Task AddListAsync()
        {
             string name = await Shell.Current.DisplayPromptAsync("New List", "Enter list name");
             if (string.IsNullOrWhiteSpace(name))
                 return;

             try 
             {
                 bool isFirst = !Lists.Any();
                 var newList = new ShoppingList 
                 { 
                     Name = name.Trim(), 
                     IsDefault = isFirst,
                     // If offline, mark dirty (though we don't have full sync yet, this marks intent)
                     IsDirty = Connectivity.NetworkAccess != NetworkAccess.Internet 
                 };

                 if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                 {
                     try 
                     {
                         // Create Cloud
                         var cloudList = await _graphQLService.CreateListAsync(name.Trim(), isFirst);
                         // Use Cloud properties (ID, dates)
                         newList.Id = cloudList.Id;
                         newList.CreatedAt = cloudList.CreatedAt;
                     }
                     catch (Exception cloudEx)
                     {
                         FileLoggerService.LogStatic($"Cloud Create Failed: {cloudEx.Message}");
                         // Fallback to offline creation if cloud fails
                         newList.IsDirty = true;
                     }
                 }
                 else
                 {
                      newList.IsDirty = true;
                 }

                 // Save Local (Critical for Offline)
                 await _databaseService.SaveListAsync(newList);
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
                list.Name = name.Trim();
                list.IsDirty = true;

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        await _graphQLService.UpdateListAsync(list.Id, list.Name, list.IsDefault.GetValueOrDefault());
                        list.IsDirty = false;
                    }
                    catch
                    {
                        // Keep dirty if cloud fails
                    }
                }

                await _databaseService.SaveListAsync(list);
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
                    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                         try { await _graphQLService.DeleteListAsync(list.Id); } catch {}
                    }
                    
                    // Logic: If verify default was deleted, promote another
                    if (list.IsDefault == true)
                    {
                        var remaining = Lists.Where(l => l.Id != list.Id).ToList();
                        if (remaining.Any())
                        {
                            await SetDefaultListAsync(remaining.First());
                        }
                    }
                    
                    // Delete Local
                    await _databaseService.DeleteListAsync(list);
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
            if (newDefault.IsDefault == true) return; // Already default

            try
            {
                // 1. Unset old default(s) in Local
                var oldDefaults = Lists.Where(l => l.IsDefault == true && l.Id != newDefault.Id).ToList();
                foreach (var old in oldDefaults)
                {
                    old.IsDefault = false;
                    old.IsDirty = true;
                    // Attempt Cloud Update (Best Effort)
                    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    {
                        try { await _graphQLService.UpdateListAsync(old.Id, old.Name, false); old.IsDirty = false; } catch {}
                    }
                    await _databaseService.SaveListAsync(old);
                }

                // 2. Set new default in Local
                newDefault.IsDefault = true;
                newDefault.IsDirty = true;
                
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                     try { await _graphQLService.UpdateListAsync(newDefault.Id, newDefault.Name, true); newDefault.IsDirty = false; } catch {}
                }
                
                await _databaseService.SaveListAsync(newDefault);

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
