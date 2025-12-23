using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.SmartShopping.Models;
using BrenoMachado.SmartShopping.Services;
using BrenoMachado.SmartShopping.Views;

namespace BrenoMachado.SmartShopping.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;

        public ObservableCollection<ShoppingList> Lists { get; } = new();

        public ICommand LoadListsCommand { get; }
        public ICommand AddListCommand { get; }
        public ICommand OpenListCommand { get; }
        public ICommand EditListCommand { get; }
        public ICommand DeleteListCommand { get; }

        public DashboardViewModel(IDatabaseService databaseService)
        {
            Title = "My Lists";
            _databaseService = databaseService;

            LoadListsCommand = new Command(async () => await LoadListsAsync());
            AddListCommand = new Command(async () => await AddListAsync());
            OpenListCommand = new Command<ShoppingList>(async (list) => await OpenListAsync(list));
            EditListCommand = new Command<ShoppingList>(async (list) => await EditListAsync(list));
            DeleteListCommand = new Command<ShoppingList>(async (list) => await DeleteListAsync(list));
        }

        async Task LoadListsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var lists = await _databaseService.GetListsAsync();
                Lists.Clear();
                foreach (var list in lists)
                {
                    // Optionally load item count here if needed, or do it in the query
                    Lists.Add(list);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        async Task AddListAsync()
        {
             string name = await Shell.Current.DisplayPromptAsync("New List", "Enter list name");
             if (string.IsNullOrWhiteSpace(name))
                 return;

             var newList = new ShoppingList { Name = name.Trim() };
             await _databaseService.SaveListAsync(newList);
             await LoadListsAsync();
        }

        async Task EditListAsync(ShoppingList list)
        {
            if (list == null) return;
            string name = await Shell.Current.DisplayPromptAsync("Edit List", "Enter new list name", initialValue: list.Name);
            if (string.IsNullOrWhiteSpace(name) || name == list.Name) return;

            list.Name = name.Trim();
            await _databaseService.SaveListAsync(list);
            await LoadListsAsync();
        }

        async Task DeleteListAsync(ShoppingList list)
        {
            if (list == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Delete", $"Delete list '{list.Name}'?", "Yes", "No");
            if (confirm)
            {
                await _databaseService.DeleteListAsync(list);
                await LoadListsAsync();
            }
        }

        async Task OpenListAsync(ShoppingList list)
        {
            if (list == null) return;
            
            // Navigate to ActiveListPage passing the list ID
            await Shell.Current.GoToAsync($"{nameof(ActiveListPage)}?Id={list.Id}");
        }
        
        public void OnAppearing()
        {
            IsBusy = false;
            LoadListsCommand.Execute(null);
        }
    }
}
