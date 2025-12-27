using SQLite;

namespace BrenoMachado.ShopFlow.Models
{
    public class PurchaseHistory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public DateTime DatePurchased { get; set; }
    }
}
