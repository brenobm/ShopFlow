using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views
{
    public partial class MainContainerPage : ContentPage
    {
        public MainContainerPage()
        {
            try
            {
                FileLoggerService.LogStatic("[MainContainerPage] Constructor started.");
                InitializeComponent();
                NavigationPage.SetHasNavigationBar(this, false); // FIX 1: Hide default header
                FileLoggerService.LogStatic("[MainContainerPage] Constructor finished (InitializeComponent done).");
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[MainContainerPage] CRASH in Constructor: {ex}");
            }
        }

        protected override void OnAppearing()
        {
             try
            {
                base.OnAppearing();
                FileLoggerService.LogStatic("[MainContainerPage] OnAppearing called.");
                
                // FIX 2: Manually trigger OnAppearing for child ViewModels to load data
                if (BindingContext is MainContainerViewModel vm)
                {
                    // If we had access to Child VMs here we could call them.
                    // Instead, let's find the DashboardView in the visual tree or use MessagingCenter? 
                    // Better: MainContainerViewModel could expose CurrentPageVM.
                    // EASIEST FIX: Use MessagingCenter or just find the ViewModel from DI since it is Singleton?
                    // Actually, DashboardView resolves its own VM. 
                    
                    var dashboardVM = IPlatformApplication.Current.Services.GetService<DashboardViewModel>();
                    dashboardVM?.OnAppearing();
                }
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[MainContainerPage] CRASH in OnAppearing: {ex}");
            }
        }
    }
    
    // Simple Converters for the Pill Logic
    public class BoolToColorConverter : IValueConverter
    {
        public Color SelectedColor { get; set; } = Colors.Black; // Default logic
        public Color UnselectedColor { get; set; } = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                // If param is 'Selected', return Black/Primary.
                // Actually, the bindings in XAML might need to be specific.
                // Let's implement simple logic: True -> #1F2937 (Dark), False -> Transparent
                return Color.FromArgb("#1F2937"); 
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return Colors.White;
            }
            return Color.FromArgb("#1F2937"); // Dark text when unselected
        }
         public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
