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
        
        public enum SortOption { Category, Name, DateAdded, Status }
        SortOption _currentSort = SortOption.Category;
        public SortOption CurrentSort
        {
            get => _currentSort;
            set
            {
                _currentSort = value;
                OnPropertyChanged();
                _ = LoadItemsAsync(); // Trigger re-sort
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
        public ICommand SortCommand { get; }

        public ICommand ListenCommand { get; }
        public ICommand ToggleShoppingModeCommand { get; }
        public ICommand ScanCommand { get; }
        public ICommand ShareCommand { get; }
        public ICommand ReadListCommand { get; }
        
        private readonly ISmartService _smartService;
        private readonly IVoiceService _voiceService;
        private readonly IProductService _productService;
        private readonly ITranslationService _translationService;
        private readonly IDatabaseService _databaseService;

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
        
        // ... (constructor)

        public ActiveListViewModel(IGraphQLService graphQLService, IVoiceService voiceService, ISmartService smartService, IProductService productService, ITranslationService translationService, IDatabaseService databaseService)
        {
            _graphQLService = graphQLService;
            _smartService = smartService;
            _voiceService = voiceService;
            _productService = productService;
            _translationService = translationService;
            _databaseService = databaseService;
            
            ListenCommand = new Command(async () => await ListenAsync(voiceService));            
            // ... (rest of constructor commands)
            AddItemCommand = new Command(async () => await AddItemAsync());
            TogglePurchasedCommand = new Command<ShoppingItem>(async (item) => await TogglePurchasedAsync(item));
            EditItemCommand = new Command<ShoppingItem>(async (item) => await EditItemAsync(item));
            DeleteItemCommand = new Command<ShoppingItem>(async (item) => await DeleteItemAsync(item));
            AddSuggestionCommand = new Command<string>(async (name) => await AddSuggestionAsync(name));
            SortCommand = new Command(async () => await ShowSortOptionsAsync());
            ToggleShoppingModeCommand = new Command(() => IsShoppingMode = !IsShoppingMode);
            ScanCommand = new Command(async () => await ScanAsync());
            ShareCommand = new Command(async () => await ShareListAsync());
            ReadListCommand = new Command(async () => await ReadListAsync());
        }

        // ... 

        async void LoadListDetailsAsync(string id)
        {
            try
            {
                var lists = await _graphQLService.GetListsAsync();
                CurrentList = lists.FirstOrDefault(l => l.Id == id) ?? new ShoppingList { Id = id, Name = "Shopping List" };
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading list: {ex.Message}");
            }
        }

        void ToggleScreenState(bool keepOn)
        {
            DeviceDisplay.KeepScreenOn = keepOn;
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
                 var items = await _graphQLService.GetItemsAsync(CurrentList.Id); // Returns IEnumerable<ShoppingItem>
                 
                 // Filter out bought items if in Shopping Mode
                 if (IsShoppingMode)
                 {
                     items = items.Where(i => !i.IsPurchased).ToList();
                 }

                 // PRE-SORT ITEMS BEFORE GROUPING
                 IEnumerable<ShoppingItem> sortedItems = items;
                 switch(CurrentSort)
                 {
                    case SortOption.Name: 
                        sortedItems = items.OrderBy(i => i.Name); 
                        break;
                    case SortOption.DateAdded: 
                        sortedItems = items.OrderByDescending(i => i.CreatedAt); 
                        break;
                    case SortOption.Status: 
                        sortedItems = items.OrderBy(i => i.IsPurchased).ThenBy(i => i.Name); 
                        break;
                    default: 
                        // Default/Category: Sort by Name inside category usually
                        sortedItems = items.OrderBy(i => i.Name); 
                        break; 
                 }
                 
                 var groups = sortedItems.GroupBy(i => i.Category ?? "Uncategorized")
                                   .Select(g => new ShoppingItemGroup(g.Key, g))
                                   .ToList();

                   // Sort Groups based on SortOption
                 if (CurrentSort == SortOption.Category)
                 {
                     groups = groups.OrderBy(g => ShoppingItem.DefaultCategories.IndexOf(g.Name) >= 0 ? ShoppingItem.DefaultCategories.IndexOf(g.Name) : 999).ToList();
                 }
                 else
                 {
                    // If sorting by Name/Date, we might want to keep Category order OR sort groups alphabetically?
                    // Let's stick to Category Order for the groups themselves to keep UI structure stable
                    groups = groups.OrderBy(g => ShoppingItem.DefaultCategories.IndexOf(g.Name) >= 0 ? ShoppingItem.DefaultCategories.IndexOf(g.Name) : 999).ToList();
                 }
                 
                 MainThread.BeginInvokeOnMainThread(async () => {
                     Items.Clear();
                     foreach(var group in groups)
                     {
                         Items.Add(group);
                     }
                     await LoadSuggestionsAsync();
                 });
             } 
             catch(Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
             }
        }

        List<ShoppingItemGroup> SortGroups(List<ShoppingItemGroup> groups)
        {
            // 1. Sort Items WITHIN Groups
            foreach(var group in groups)
            {
                // We need to sort the internal list of the group. 
                // Since ShoppingItemGroup inherits from ObservableCollection/List, we might need to clear and re-add or sort a copy.
                // Assuming ShoppingItemGroup is : ObservableCollection<ShoppingItem> or List<ShoppingItem>
                // Changes here might be tricky if it's ObservableCollection. Ideally we sort 'items' BEFORE grouping.
            }
            // Retrying logic: Sort 'items' list BEFORE grouping is cleaner.
            return groups;
        }

        async Task ShowSortOptionsAsync()
        {
            string action = await Shell.Current.DisplayActionSheet("Sort By", "Cancel", null, "Category (Default)", "Name (A-Z)", "Date Added (Newest)", "Status (Unchecked First)");
            switch(action)
            {
                case "Category (Default)": CurrentSort = SortOption.Category; break;
                case "Name (A-Z)": CurrentSort = SortOption.Name; break;
                case "Date Added (Newest)": CurrentSort = SortOption.DateAdded; break;
                case "Status (Unchecked First)": CurrentSort = SortOption.Status; break;
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

            try {
                await _graphQLService.UpdateItemAsync(item);
                
                // Log purchase if item is now purchased
                if (item.IsPurchased)
                {
                    await _databaseService.LogPurchaseAsync(item);
                }
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
                     
                     // FR-2.5: Voice Confirm
                     await Microsoft.Maui.Media.TextToSpeech.SpeakAsync($"Added {text}");
                     
                     await Shell.Current.DisplayAlert("Voice Input", $"Added '{text}'", "OK");
                }
            }
            catch (Exception ex)
            {
                 await Shell.Current.DisplayAlert("Error", $"Voice error: {ex.Message}", "OK");
            }
        }

        async Task ScanAsync()
        {
            try
            {
                // 1. Scan Barcode
                string barcode = await ScanPage.ScanAsync(Shell.Current.Navigation);
                if (string.IsNullOrWhiteSpace(barcode)) return;

                string? productName = null;
                string category = "Scanned";

                // 2. Lookup OpenFoodFacts
                var offProduct = await _productService.LookupProductAsync(barcode);
                if (offProduct != null && !string.IsNullOrWhiteSpace(offProduct.Name))
                {
                    productName = offProduct.Name;
                    category = offProduct.Category ?? category;
                }
                else
                {
                    // 3. Fallback: Lookup Global Product Table
                    var globalProduct = await _graphQLService.GetGlobalProductAsync(barcode);
                    if (globalProduct != null)
                    {
                        productName = globalProduct.Name;
                        category = globalProduct.Category ?? category;
                    }
                }

                // 4. Not Found anywhere? -> Register
                if (string.IsNullOrWhiteSpace(productName))
                {
                    bool register = await Shell.Current.DisplayAlert("Not Found", "Product not found. Do you want to register it?", "Yes", "No");
                    if (!register) return;

                    productName = await Shell.Current.DisplayPromptAsync("Register Product", "Enter product name:");
                    if (string.IsNullOrWhiteSpace(productName)) return;

                    // Save to Global Table for future (Fire and Forget or Await?)
                    // Await to ensure it works
                    try 
                    {
                        await _graphQLService.CreateGlobalProductAsync(barcode, productName, category);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to save global product: {ex.Message}");
                    }
                }
                
                // 5. Smart Matching (Common Flow)
                bool matchFound = await SmartMatchAndMarkAsync(productName);

                if (matchFound)
                {
                     await Shell.Current.DisplayAlert("Match!", $"Marked '{productName}' as bought.", "OK");
                }
                else
                {
                    // 6. Add if new
                    bool add = await Shell.Current.DisplayAlert("New Item", $"Add '{productName}' to list?", "Yes", "No");
                    if (add)
                    {
                        await _graphQLService.AddItemAsync(CurrentList.Id, productName, 1, "qty", category);
                        await LoadItemsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Scan error: {ex.Message}", "OK");
            }
        }

        async Task<bool> SmartMatchAndMarkAsync(string scannedName)
        {
            // Prepare Candidates: [Original, PT, EN]
            var candidates = new List<string> { scannedName };
            
            // Translate to PT-BR
            var pt = await _translationService.TranslateTextAsync(scannedName, "auto", "pt");
            if (!string.IsNullOrWhiteSpace(pt) && !candidates.Contains(pt)) candidates.Add(pt);
            
            // Translate to EN
            var en = await _translationService.TranslateTextAsync(scannedName, "auto", "en");
            if (!string.IsNullOrWhiteSpace(en) && !candidates.Contains(en)) candidates.Add(en);

            // Fetch latest items (just to be sure)
             var items = await _graphQLService.GetItemsAsync(CurrentList.Id);
             var unboughtItems = items.Where(i => !i.IsPurchased).ToList();

            foreach (var candidate in candidates)
            {
                 // Logic: Does the Candidate (e.g. "Neilson Milk") contain the List Item (e.g. "Milk")?
                 var match = unboughtItems.FirstOrDefault(i => candidate.Contains(i.Name, StringComparison.OrdinalIgnoreCase));
                 
                 if (match != null)
                 {
                     await TogglePurchasedAsync(match);
                     return true;
                 }
                 
                 // Reverse Logic: Does List Item (e.g. "Leite Integral") contain Candidate (e.g. "Leite")?
                 var matchReverse = unboughtItems.FirstOrDefault(i => i.Name.Contains(candidate, StringComparison.OrdinalIgnoreCase));
                  if (matchReverse != null)
                 {
                     await TogglePurchasedAsync(matchReverse);
                     return true;
                 }
            }

            return false;
        }
        async Task ShareListAsync()
        {
            if (CurrentList == null) return;
            string email = await Shell.Current.DisplayPromptAsync("Share List", $"Enter email to share '{CurrentList.Name}' with:");
            if (string.IsNullOrWhiteSpace(email)) return;

            try
            {
                await _graphQLService.ShareListAsync(CurrentList.Id, email.Trim());
                await Application.Current.MainPage.DisplayAlert("Success", $"Invited {email} to list.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to share list: {ex.Message}", "OK");
            }
        }

        public async Task AddItemFromSiriAsync(string itemName)
        {
             if (string.IsNullOrWhiteSpace(itemName)) return;
             
             // If CurrentList matches the "Default", great. But if we are just launching, we need to find the default.
             // If CurrentList is set (we were already open), usage depends on requirement. 
             // "when doing command voice, it will add to the default list." -> Implies ALWAYS default list?
             // Or if I'm inside "Project B" list, should "Siri add milk" add to Project B or Default?
             // Usually Siri "Add to ShopFlow" implies default unless specified.
             // But if the app is foregrounded in a specific list, it might be ambiguous.
             // Let's assume: If app is NOT in a list context (launched fresh), use Default.
             // If app IS in a list context, probably stick to it? 
             // The user said: "Siri, add milk... add to the default list."
             
             ShoppingList targetList = CurrentList;
             
             if (targetList == null)
             {
                 var lists = await _graphQLService.GetListsAsync();
                 targetList = lists.FirstOrDefault(l => l.IsDefault == true) ?? lists.FirstOrDefault();
                 if(targetList == null) return;
                 CurrentList = targetList; // Set it so the UI loads it
             }
             else 
             {
                 // Optional: Explicitly switch to default list?
                 // "when doing command voice, it will add to the default list."
                 // This sounds like a strict rule.
                 // Let's check if we should switch.
                 var lists = await _graphQLService.GetListsAsync();
                 var defaultList = lists.FirstOrDefault(l => l.IsDefault == true);
                 if (defaultList != null && defaultList.Id != CurrentList.Id)
                 {
                     targetList = defaultList;
                     CurrentList = targetList; // Switch UI to Default List
                 }
             }

             await _graphQLService.AddItemAsync(targetList.Id, itemName, 1, "qty", "Siri");
             await LoadItemsAsync();
             // Maybe show a toast
        }

        async Task ReadListAsync()
        {
             // Flat list of unpurchased items
             var itemsToRead = Items.SelectMany(g => g).Where(i => !i.IsPurchased).ToList();
             
             if (!itemsToRead.Any())
             {
                 await Microsoft.Maui.Media.TextToSpeech.SpeakAsync("Your list is empty.");
                 return;
             }
             
             var textBuilder = new System.Text.StringBuilder();
             textBuilder.AppendLine($"You have {itemsToRead.Count} items on your list.");
             
             foreach(var item in itemsToRead)
             {
                 // E.g. "Milk." or "2 Milk."
                 if (item.Quantity > 1) 
                     textBuilder.AppendLine($"{item.Quantity} {item.Name} .");
                 else
                     textBuilder.AppendLine($"{item.Name} .");
             }
             
             try 
             {
                await Microsoft.Maui.Media.TextToSpeech.SpeakAsync(textBuilder.ToString());
             }
             catch(Exception ex)
             {
                await Shell.Current.DisplayAlert("Read Error", ex.Message, "OK");
             }
        }
    }
}
