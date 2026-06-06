using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;
using BrenoMachado.ShopFlow.Repositories;
using CommunityToolkit.Maui.Views;
using BrenoMachado.ShopFlow.Views.Popups;
using CommunityToolkit.Mvvm.Messaging;
using BrenoMachado.ShopFlow.Messages;

namespace BrenoMachado.ShopFlow.ViewModels
{
    using AppResources = BrenoMachado.ShopFlow.Resources.Strings.AppResources;
    [QueryProperty(nameof(ListId), "Id")]
    public class ActiveListViewModel : BaseViewModel, IQueryAttributable
    {
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("action") && query["action"].ToString() == "add")
            {
                 if (query.ContainsKey("item"))
                 {
                     string itemToAdd = query["item"].ToString();
                     // Use fire-and-forget safely on main thread
                     MainThread.BeginInvokeOnMainThread(async () => await AddItemFromSiriAsync(itemToAdd));
                 }
            }
        }
        private readonly IShoppingRepository _shoppingRepository;
        
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


        public ICommand ToggleShoppingModeCommand { get; }
        public ICommand ScanCommand { get; }
        public ICommand ShareCommand { get; }
        public ICommand ReadListCommand { get; }
        public ICommand OpenQuickTuneCommand { get; }
        
        private readonly ISmartService _smartService;
        private readonly IVoiceService _voiceService;
        private readonly IProductService _productService;
        private readonly ITranslationService _translationService;
        private readonly ILocalizationService _localizationService;
        private readonly IDialogService _dialogService;
        private readonly IBarcodeWorkflowService _barcodeWorkflowService;

        bool _isShoppingMode;
        public bool IsShoppingMode
        {
            get => _isShoppingMode;
            set
            {
                _isShoppingMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShoppingModeText));
                ToggleScreenState(value);
                _ = LoadItemsAsync(); // Refresh list to apply filter
            }
        }
        
        // Voice State
        bool _isListening;
        public bool IsListening
        {
            get => _isListening;
            set
            {
                _isListening = value;
                OnPropertyChanged();
            }
        }
        
        CancellationTokenSource? _speechCts;

        public ICommand StartListeningCommand { get; }
        public ICommand StopListeningCommand { get; }
        public ICommand ToggleListeningCommand { get; }

        public ActiveListViewModel(
            IVoiceService voiceService, 
            ISmartService smartService, 
            IProductService productService, 
            ITranslationService translationService, 
            IShoppingRepository shoppingRepository, 
            ILocalizationService localizationService,
            IDialogService dialogService,
            IBarcodeWorkflowService barcodeWorkflowService)
        {
            _voiceService = voiceService;
            _smartService = smartService;
            _productService = productService;
            _translationService = translationService;
            _shoppingRepository = shoppingRepository;
            _localizationService = localizationService;
            _dialogService = dialogService;
            _barcodeWorkflowService = barcodeWorkflowService;

            // Subscribe to ItemParsedMessage
            WeakReferenceMessenger.Default.Register<ItemParsedMessage>(this, async (r, m) =>
            {
                if (m.Value == ListId)
                {
                    await LoadItemsAsync();
                }
            });

            StartListeningCommand = new Command(async () => await StartListeningAsync());
            StopListeningCommand = new Command(async () => await StopListeningAsync());
            ToggleListeningCommand = new Command(async () =>
            {
                if (IsListening)
                    await StopListeningAsync();
                else
                    await StartListeningAsync();
            });
            
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
            OpenQuickTuneCommand = new Command<ShoppingItem>(async (item) => await OpenQuickTuneAsync(item));
            BackCommand = new Command(async () => await Shell.Current.Navigation.PopAsync());
        }

        public ICommand BackCommand { get; }

        public string ShoppingModeText => IsShoppingMode ? "Stop Shopping" : "Start Shopping";

        public async Task RefreshAsync() => await LoadItemsAsync();

        async Task LoadListDetailsAsync(string listId)
        {
             // Fallback to local DB lists
             var lists = await _shoppingRepository.GetListsAsync();
             var list = lists.FirstOrDefault(l => l.Id == listId);
             
             if (list != null)
             {
                 CurrentList = list;
                 Title = list.Name;
                 await LoadItemsAsync();
             }
        }

        async Task LoadSuggestionsAsync()
        {
             if (Suggestions.Count > 0) return;
             // Mock Suggestions or Load from History
             Suggestions.Add("Milk");
             Suggestions.Add("Bread");
             Suggestions.Add("Eggs");
             Suggestions.Add("Cheese");
             Suggestions.Add("Bananas");
        }

        async Task AddItemAsync()
        {
            string name = await _dialogService.DisplayPromptAsync("Add Item", "Name:");
            if (string.IsNullOrWhiteSpace(name)) return;
             
             // Smart Parse (Simple fallback if Gemini not used directly here)
            var newItem = new ShoppingItem 
            { 
               ListId = CurrentList.Id, 
               Name = name, 
               Quantity = 1, 
               Category = "Uncategorized",
               Unit = "qty",
               OriginalText = name,
               IsParsed = false
            };
            
            await _shoppingRepository.SaveItemAsync(newItem);
            await LoadItemsAsync();
        }

        async Task TogglePurchasedAsync(ShoppingItem item)
        {
            if (item == null) return;
            item.IsPurchased = !item.IsPurchased;
            await _shoppingRepository.SaveItemAsync(item);
            await LoadItemsAsync();
        }

        async Task EditItemAsync(ShoppingItem item)
        {
             if (item == null) return;
             // Quick Edit Name
             string newName = await _dialogService.DisplayPromptAsync("Edit Item", "Update name:", initialValue: item.Name);
             if (!string.IsNullOrWhiteSpace(newName))
             {
                 item.Name = newName;
                 await _shoppingRepository.SaveItemAsync(item);
                 await LoadItemsAsync();
             }
        }

        async Task DeleteItemAsync(ShoppingItem item)
        {
            if (item == null) return;
            bool confirm = await _dialogService.DisplayAlertAsync("Delete", $"Remove '{item.Name}'?", "Yes", "No");
            if (!confirm) return;
            
            try 
            {
               await _shoppingRepository.DeleteItemAsync(item);
               await LoadItemsAsync();
            }
            catch(Exception ex)
            {
               FileLoggerService.LogStatic($"[ActiveListVM] Delete Error: {ex.Message}");
            }
        }

        async Task AddSuggestionAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
             var newItem = new ShoppingItem { ListId = CurrentList.Id, Name = name, Category = "Uncategorized", Quantity = 1 };
             await _shoppingRepository.SaveItemAsync(newItem);
             await LoadItemsAsync();
        }

        async Task ShowSortOptionsAsync()
        {
             string action = await _dialogService.DisplayActionSheetAsync("Sort By", "Cancel", null, "Category", "Name", "Date Added", "Status");
             if (action == "Category") CurrentSort = SortOption.Category;
             else if (action == "Name") CurrentSort = SortOption.Name;
             else if (action == "Date Added") CurrentSort = SortOption.DateAdded;
             else if (action == "Status") CurrentSort = SortOption.Status;
        }

        void ToggleScreenState(bool isShopping)
        {
             // Signal UI update if needed
        }


        
        // ... LoadItemsAsync Refactor ...
        async Task LoadItemsAsync()
        {
             if (CurrentList == null) return;
             
             try 
             {
                 // Fetch data asynchronously (non-blocking)
                 var items = await _shoppingRepository.GetItemsAsync(CurrentList.Id); 
                 
                 // Filter out bought items if in Shopping Mode
                 if (IsShoppingMode)
                 {
                     items = items.Where(i => !i.IsPurchased).ToList();
                 }

                 // Sorting Logic
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
                        sortedItems = items.OrderBy(i => i.Name); 
                        break; 
                 }
                 
                 var groups = sortedItems.GroupBy(i => i.Category ?? "Uncategorized")
                                   .Select(g => new ShoppingItemGroup(g.Key, g))
                                   .ToList();

                   // Sort Groups
                 if (CurrentSort == SortOption.Category)
                 {
                     groups = groups.OrderBy(g => ShoppingItem.DefaultCategories.IndexOf(g.Name) >= 0 ? ShoppingItem.DefaultCategories.IndexOf(g.Name) : 999).ToList();
                 }
                 else
                 {
                     groups = groups.OrderBy(g => ShoppingItem.DefaultCategories.IndexOf(g.Name) >= 0 ? ShoppingItem.DefaultCategories.IndexOf(g.Name) : 999).ToList();
                 }
                 
                 // Dispatch to Main Thread ONLY for UI updates
                 MainThread.BeginInvokeOnMainThread(() =>
                 {
                     Items.Clear();
                     foreach(var group in groups)
                     {
                         Items.Add(group);
                     }
                 });
                 
                 await LoadSuggestionsAsync();
             } 
             catch(Exception ex)
             {
                 FileLoggerService.LogStatic($"Error loading items: {ex.Message}");
             }
        }

        async Task StartListeningAsync()
        {
            FileLoggerService.LogStatic("[VoiceCmd] StartListeningAsync invoked");
            if (IsListening) 
            {
                FileLoggerService.LogStatic("[VoiceCmd] Already listening, aborting.");
                return;
            }
            IsListening = true;
            _speechCts = new CancellationTokenSource();

            try 
            {
                FileLoggerService.LogStatic("[VoiceCmd] Awaiting _voiceService.ListenAsync...");
                var text = await _voiceService.ListenAsync(_speechCts.Token);
                FileLoggerService.LogStatic($"[VoiceCmd] ListenAsync returned text: '{text}'");
                
                if (!string.IsNullOrWhiteSpace(text))
                {
                     FileLoggerService.LogStatic($"[VoiceCmd] Executing Smart Parse on text: {text}");
                     
                     // Use the existing SmartService explicitly to parse the text immediately
                     var parsed = _smartService.ParseItemInput(text);
                     
                     var newItem = new ShoppingItem 
                     { 
                         ListId = CurrentList.Id, 
                         Name = parsed.Name, 
                         Quantity = parsed.Quantity, 
                         Unit = parsed.Unit, 
                         Category = "Voice",
                         IsParsed = false,
                         OriginalText = text
                     };
                     FileLoggerService.LogStatic($"[VoiceCmd] Saving parsed item: {newItem.Name}");
                     await _shoppingRepository.SaveItemAsync(newItem);
                     await LoadItemsAsync();
                     
                     // FR-2.5: Voice Confirm
                     var locale = _localizationService.GetCurrentCulture().Name;
                     var options = new Microsoft.Maui.Media.SpeechOptions { Locale = Microsoft.Maui.Media.TextToSpeech.GetLocalesAsync().Result.FirstOrDefault(l => l.Language == locale) };
                     await Microsoft.Maui.Media.TextToSpeech.SpeakAsync($"Added {parsed.Name}", options);
                }
                else
                {
                     FileLoggerService.LogStatic("[VoiceCmd] Returned text was empty or null.");
                }
            }
            catch (Exception ex)
            {
                 FileLoggerService.LogStatic($"[VoiceCmd] Exception in StartListeningAsync: {ex}");
                 // Ignore standard cancellations
            }
            finally
            {
                IsListening = false;
                FileLoggerService.LogStatic("[VoiceCmd] StartListeningAsync completed.");
            }
        }

        async Task StopListeningAsync()
        {
            FileLoggerService.LogStatic("[VoiceCmd] StopListeningAsync invoked");
            if (_speechCts != null && !_speechCts.IsCancellationRequested)
            {
                FileLoggerService.LogStatic("[VoiceCmd] Cancelling CTS...");
                _speechCts.Cancel();
            }
            IsListening = false;
        }

        async Task ScanAsync()
        {
            if (CurrentList == null) return;
            await _barcodeWorkflowService.ExecuteScanWorkflowAsync(CurrentList.Id);
            await LoadItemsAsync();
        }
        async Task ShareListAsync()
        {
            if (CurrentList == null) return;
            string email = await _dialogService.DisplayPromptAsync("Share List", $"Enter email to share '{CurrentList.Name}' with:");
            if (string.IsNullOrWhiteSpace(email)) return;

            try
            {
                await _shoppingRepository.ShareListAsync(CurrentList.Id, email.Trim());
                await _dialogService.DisplayAlertAsync(AppResources.Success, string.Format(AppResources.Msg_InviteSuccess, email), "OK");
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayAlertAsync(AppResources.Error, string.Format(AppResources.Msg_ShareFailed, ex.Message), "OK");
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
                 var lists = await _shoppingRepository.GetListsAsync();
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
                 var lists = await _shoppingRepository.GetListsAsync();
                 var defaultList = lists.FirstOrDefault(l => l.IsDefault == true);
                 if (defaultList != null && defaultList.Id != CurrentList.Id)
                 {
                     targetList = defaultList;
                     CurrentList = targetList; // Switch UI to Default List
                 }
             }

              // Offline-First Smart Add
              var newItem = new ShoppingItem 
              { 
                  ListId = targetList.Id, 
                  Name = itemName, 
                  OriginalText = itemName,
                  IsParsed = false, // Trigger Gemini
                  Quantity = 1, 
                  Unit = "qty", 
                  Category = "Uncategorized" 
              };
              await _shoppingRepository.SaveItemAsync(newItem);
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
                 var settings = new SpeechOptions()
                 {
                     Locale = await Microsoft.Maui.Media.TextToSpeech.GetLocalesAsync().ContinueWith(t => 
                     {
                        var locales = t.Result;
                        var currentCulture = _localizationService.GetCurrentCulture();
                        // Find best matching locale (e.g. pt-BR)
                        return locales.FirstOrDefault(l => l.Language == currentCulture.TwoLetterISOLanguageName) ?? locales.FirstOrDefault();
                     })
                 };

                await Microsoft.Maui.Media.TextToSpeech.SpeakAsync(textBuilder.ToString(), settings);
             }
             catch(Exception ex)
             {
                await _dialogService.DisplayAlertAsync("Read Error", ex.Message, "OK");
             }
        }

        async Task OpenQuickTuneAsync(ShoppingItem item)
        {
             if (item == null) return;
             var popup = new QuickTunePopup(item, _shoppingRepository);
             
             // Find valid page to show popup
             var page = Shell.Current.CurrentPage ?? App.Current.MainPage;
             await page.ShowPopupAsync(popup);
             
             // After popup closes (it waits), refresh UI if needed?
             // Since we bind directly to Item in VM, UI might update automatically via INotifyPropertyChanged
             // But let's trigger a light refresh or re-sort if quantity affects it (rare)
             // item.OnPropertyChanged() is local, but list view needs to know?
             // ObservableCollection handles property changes if item implements INPC.
             // Our ShoppingItem assumes Fody/PropertyChanged or clean setters.
             // Inspecting ShoppingItem found it's a simple class.
             // We might need to force refresh the list if UI doesn't update.
             // Actually, ShoppingItem does NOT implement INotifyPropertyChanged in Models.
             // So we need to modify ShoppingItem OR reload the list.
             await LoadItemsAsync();
        }
    }
}
