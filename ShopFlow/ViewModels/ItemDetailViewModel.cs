using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Repositories;

namespace BrenoMachado.ShopFlow.ViewModels
{
    [QueryProperty(nameof(ItemId), "ItemId")]
    [QueryProperty(nameof(ListId), "ListId")]
    [QueryProperty(nameof(Name), "Name")]
    [QueryProperty(nameof(SelectedCategory), "Category")]
    [QueryProperty(nameof(Price), "Price")]
    public class ItemDetailViewModel : BaseViewModel
    {
        private readonly IShoppingRepository _shoppingRepository;
        private readonly IDialogService _dialogService;

        public ItemDetailViewModel(IShoppingRepository shoppingRepository, IDialogService dialogService)
        {
            _shoppingRepository = shoppingRepository;
            _dialogService = dialogService;

            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
            AddCategoryCommand = new Command(async () => await AddCategoryAsync());

            Categories = new ObservableCollection<string>(ShoppingItem.DefaultCategories);
            SelectedCategory = Categories.FirstOrDefault();
        }

        public ICommand AddCategoryCommand { get; }

        async Task AddCategoryAsync()
        {
            string newCategory = await _dialogService.DisplayPromptAsync("New Category", "Enter category name:");
            if (!string.IsNullOrWhiteSpace(newCategory))
            {
                newCategory = newCategory.Trim();
                if (!Categories.Contains(newCategory))
                {
                    Categories.Add(newCategory);
                }
                SelectedCategory = newCategory;
            }
        }

        public ObservableCollection<string> Categories { get; }

        string _itemId;
        public string ItemId
        {
            get => _itemId;
            set
            {
                _itemId = value;
                if(!string.IsNullOrEmpty(value)) Title = "Edit Item";
                else Title = "New Item";
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
                _name = Uri.UnescapeDataString(value ?? "");
                OnPropertyChanged();
            }
        }
        
        double? _price;
        public double? Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }

        string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = Uri.UnescapeDataString(value ?? "");
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _dialogService.DisplayAlertAsync("Error", "Name is required", "OK");
                return;
            }

            if (IsBusy) return;
            IsBusy = true;

            try 
            {
                ShoppingItem item;
                if (!string.IsNullOrEmpty(ItemId))
                {
                    item = await _shoppingRepository.GetItemAsync(ItemId);
                    if (item == null)
                    {
                         item = new ShoppingItem { Id = ItemId, ListId = ListId };
                    }
                }
                else
                {
                    item = new ShoppingItem { ListId = ListId };
                }

                item.Name = Name.Trim();
                item.Category = SelectedCategory;
                item.Price = Price;
                item.Quantity = item.Quantity > 0 ? item.Quantity : 1;
                item.Unit = string.IsNullOrEmpty(item.Unit) ? "qty" : item.Unit;

                await _shoppingRepository.SaveItemAsync(item);
                
                await Shell.Current.GoToAsync("..");
            } 
            catch(Exception ex)
            {
                await _dialogService.DisplayAlertAsync("Error", $"Save failed: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
