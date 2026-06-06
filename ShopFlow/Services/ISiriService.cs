namespace BrenoMachado.ShopFlow.Services;

public interface ISiriService
{
    void DonateAddItemShortcut();
    Task PresentAddVoiceShortcutAsync();
    void DonateAddedItemInteraction(string itemName);
}
