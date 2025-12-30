using SQLite;

namespace BrenoMachado.ShopFlow.Models
{
    public class BarcodeMetric
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Barcode { get; set; }
        public bool WasFoundOnline { get; set; } // True = Match, False = Miss
        public DateTime DateSearched { get; set; }
    }
}
