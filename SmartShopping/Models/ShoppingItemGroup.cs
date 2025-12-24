using System.Collections.ObjectModel;

namespace BrenoMachado.SmartShopping.Models
{
    public class ShoppingItemGroup : ObservableCollection<ShoppingItem>
    {
        public string Name { get; private set; }

        public ShoppingItemGroup(string name, IEnumerable<ShoppingItem> items) : base(items)
        {
            Name = name;
        }
    }
}
