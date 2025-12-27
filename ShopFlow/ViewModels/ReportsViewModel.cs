using System.Collections.ObjectModel;
using System.Windows.Input;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;

        public ObservableCollection<ChartItem> TopItems { get; } = new();
        public ObservableCollection<ChartItem> CategoryDistribution { get; } = new();
        public ObservableCollection<PurchaseHistory> History { get; } = new();

        public ICommand RefreshCommand { get; }

        public ReportsViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Insights & History";
            RefreshCommand = new Command(async () => await LoadReportsAsync());
        }

        public async Task LoadReportsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var history = await _databaseService.GetPurchaseHistoryAsync();

                // 1. History Tab Data
                History.Clear();
                foreach (var h in history.OrderByDescending(x => x.DatePurchased))
                {
                    History.Add(h);
                }

                // 2. Top Items (Bar Chart Data)
                var top = history.GroupBy(x => x.ItemName)
                                 .Select(g => new ChartItem 
                                 { 
                                     Label = g.Key, 
                                     Value = g.Count(), 
                                     ValueLabel = $"{g.Count()} times" 
                                 })
                                 .OrderByDescending(x => x.Value)
                                 .Take(5)
                                 .ToList();

                // Normalize for "Bar" width (0 to 1) or UI sizing
                double maxVal = top.Any() ? top.Max(x => x.Value) : 1;
                foreach(var t in top) t.NormalizedValue = t.Value / maxVal;

                TopItems.Clear();
                foreach (var t in top) TopItems.Add(t);

                // 3. Category Distribution
                var cats = history.GroupBy(x => x.Category ?? "Other")
                                  .Select(g => new ChartItem
                                  {
                                      Label = g.Key,
                                      Value = g.Count(),
                                      ValueLabel = $"{g.Count()}"
                                  })
                                  .OrderByDescending(x => x.Value)
                                  .ToList();
                
                double maxCat = cats.Any() ? cats.Max(x => x.Value) : 1;
                foreach (var c in cats) c.NormalizedValue = c.Value / maxCat;

                CategoryDistribution.Clear();
                foreach (var c in cats) CategoryDistribution.Add(c);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading reports: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class ChartItem
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public string ValueLabel { get; set; }
        public double NormalizedValue { get; set; } // 0.0 to 1.0 for UI binding
        public double BarWidth => NormalizedValue * 200; // Example pixel width
    }
}
