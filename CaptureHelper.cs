using System;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.UI.Xaml.Controls;

namespace VideoRecordingCC
{
    public static class CaptureHelper
    {
        public static async Task<GraphicsCaptureItem> PickCaptureItemAsync()
        {
            var picker = new GraphicsCapturePicker();
            return await picker.PickSingleItemAsync();
        }
    }
}
