using SQLite;
using Newtonsoft.Json;

namespace BrenoMachado.ShopFlow.Models;

public class ShoppingItem : System.ComponentModel.INotifyPropertyChanged
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty("quantity")]
    public double Quantity { get; set; }

    [JsonProperty("price")]
    public double? Price { get; set; }

    [JsonProperty("unit")]
    public string Unit { get; set; }
    
    [Indexed]
    public string ListId { get; set; }
    
    public string Name { get; set; }
    
    private bool _isPurchased;
    public bool IsPurchased 
    { 
        get => _isPurchased; 
        set 
        {
            if (_isPurchased != value)
            {
                _isPurchased = value;
                OnPropertyChanged(nameof(IsPurchased));
            }
        }
    }

    public string Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PurchasedAt { get; set; }
    
    // Sync Flags
    public bool IsDirty { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastUpdated { get; set; }

    public static List<string> DefaultCategories = new List<string>
    {
        "Uncategorized",
        "Produce",
        "Dairy & Eggs",
        "Meat & Poultry",
        "Bakery",
        "Frozen Foods",
        "Pantry",
        "Beverages",
        "Household",
        "Personal Care",
        "Other"
    };

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
