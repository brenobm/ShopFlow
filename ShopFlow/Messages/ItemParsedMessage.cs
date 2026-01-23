using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BrenoMachado.ShopFlow.Messages
{
    public class ItemParsedMessage : ValueChangedMessage<string>
    {
        public ItemParsedMessage(string listId) : base(listId)
        {
        }
    }
}
