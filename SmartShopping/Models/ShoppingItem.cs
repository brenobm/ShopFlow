using SQLite;

namespace BrenoMachado.SmartShopping.Models
{
    public class ShoppingItem
    {
        [PrimaryKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Indexed]
        public string ListId { get; set; }
        
        public string Name { get; set; }
        public bool IsPurchased { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PurchasedAt { get; set; }
    }
}
