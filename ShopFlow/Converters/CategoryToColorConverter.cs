using System.Globalization;

namespace BrenoMachado.ShopFlow.Converters;

public class CategoryToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string category)
        {
            var color = category.ToLower() switch
            {
                "produce" or "fruits" or "vegetables" or "hortifruti" => Color.FromArgb("#4ADE80"), // Green
                "dairy" or "laticínios" => Color.FromArgb("#60A5FA"), // Blue
                "meat" or "carnes" => Color.FromArgb("#F87171"), // Red
                "bakery" or "padaria" => Color.FromArgb("#FBBF24"), // Orange
                "beverages" or "bebidas" => Color.FromArgb("#A78BFA"), // Purple
                "frozen" or "congelados" => Color.FromArgb("#38BDF8"), // Light Blue
                "pantry" or "despensa" => Color.FromArgb("#94A3B8"), // Slate
                "household" or "limpeza" => Color.FromArgb("#FBbf24"), // Yellow
                "personal care" or "higiene" => Color.FromArgb("#F472B6"), // Pink
                _ => Color.FromArgb("#9CA3AF") // Default Gray
            };

            if (parameter is string alphaString && float.TryParse(alphaString, NumberStyles.Any, CultureInfo.InvariantCulture, out float alpha))
            {
                return color.WithAlpha(alpha);
            }
            return color;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
