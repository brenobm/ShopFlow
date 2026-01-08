using CommunityToolkit.Maui.Views;

namespace BrenoMachado.ShopFlow.Views.Popups;

public partial class EmojiPickerPopup : Popup
{
	public EmojiPickerPopup()
	{
		InitializeComponent();
	}

    private void OnEmojiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string emoji)
        {
            Close(emoji);
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null);
    }
}
