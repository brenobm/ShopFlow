using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace BrenoMachado.ShopFlow.Views
{
    public partial class MainContainerPage : ContentPage
    {
        public MainContainerPage()
        {
            InitializeComponent();
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
