using SQLite;

namespace BrenoMachado.ShopFlow.Models;

public class ShoppingList
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Name { get; set; }
    public string OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool? IsDefault { get; set; }
    
    // Sync Flags
    public bool IsDirty { get; set; }
    public bool IsDeleted { get; set; }
    
    [Ignore]
    public List<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
}
