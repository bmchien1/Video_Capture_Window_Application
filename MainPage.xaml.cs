using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Media.Editing;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using System.ComponentModel;
using Windows.Graphics.DirectX;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Windows.Media.Core;

namespace VideoRecordingCC
{
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private bool isFlipping = false;
        private bool isPreviewing;
        private bool isRecording = false;
        private bool enabledAudio = false;
        private StorageFile recordedFile;
        private List<VideoSetting> AvailableVideoSetting = new List<VideoSetting>();
        private string CurrentVideoSetting = null;

        private GraphicsCaptureItem captureItem;
        private CanvasDevice canvasDevice;
        private CanvasSwapChain swapChain;
        private Direct3D11CaptureFramePool framePool;
        private GraphicsCaptureSession captureSession;

        private bool isCapturingScreen = false;

        public MainPage()
        {
            InitializeMediaCaptureAsync();
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine(CurrentVideoSetting);
            Debug.WriteLine(AvailableVideoSetting.Count());
            if (e.Parameter != null && !"".Equals(e.Parameter.ToString()))
            {
                CurrentVideoSetting = e.Parameter.ToString();
                if (AvailableVideoSetting.Count() > 0)
                {
                    SetPreviewSettings(CurrentVideoSetting);
                }
            }
            //foreach (VideoSetting setting in AvailableVideoSetting)
            //{
            //    Debug.WriteLine(setting.Display);
            //    if (CurrentVideoSetting.Equals(setting.Display))
            //    {
            //        Debug.WriteLine(CurrentVideoSetting);
            //        var encodingProperties = (setting.property as StreamPropertiesHelper).EncodingProperties;
            //        mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);

            //    }
            //}
        }

        private async Task InitializeMediaCaptureAsync()
        {
            Debug.WriteLine("Init media capture");
            mediaCapture = new MediaCapture();
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = enabledAudio ? StreamingCaptureMode.AudioAndVideo : StreamingCaptureMode.Video,
                MediaCategory = MediaCategory.Other,
                AudioProcessing = AudioProcessing.Default
            };

            await mediaCapture.InitializeAsync(settings);
            if (isFlipping)
            {
                var videoEffectDefinition = new VideoEffectDefinition("CustomVideoEffects.Flip");
                IMediaExtension videoEffect = await mediaCapture.AddVideoEffectAsync(videoEffectDefinition, MediaStreamType.VideoPreview);
            }
            if (AvailableVideoSetting.Count() == 0)
            {
                PopulateStreamPropertiesUI(MediaStreamType.VideoPreview);
            }

