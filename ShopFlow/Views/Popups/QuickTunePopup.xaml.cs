using CommunityToolkit.Maui.Views;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Repositories;

namespace BrenoMachado.ShopFlow.Views.Popups;

public partial class QuickTunePopup : Popup
{
    private readonly ShoppingItem _item;
    private readonly IShoppingRepository _repository;
    
    // Bindable Properties for UI
    public double Quantity { get; set; }
    public string Unit { get; set; }
    
    public QuickTunePopup(ShoppingItem item, IShoppingRepository repository)
    {
        InitializeComponent();
        _item = item;
        _repository = repository;
        
        Quantity = item.Quantity;
        Unit = item.Unit;
        
        BindingContext = this;
    }

    private void OnMinusClicked(object sender, EventArgs e)
    {
        if (Quantity > 1) { Quantity--; }
        else if (Quantity > 0.1) { Quantity -= 0.1; } // Micro-adjust for small
        
        Quantity = Math.Round(Quantity, 2);
        UpdateDisplay();
    }

    private void OnPlusClicked(object sender, EventArgs e)
    {
        if (Quantity < 1) { Quantity += 0.1; }
        else { Quantity++; }
        
        Quantity = Math.Round(Quantity, 2);
        UpdateDisplay();
    }
    
    private void OnUnitClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            Unit = btn.Text.ToLower();
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(Unit));
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        _item.Quantity = Quantity;
        _item.Unit = Unit;
        
        await _repository.SaveItemAsync(_item);
        Close();
    }
}
