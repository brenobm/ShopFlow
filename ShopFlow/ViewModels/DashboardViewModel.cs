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
                    
                    // Differential Update to prevent CollectionView Crash on iOS (Avoid Clear())
                    var ordered = source.OrderByDescending(l => l.IsDefault.GetValueOrDefault()).ThenBy(l => l.Name).ToList();
                    
                    // 1. Remove items not in source
                    for (int i = Lists.Count - 1; i >= 0; i--)
                    {
                        var item = Lists[i];
                        if (!ordered.Any(l => l.Id == item.Id))
                        {
                            Lists.RemoveAt(i);
                        }
                    }

                    // 2. Add or Update items
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        var sourceItem = ordered[i];
                        
                        // Calculate Visuals
                        string initials = "SF";
                        if (!string.IsNullOrEmpty(sourceItem.Name))
                        {
                            var parts = sourceItem.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                                initials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
                            else if (parts.Length == 1)
                                initials = $"{parts[0][0]}".ToUpper();
                        }
                        sourceItem.Initials = initials;

                        // Color Logic (Cycle based on index)
                        int cycle = i % 4;
                        switch (cycle)
                        {
                            case 0: // Green
                                sourceItem.PrimaryColor = Color.FromArgb("#34D399");
                                sourceItem.SecondaryColor = Color.FromArgb("#059669");
                                sourceItem.TotalItemsCount = 32; sourceItem.BoughtItemsCount = 17; // Mock
                                break;
                            case 1: // Orange
                                sourceItem.PrimaryColor = Color.FromArgb("#FBBF24");
                                sourceItem.SecondaryColor = Color.FromArgb("#D97706");
                                sourceItem.TotalItemsCount = 10; sourceItem.BoughtItemsCount = 3; // Mock
                                break;
                            case 2: // Blue
                                sourceItem.PrimaryColor = Color.FromArgb("#60A5FA");
                                sourceItem.SecondaryColor = Color.FromArgb("#2563EB");
                                sourceItem.TotalItemsCount = 24; sourceItem.BoughtItemsCount = 8; // Mock
                                break;
                            case 3: // Pink
                                sourceItem.PrimaryColor = Color.FromArgb("#F472B6");
                                sourceItem.SecondaryColor = Color.FromArgb("#DB2777");
                                sourceItem.TotalItemsCount = 5; sourceItem.BoughtItemsCount = 5; // Mock
                                break;
                        }

                        // Sync with ObservableCollection
                        var existing = Lists.FirstOrDefault(l => l.Id == sourceItem.Id);
                        if (existing != null)
                        {
                            // Update properties if changed
                            if (existing.Name != sourceItem.Name) existing.Name = sourceItem.Name;
                            if (existing.IsDefault != sourceItem.IsDefault) existing.IsDefault = sourceItem.IsDefault;
                            
                            // Visuals (Always update to ensure consistency with cycle)
                            existing.Initials = sourceItem.Initials;
                            existing.PrimaryColor = sourceItem.PrimaryColor;
                            existing.SecondaryColor = sourceItem.SecondaryColor;
                            existing.TotalItemsCount = sourceItem.TotalItemsCount;
                            existing.BoughtItemsCount = sourceItem.BoughtItemsCount;
                            
                            // Move if order is wrong
                            int oldIndex = Lists.IndexOf(existing);
                            if (oldIndex != i)
                            {
                                Lists.Move(oldIndex, i);
                            }
                        }
                        else
                        {
                            // Insert at correct position
                            if (i < Lists.Count)
                                Lists.Insert(i, sourceItem);
                            else
                                Lists.Add(sourceItem);
                        }
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
