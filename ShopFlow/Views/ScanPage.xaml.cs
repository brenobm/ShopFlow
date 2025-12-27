using ZXing.Net.MAUI;

namespace BrenoMachado.ShopFlow.Views;

public partial class ScanPage : ContentPage
{
    private TaskCompletionSource<string> _tcs;

    public ScanPage()
    {
        InitializeComponent();
        _tcs = new TaskCompletionSource<string>();
        
        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false
        };
    }

    // Static method for easy calling and returning result
    public static async Task<string> ScanAsync(INavigation navigation)
    {
        var page = new ScanPage();
        await navigation.PushModalAsync(page);
        var result = await page.WaitForResultAsync();
        await navigation.PopModalAsync();
        return result;
    }

    private Task<string> WaitForResultAsync()
    {
        return _tcs.Task;
    }

    private void CameraView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (e.Results.Length > 0)
        {
            var value = e.Results[0].Value;
            if (!string.IsNullOrEmpty(value))
            {
                 // Debounce or ensure only one result is returned;
                 cameraView.IsDetecting = false; 
                 // Fire and forget MainThread dispatch to allow UI update
                 MainThread.BeginInvokeOnMainThread(() => 
                 {
                     _tcs.TrySetResult(value);
                 });
            }
        }
    }
}
