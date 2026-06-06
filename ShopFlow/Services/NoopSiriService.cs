namespace BrenoMachado.ShopFlow.Services;

public class NoopSiriService : ISiriService
{
    public void DonateAddItemShortcut() { }
    public Task PresentAddVoiceShortcutAsync() => Task.CompletedTask;
    public void DonateAddedItemInteraction(string itemName) { }
}
