using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views
{
    public partial class ActiveListPage : ContentPage, IQueryAttributable
    {
        public ActiveListPage(ActiveListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            
            // Push-to-Talk Logic
            if (VoiceBtn != null)
            {
                VoiceBtn.Pressed += (s, e) => viewModel.StartListeningCommand.Execute(null);
                VoiceBtn.Released += (s, e) => viewModel.StopListeningCommand.Execute(null);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if(BindingContext is ActiveListViewModel vm)
            {
                await vm.RefreshAsync();
            }
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (BindingContext is ActiveListViewModel vm)
            {
                if (query.ContainsKey("action") && query["action"].ToString() == "add")
                {
                    if (query.ContainsKey("item"))
                    {
                        string newItem = query["item"].ToString();
                         // Wait for VM to be ready or just fire and forget?
                        await vm.AddItemFromSiriAsync(newItem);
                    }
                }
            }
        }
    }
}
