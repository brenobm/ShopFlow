using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.SmartShopping.Models;
using BrenoMachado.SmartShopping.Services;
using BrenoMachado.SmartShopping.Views;

namespace BrenoMachado.SmartShopping.ViewModels
{
    [QueryProperty(nameof(ListId), "Id")]
    public class ActiveListViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        
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
        
        private readonly ISmartService _smartService;

        public ActiveListViewModel(IDatabaseService databaseService, IVoiceService voiceService, ISmartService smartService)
        {
            _databaseService = databaseService;
            _smartService = smartService;
            ListenCommand = new Command(async () => await ListenAsync(voiceService));
            AddItemCommand = new Command(async () => await AddItemAsync());
            TogglePurchasedCommand = new Command<ShoppingItem>(async (item) => await TogglePurchasedAsync(item));
            EditItemCommand = new Command<ShoppingItem>(async (item) => await EditItemAsync(item));
            DeleteItemCommand = new Command<ShoppingItem>(async (item) => await DeleteItemAsync(item));
            AddSuggestionCommand = new Command<string>(async (name) => await AddSuggestionAsync(name));
        }

        async void LoadListDetailsAsync(string id)
        {
            try
            {
                CurrentList = await _databaseService.GetListAsync(id);
                // LoadItemsAsync(); // Refresh() will be called by OnAppearing
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
             
             var items = await _databaseService.GetItemsAsync(CurrentList.Id);
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

        async Task LoadSuggestionsAsync()
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
        }

        async Task AddSuggestionAsync(string name)
        {
             if (string.IsNullOrWhiteSpace(name)) return;

             var newItem = new ShoppingItem 
             { 
                 Name = name.Trim(),
                 ListId = CurrentList.Id,
                 Category = "Smart Suggestion"
             };
             
             await _databaseService.SaveItemAsync(newItem);
             await LoadItemsAsync(); // Will reload suggestions too
        }

        async Task AddItemAsync()
        {
             await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?ListId={CurrentList.Id}");
        }

        async Task TogglePurchasedAsync(ShoppingItem item)
        {
            if (item == null) return;
            item.IsPurchased = !item.IsPurchased;
            item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null;
            
            await _databaseService.SaveItemAsync(item);
            await LoadItemsAsync(); // Refresh to sort
            
            // Voice feedback could go here
        }

        async Task EditItemAsync(ShoppingItem item)
        {
             if (item == null) return;
             await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?ItemId={item.Id}&ListId={CurrentList.Id}");
        }

        async Task DeleteItemAsync(ShoppingItem item)
        {
            if(item == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Delete", $"Remove {item.Name}?", "Yes", "No");
            if(confirm)
            {
                await _databaseService.DeleteItemAsync(item);
                await LoadItemsAsync();
            }
        }
        async Task ListenAsync(IVoiceService voiceService)
        {
            try 
            {
                var text = await voiceService.ListenAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Add item directly
                     var newItem = new ShoppingItem 
                     { 
                         Name = text.Trim(), // Capitalize first letter logic could go here
                         ListId = CurrentList.Id,
                         Category = "Voice Added"
                     };
                     
                     await _databaseService.SaveItemAsync(newItem);
                     await LoadItemsAsync();
                     
                     // Feedback
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
