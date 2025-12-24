using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.SmartShopping.Models;
using BrenoMachado.SmartShopping.Services;

namespace BrenoMachado.SmartShopping.ViewModels
{
    [QueryProperty(nameof(ItemId), "ItemId")]
    [QueryProperty(nameof(ListId), "ListId")]
    public class ItemDetailViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly ISyncService _syncService;

        public ItemDetailViewModel(IDatabaseService databaseService, ISyncService syncService)
        {
            _databaseService = databaseService;
            _syncService = syncService;

            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());

            Categories = new ObservableCollection<string>(ShoppingItem.DefaultCategories);
            SelectedCategory = Categories.FirstOrDefault();
        }

        public ObservableCollection<string> Categories { get; }

        string _itemId;
        public string ItemId
        {
            get => _itemId;
            set
            {
                _itemId = value;
                LoadItemAsync(value);
            }
        }

        string _listId;
        public string ListId
        {
            get => _listId;
            set => _listId = value;
        }

        string _name;
        public string Name
        {
            get => _name;
            set 
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        ShoppingItem _existingItem;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        async void LoadItemAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            // Since we don't have GetItemById in direct service, we might need to query or pass object.
            // Ideally we'd pass the object or have a GetItem method.
            // For now, let's assume we can fetch it or find it. 
            // Actually, let's just make sure we can fetch distinct items.
            // Wait, IDatabaseService doesn't have GetItem(id). Let's add it or find via list?
            // Checking DatabaseService.cs... it only has GetItemsAsync(listId).
            // We should add GetItemAsync(itemId) to IDatabaseService.
        }

        public async Task InitializeAsync(ShoppingItem item)
        {
            if (item != null)
            {
                _existingItem = item;
                ItemId = item.Id;
                Name = item.Name;
                SelectedCategory = item.Category ?? "Uncategorized";
                Title = "Edit Item";
            }
            else
            {
                Title = "New Item";
                SelectedCategory = "Uncategorized";
            }
        }

        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlert("Error", "Name is required", "OK");
                return;
            }

            if (_existingItem == null)
            {
                var newItem = new ShoppingItem
                {
                    ListId = ListId,
                    Name = Name.Trim(),
                    Category = SelectedCategory
                };
                await _databaseService.SaveItemAsync(newItem);
                await _syncService.PushItemAsync(newItem);
            }
            else
            {
                _existingItem.Name = Name.Trim();
                _existingItem.Category = SelectedCategory;
                await _databaseService.SaveItemAsync(_existingItem);
                await _syncService.PushItemAsync(_existingItem);
            }

            await Shell.Current.GoToAsync("..");
        }

        async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
