using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    [QueryProperty(nameof(ListId), "Id")]
    public class ActiveListViewModel : BaseViewModel
    {
        private readonly IGraphQLService _graphQLService;
        
        string _listId;
        public string ListId
        {
            get => _listId;
            set
            {
                _listId = value;
                LoadListDetailsAsync(value); 
            }
        }
        
        ShoppingList _currentList;
        public ShoppingList CurrentList
        {
            get => _currentList;
            set
            {
                _currentList = value;
                OnPropertyChanged();
                if(_currentList != null) Title = _currentList.Name;
            }
        }

        public ObservableCollection<ShoppingItemGroup> Items { get; } = new();
        public ObservableCollection<string> Suggestions { get; } = new();

        public ICommand AddItemCommand { get; }
        public ICommand TogglePurchasedCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand AddSuggestionCommand { get; }

        public ICommand ListenCommand { get; }
        public ICommand ToggleShoppingModeCommand { get; }
        
        private readonly ISmartService _smartService;
        private readonly IVoiceService _voiceService;

        bool _isShoppingMode;
        public bool IsShoppingMode
        {
            get => _isShoppingMode;
            set
            {
                _isShoppingMode = value;
                OnPropertyChanged();
                ToggleScreenState(value);
                _ = LoadItemsAsync(); // Refresh list to apply filter
            }
        }

        public ActiveListViewModel(IGraphQLService graphQLService, IVoiceService voiceService, ISmartService smartService)
        {
            _graphQLService = graphQLService;
            _smartService = smartService;
            _voiceService = voiceService;
            
            ListenCommand = new Command(async () => await ListenAsync(voiceService));
            AddItemCommand = new Command(async () => await AddItemAsync());
            TogglePurchasedCommand = new Command<ShoppingItem>(async (item) => await TogglePurchasedAsync(item));
            EditItemCommand = new Command<ShoppingItem>(async (item) => await EditItemAsync(item));
            DeleteItemCommand = new Command<ShoppingItem>(async (item) => await DeleteItemAsync(item));
            AddSuggestionCommand = new Command<string>(async (name) => await AddSuggestionAsync(name));
            ToggleShoppingModeCommand = new Command(() => IsShoppingMode = !IsShoppingMode);
        }

        void ToggleScreenState(bool keepOn)
        {
            DeviceDisplay.KeepScreenOn = keepOn;
        }

        async void LoadListDetailsAsync(string id)
        {
            try
            {
                // We don't have GetListAsync(id) in Service, but we can assume the ID is valid or fetch lists.
                // For now, let's just set the ID and fetch items. The Name might be missing until we have GetList(id).
                // Or we can fetch all lists and find it.
                // Let's implement GetList(id) or just use what we have.
                // CurrentList = await _databaseService.GetListAsync(id);
                // Workaround: Set ID, try to fetch name or just Items.
                var lists = await _graphQLService.GetListsAsync();
                CurrentList = lists.FirstOrDefault(l => l.Id == id) ?? new ShoppingList { Id = id, Name = "Shopping List" };
                
                // LoadItemsAsync(); // Refresh() will be called by OnAppearing? No, manually call it?
                // Wait, property setter is void async.
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading list: {ex.Message}");
            }
        }

        public async Task RefreshAsync()
        {
            await LoadItemsAsync();
        }

        async Task LoadItemsAsync()
        {
             if (CurrentList == null) return;
             
             try 
             {
                 var items = await _graphQLService.GetItemsAsync(CurrentList.Id);
                 
                 // Filter out bought items if in Shopping Mode
                 if (IsShoppingMode)
                 {
                     items = items.Where(i => !i.IsPurchased).ToList();
                 }

                 Items.Clear();
                 
                 var groups = items.GroupBy(i => i.Category ?? "Uncategorized")
                                   .Select(g => new ShoppingItemGroup(g.Key, g))
                                   .OrderBy(g => ShoppingItem.DefaultCategories.IndexOf(g.Name) >= 0 ? ShoppingItem.DefaultCategories.IndexOf(g.Name) : 999);
    
                 foreach(var group in groups)
                 {
                     Items.Add(group);
                 }
                 await LoadSuggestionsAsync();
             } 
             catch(Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
             }
        }

        async Task LoadSuggestionsAsync()
        {
            try
            {
                var suggestions = await _smartService.GetSuggestionsAsync();
                Suggestions.Clear();
                var allItems = Items.SelectMany(g => g).ToList();
                foreach(var s in suggestions)
                {
                    if(!allItems.Any(i => i.Name.Equals(s, StringComparison.OrdinalIgnoreCase))) // Don't suggest if already in list
                    {
                        Suggestions.Add(s);
                    }
                }
            } catch(Exception) {}
        }

        async Task AddSuggestionAsync(string name)
        {
             if (string.IsNullOrWhiteSpace(name)) return;

             try {
                await _graphQLService.AddItemAsync(CurrentList.Id, name.Trim(), 1, "qty", "Smart Suggestion");
                await LoadItemsAsync(); 
             } catch(Exception ex)
             {
                  await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add suggestion: {ex.Message}", "OK");
             }
        }

        async Task AddItemAsync()
        {
             await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?ListId={CurrentList.Id}");
        }

        async Task TogglePurchasedAsync(ShoppingItem item)
        {
            if (item == null) return;
            // Optimistic update
            bool original = item.IsPurchased;
            item.IsPurchased = !item.IsPurchased;
            // item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null; // Backend might handle this? Or we pass it? 
            // UpdateItem mutation handles IsPurchased.

            try {
                await _graphQLService.UpdateItemAsync(item);
                // await LoadItemsAsync(); // Optional: Refresh to sort?
            } catch {
                item.IsPurchased = original; // Revert on error
            }
        }

        async Task EditItemAsync(ShoppingItem item)
        {
             if (item == null) return;
             await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?ItemId={item.Id}&ListId={CurrentList.Id}&Name={Uri.EscapeDataString(item.Name)}&Category={Uri.EscapeDataString(item.Category ?? "")}");
        }

        async Task DeleteItemAsync(ShoppingItem item)
        {
            if(item == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Delete", $"Remove {item.Name}?", "Yes", "No");
            if(confirm)
            {
                try {
                    await _graphQLService.DeleteItemAsync(item.ListId, item.Id);
                    await LoadItemsAsync();
                } catch(Exception ex) {
                     await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
                }
            }
        }
        async Task ListenAsync(IVoiceService voiceService)
        {
            try 
            {
                var text = await voiceService.ListenAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                     await _graphQLService.AddItemAsync(CurrentList.Id, text.Trim(), 1, "qty", "Voice Added");
                     await LoadItemsAsync();
                     await Shell.Current.DisplayAlert("Voice Input", $"Added '{text}'", "OK");
                }
            }
            catch (Exception ex)
            {
                 await Shell.Current.DisplayAlert("Error", $"Voice error: {ex.Message}", "OK");
            }
        }
    }
}
