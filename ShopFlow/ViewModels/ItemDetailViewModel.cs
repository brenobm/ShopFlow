using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.ViewModels
{
    [QueryProperty(nameof(ItemId), "ItemId")]
    [QueryProperty(nameof(ListId), "ListId")]
    [QueryProperty(nameof(Name), "Name")]
    [QueryProperty(nameof(SelectedCategory), "Category")]
    [QueryProperty(nameof(Price), "Price")]
    public class ItemDetailViewModel : BaseViewModel
    {
        private readonly IGraphQLService _graphQLService;

        public ItemDetailViewModel(IGraphQLService graphQLService)
        {
            _graphQLService = graphQLService;

            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
            AddCategoryCommand = new Command(async () => await AddCategoryAsync());

            Categories = new ObservableCollection<string>(ShoppingItem.DefaultCategories);
            SelectedCategory = Categories.FirstOrDefault();
        }

        public ICommand AddCategoryCommand { get; }

        async Task AddCategoryAsync()
        {
            string newCategory = await Shell.Current.DisplayPromptAsync("New Category", "Enter category name:");
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
                await Shell.Current.DisplayAlert("Error", "Name is required", "OK");
                return;
            }

            try 
            {
                if (!string.IsNullOrEmpty(ItemId))
                {
                    // Workaround: Delete existing to update details (incl. Category/Price)
                     await _graphQLService.DeleteItemAsync(ListId, ItemId);
                }
                
                // Add New (or Re-Add)
                string newItemId = ItemId;
                if (string.IsNullOrEmpty(newItemId))
                {
                    newItemId = Guid.NewGuid().ToString();
                }

                await _graphQLService.AddItemAsync(ListId, newItemId, Name.Trim(), 1, "qty", SelectedCategory, Price);
                
                await Shell.Current.GoToAsync("..");
            } 
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Save failed: {ex.Message}", "OK");
            }
        }

        async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
