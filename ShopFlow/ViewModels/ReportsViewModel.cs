using System.Collections.ObjectModel;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Repositories;
using System.Windows.Input;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IShoppingRepository _shoppingRepository;

        public ObservableCollection<TopItemStat> TopItems { get; } = new();
        
        string _weeklySummary;
        public string WeeklySummary
        {
            get => _weeklySummary;
            set
            {
                _weeklySummary = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }

        public ReportsViewModel(IShoppingRepository shoppingRepository)
        {
            _shoppingRepository = shoppingRepository;
            Title = "Reports & History";
            RefreshCommand = new Command(async () => await LoadStatsAsync());
        }

        public async Task InitializeAsync()
        {
            await LoadStatsAsync();
        }

        async Task LoadStatsAsync()
        {
            IsBusy = true;
            try
            {
                var history = await _shoppingRepository.GetPurchaseHistoryAsync();

                // 1. Weekly Stats
                var last7Days = DateTime.Now.AddDays(-7);
                var recentItems = history.Where(h => h.DatePurchased >= last7Days).Count();
                WeeklySummary = $"{recentItems} items purchased in the last 7 days.";

                // 2. Top Items
                var grouped = history.GroupBy(h => h.ItemName)
                                     .Select(g => new TopItemStat { Name = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count)
                                     .Take(5)
                                     .ToList();

                TopItems.Clear();
                foreach (var item in grouped)
                {
                    TopItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"Error loading stats: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class TopItemStat
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
