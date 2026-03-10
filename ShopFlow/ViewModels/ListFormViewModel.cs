using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using CommunityToolkit.Maui.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class ListFormViewModel : BaseViewModel
    {
        private readonly IShoppingRepository _shoppingRepository;
        private readonly IDialogService _dialogService;
        private ShoppingList _existingList;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        private Color _selectedColor;
        public Color SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        private string _selectedIcon;
        public string SelectedIcon
        {
            get => _selectedIcon;
            set => SetProperty(ref _selectedIcon, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PickColorCommand { get; }
        public ICommand PickIconCommand { get; }

        public List<Color> AvailableColors { get; } = new List<Color>
        {
            Color.FromArgb("#34D399"), // Green
            Color.FromArgb("#FBBF24"), // Orange
            Color.FromArgb("#60A5FA"), // Blue
            Color.FromArgb("#F472B6"), // Pink
            Color.FromArgb("#A78BFA"), // Purple
            Color.FromArgb("#F87171"), // Red
            Color.FromArgb("#9ca3af"), // Gray
            Color.FromArgb("#111827")  // Dark
        };

        public ListFormViewModel(ShoppingList list = null)
        {
            // Manual DI for simplicity in this detail view, or resolved via shell navigation passing ID
            _shoppingRepository = ServiceHelper.GetService<IShoppingRepository>();
            _dialogService = ServiceHelper.GetService<IDialogService>();
            
            _existingList = list;

            if (_existingList != null)
            {
                Title = "Edit List";
                Name = _existingList.Name;
                IsDefault = _existingList.IsDefault ?? false;
                
                if(!string.IsNullOrEmpty(_existingList.Color))
                     SelectedColor = Color.FromHex(_existingList.Color);
                else
                     SelectedColor = AvailableColors[0];

                SelectedIcon = !string.IsNullOrEmpty(_existingList.Icon) ? _existingList.Icon : "📝";
            }
            else
            {
                Title = "New List";
                SelectedColor = AvailableColors[0];
                SelectedIcon = "📝";
            }

            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await Shell.Current.Navigation.PopAsync());
            PickColorCommand = new Command<Color>(c => SelectedColor = c);
            PickIconCommand = new Command(async () => await PickIconAsync());
        }

        async Task PickIconAsync()
        {
            var popup = new Views.Popups.EmojiPickerPopup();
            var result = await Shell.Current.CurrentPage.ShowPopupAsync(popup);

            if (result is string emoji && !string.IsNullOrWhiteSpace(emoji))
            {
                SelectedIcon = emoji;
            }
        }

        async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _dialogService.DisplayAlertAsync("Error", "Name is required.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                bool isNew = _existingList == null;
                var listToSave = _existingList ?? new ShoppingList();

                listToSave.Name = Name.Trim();
                listToSave.Color = SelectedColor.ToHex();
                listToSave.Icon = SelectedIcon;
                
                // Handle Default Logic
                if (IsDefault)
                {
                    // If this is becoming default, unset others locally
                     var otherLists = await _shoppingRepository.GetListsAsync();
                     foreach(var l in otherLists.Where(x => x.IsDefault == true && x.Id != listToSave.Id))
                     {
                         l.IsDefault = false;
                         l.IsDirty = true;
                         await _shoppingRepository.SaveListAsync(l);
                     }
                }
                listToSave.IsDefault = IsDefault;

                await _shoppingRepository.SaveListAsync(listToSave);
                
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                 await _dialogService.DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
