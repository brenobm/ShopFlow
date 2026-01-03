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

    // UI Properties (Not persisted)
    [Ignore]
    public string Initials { get; set; }
    [Ignore]
    public Microsoft.Maui.Graphics.Color PrimaryColor { get; set; }
    [Ignore]
    public Microsoft.Maui.Graphics.Color SecondaryColor { get; set; }
    
    // Stats for Dashboard
    [Ignore]
    public int TotalItemsCount { get; set; }
    [Ignore]
    public int BoughtItemsCount { get; set; }
    
    [Ignore]
    public double Progress => TotalItemsCount == 0 ? 0 : (double)BoughtItemsCount / TotalItemsCount;
}