            PreviewControl.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();
            isPreviewing = true;
            SetPreviewSettings(CurrentVideoSetting);
        }

        private async Task StartRecordingAsync()
        {

            recordedFile = await KnownFolders.VideosLibrary.CreateFileAsync("recordedVideo.mp4", CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), recordedFile);

            isRecording = true;
        }

        private async Task StopRecordingAsync()
        {
            await mediaCapture.StopRecordAsync();

            isRecording = false;

            var dialog = new ContentDialog
            {
                Title = "Recording Saved",
                Content = $"Video saved to {recordedFile.Path}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };

            await dialog.ShowAsync();
        }


        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                //if (isCapturingScreen)
                //{
                //    frameServerDest.StopRecording();
                //}
                //else
                //{
                    RecordButtonContent.Content = new Ellipse
                    {
                        Width = 30,
                        Height = 30,
                        Fill = new SolidColorBrush(Windows.UI.Colors.Red),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    await StopRecordingAsync();
                //}
            }
            else 
            //if (isCapturingScreen)
            //{
            //    await StartScreenRecordingAsync();
            //}
            //else
            //{
                RecordEllipse.Fill = new SolidColorBrush(Windows.UI.Colors.Gray);
                RecordButtonContent.Content = new Rectangle
                {
                    Width = 30,
                    Height = 30,
                    Fill = new SolidColorBrush(Windows.UI.Colors.Red),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                await StartRecordingAsync();
            //}
        }

        private async void FlipCamera_Click(object sender, RoutedEventArgs e)
        {
            if (!isPreviewing)
            {
                return;
            }
            isFlipping = !isFlipping;
            await InitializeMediaCaptureAsync();

            await Task.Delay(500);
        }

        private async void AudioToggleButton_Click(object sender, RoutedEventArgs e)
        {
            enabledAudio = !enabledAudio;
            if (enabledAudio)
            {
                AudioButtonContent.Content = new FontIcon
                {
                    Glyph = "\uF12E",  // Use Unicode escape sequence without the '&#x' prefix
                    FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),  // Specify FontFamily correctly
                    Foreground = new SolidColorBrush(Windows.UI.Colors.White),  // Use SolidColorBrush to set Foreground
                    FontSize = 24,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,  // Use HorizontalAlignment enum value
                };
            }
            else
            {
                AudioButtonContent.Content = new FontIcon
                {
                    Glyph = "\uE720",  
                    FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),  
                    Foreground = new SolidColorBrush(Windows.UI.Colors.White),  
                    FontSize = 24,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                };
            }

            await InitializeMediaCaptureAsync();
            await Task.Delay(500);
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> settings = new List<string>();

            foreach (VideoSetting videoSetting in AvailableVideoSetting)
            {
                settings.Add(videoSetting.Display);
            }
            string Param = JsonSerializer.Serialize(new NavigateData { AvailableResolution = settings, CurrentResolution = CurrentVideoSetting});

            Frame.Navigate(typeof(SettingsPage), Param);
        }

        private async void EditVideoButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the file picker to select a video file
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wmv");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Frame.Navigate(typeof(VideoEditingPage), file);
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "No file selected",
                    Content = "Operation cancelled.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };

                await dialog.ShowAsync();
            }
        }

        private async void ScreenCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (isCapturingScreen)
            {
                StopScreenCapture();
            }
            else
            {
                await StartScreenCaptureAsync();
            }
        }

        //private async Task StartScreenRecordingAsync()
        //{
        //    recordedFile = await KnownFolders.VideosLibrary.CreateFileAsync("screenRecording.mp4", CreationCollisionOption.GenerateUniqueName);

        //    var mediaComposition = new MediaComposition();
        //    var mediaStreamSource = await CreateMediaStreamSourceAsync();

        //    var mediaEncodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
        //    await mediaComposition.RenderToFileAsync(recordedFile, MediaTrimmingPreference.Precise, mediaEncodingProfile);
        //}

        //private async Task<MediaStreamSource> CreateMediaStreamSourceAsync()
        //{
        //    var frameServerDest = new FrameServerDest(canvasDevice);
        //    frameServerDest.StartServer(captureItem);

        //    var mediaStreamSource = frameServerDest.CreateMediaStreamSource();
        //    return mediaStreamSource;
        //}

        private async Task StartScreenCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            captureItem = await picker.PickSingleItemAsync();

            if (captureItem != null)
            {
                canvasDevice = CanvasDevice.GetSharedDevice();
                swapChain = new CanvasSwapChain(canvasDevice, captureItem.Size.Width, captureItem.Size.Height, 96);
                var compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(Window.Current.Compositor, canvasDevice);
                var compositionSurface = compositionGraphicsDevice.CreateDrawingSurface(
                    new Windows.Foundation.Size(captureItem.Size.Width, captureItem.Size.Height),
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

                var visual = ElementCompositionPreview.GetElementVisual(PreviewControl);
                var compositor = visual.Compositor;
                var surfaceBrush = compositor.CreateSurfaceBrush(compositionSurface);
                var spriteVisual = compositor.CreateSpriteVisual();
                spriteVisual.Brush = surfaceBrush;
                spriteVisual.Size = new System.Numerics.Vector2((float)captureItem.Size.Width, (float)captureItem.Size.Height);
                ElementCompositionPreview.SetElementChildVisual(PreviewControl, spriteVisual);

                framePool = Direct3D11CaptureFramePool.Create(
                    canvasDevice,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    captureItem.Size);
                framePool.FrameArrived += (s, a) =>
                {
                    using (var frame = framePool.TryGetNextFrame())
                    {
                        using (var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface))
                        using (var ds = CanvasComposition.CreateDrawingSession(compositionSurface))
                        {
                            ds.DrawImage(canvasBitmap);
                        }
                        swapChain.Present();
                    }
                };

                captureSession = framePool.CreateCaptureSession(captureItem);
                captureSession.StartCapture();

                isCapturingScreen = true;
            }
        }

        private void StopScreenCapture()
        {
            captureSession?.Dispose();
            framePool?.Dispose();
            canvasDevice = null;
            swapChain = null;
            captureItem = null;

            ElementCompositionPreview.SetElementChildVisual(PreviewControl, null);
            isCapturingScreen = false;

            InitializeMediaCaptureAsync();
        }

        private void CheckIfStreamsAreIdentical()
        {
            if (mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.AllStreamsIdentical ||
                mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.PreviewRecordStreamsIdentical)
            {
                Debug.WriteLine("Preview and video streams for this device are identical. Changing one will affect the other");
            }
        }

        private void PopulateStreamPropertiesUI(MediaStreamType streamType, bool showFrameRate = true)
        {
            Debug.WriteLine("Populate settings");
            // Query all properties of the specified stream type 
            IEnumerable<StreamPropertiesHelper> allStreamProperties =
            mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamPropertiesHelper(x));

            // Order them by resolution then frame rate
            allStreamProperties = allStreamProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

            // Populate the combo box with the entries
            foreach (var property in allStreamProperties)
            {
                AvailableVideoSetting.Add(
                    new VideoSetting { Display = property.GetFriendlyName(showFrameRate) , property = property}
                );
                //Debug.WriteLine(property.GetFriendlyName(showFrameRate));
                //localSettings.Add(property.GetFriendlyName(showFrameRate));
                //ComboBoxItem comboBoxItem = new ComboBoxItem();
                //comboBoxItem.Content = property.GetFriendlyName(showFrameRate);
                //comboBoxItem.Tag = property;
                //comboBox.Items.Add(comboBoxItem);
            }
            AvailableVideoSetting=AvailableVideoSetting.Distinct().ToList();
            Debug.WriteLine($"Current -> {CurrentVideoSetting}");
            if (CurrentVideoSetting == null)
            {
                Debug.WriteLine($"Set current {CurrentVideoSetting}");
                CurrentVideoSetting = AvailableVideoSetting[0].Display;
            }
        }

        private async void SetPreviewSettings(string propertyName)
        {
            Debug.WriteLine($"Set preview setting {propertyName}");
            if (isPreviewing)
            {
                foreach (VideoSetting setting in AvailableVideoSetting)
                {
                    Debug.WriteLine(setting.Display);
                    if (propertyName.Equals(setting.Display))
                    {
                        Debug.WriteLine(CurrentVideoSetting);
                        try
                        {
                            var encodingProperties = (setting.property as StreamPropertiesHelper).EncodingProperties;
                            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error: {ex.Message}");
                        }

                    }
                }
            }
        }

        private async void VideoSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                var encodingProperties = (selectedItem.Tag as StreamPropertiesHelper).EncodingProperties;
                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, encodingProperties);
            }
        }

        class VideoSetting
        {
            public string Display;
            public StreamPropertiesHelper property;
        }

    }
}

