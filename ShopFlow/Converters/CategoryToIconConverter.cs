using System.Globalization;

namespace BrenoMachado.ShopFlow.Converters;

public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string category)
        {
            return category.ToLower() switch
            {
                "produce" or "vegetables" or "hortifruti" => "🥬",
                "fruits" or "frutas" => "🍎",
                "dairy" or "laticínios" => "🥛",
                "meat" or "carnes" => "🥩",
                "bakery" or "padaria" => "🥖",
                "beverages" or "bebidas" => "🥤",
                "frozen" or "congelados" => "🧊",
                "pantry" or "despensa" => "🥫",
                "household" or "limpeza" => "🧹",
                "personal care" or "higiene" => "🧴",
                "pharmacy" or "farmácia" => "💊",
                _ => "🛒"
            };
        }
        return "🛒";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
