using SQLite;

namespace BrenoMachado.ShopFlow.Models;

public class User
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; }
    public string Name { get; set; }
}
