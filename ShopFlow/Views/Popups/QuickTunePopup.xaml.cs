using CommunityToolkit.Maui.Views;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Repositories;

namespace BrenoMachado.ShopFlow.Views.Popups;

public partial class QuickTunePopup : Popup
{
    private readonly ShoppingItem _item;
    private readonly IShoppingRepository _repository;
    
    // Bindable Properties for UI
    private double _quantity;
    public double Quantity 
    { 
        get => _quantity; 
        set
        {
            _quantity = value;
            OnPropertyChanged();
        } 
    }

    private string _unit;
    public string Unit 
    { 
        get => _unit; 
        set
        {
            _unit = value;
            OnPropertyChanged();
        } 
    }
    
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
    }

    private void OnPlusClicked(object sender, EventArgs e)
    {
        if (Quantity < 1) { Quantity += 0.1; }
        else { Quantity++; }
        
        Quantity = Math.Round(Quantity, 2);
    }
    
    private void OnUnitClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
             // Handle localized text if needed, but for now assuming code matches logic
             // Ideally we map button text to key, but here we just lowercase it
             string txt = btn.Text?.ToLower() ?? "qty";
             
             // Simple mapping back to canonical codes if localized
             if (txt == "un" || txt == "unidade") Unit = "qty";
             else if (txt == "pacote") Unit = "pack";
             else Unit = txt;
        }
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        _item.Quantity = Quantity;
        _item.Unit = Unit;
        
        await _repository.SaveItemAsync(_item);
        Close();
    }
}
