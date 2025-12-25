using System.Collections.ObjectModel;

namespace BrenoMachado.ShopFlow.Models;

public class ShoppingItemGroup : ObservableCollection<ShoppingItem>
{
    public string Name { get; private set; }

    public ShoppingItemGroup(string name, IEnumerable<ShoppingItem> items) : base(items)
    {
        Name = name;
    }
}
