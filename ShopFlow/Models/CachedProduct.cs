using SQLite;

namespace BrenoMachado.ShopFlow.Models
{
    public class CachedProduct
    {
        [PrimaryKey]
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
