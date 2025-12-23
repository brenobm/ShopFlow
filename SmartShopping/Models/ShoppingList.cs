using SQLite;

namespace BrenoMachado.SmartShopping.Models
{
    public class ShoppingList
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Ignore]
        public List<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
    }
}
