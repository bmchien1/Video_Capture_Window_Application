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

namespace VideoRecordingCC
{
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private bool isFlipping = false;
        private bool isPreviewing;
        private bool isRecording = false;
        //private bool isPaused;
        private bool enabledAudio = false;
        private StorageFile recordedFile;
        private List<VideoSetting> AvailableVideoSetting = new List<VideoSetting>();
        private string CurrentVideoSetting = null;

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
            RecordButton.Content = "Stop Recording";
        }

        private async Task StopRecordingAsync()
        {
            await mediaCapture.StopRecordAsync();

            isRecording = false;
            RecordButton.Content = "Start Recording";

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
                await StopRecordingAsync();
            }
            else
            {
                await StartRecordingAsync();
            }

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
            ToggleButton toggleButton = (ToggleButton)sender;
            enabledAudio = toggleButton.IsChecked ?? false;

            AudioToggleButton.Content = enabledAudio ? "Disable Audio" : "Enable Audio";

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

        private async void ComposeButton_Click(object sender, RoutedEventArgs e)
        {
            await ComposeAndEditMediaAsync();
        }

        private async Task ComposeAndEditMediaAsync()
        {
            // Create a new MediaComposition
            MediaComposition composition = new MediaComposition();

            // Add a video clip
            StorageFile videoFile = await KnownFolders.VideosLibrary.GetFileAsync("sample.mp4");
            MediaClip videoClip = await MediaClip.CreateFromFileAsync(videoFile);
            composition.Clips.Add(videoClip);

            // Add background music
            StorageFile audioFile = await KnownFolders.MusicLibrary.GetFileAsync("background.mp3");
            MediaClip audioClip = await MediaClip.CreateFromFileAsync(audioFile);
            audioClip.Volume = 0.5; // Set the volume of the background music
            composition.Clips.Add(audioClip);

            // Save the composition to a file
            StorageFile resultFile = await KnownFolders.VideosLibrary.CreateFileAsync("editedVideo.mp4", CreationCollisionOption.GenerateUniqueName);
            await composition.RenderToFileAsync(resultFile, MediaTrimmingPreference.Precise);

            // Notify user
            var dialog = new ContentDialog
            {
                Title = "Media Composition Saved",
                Content = $"Video saved to {resultFile.Path}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };

            await dialog.ShowAsync();
        }

        //private async void SettingsPage_SettingsSaved(object sender, EventArgs e)
        //{
        //    // Re-initialize media capture with the updated settings
        //    await InitializeMediaCaptureAsync();
        //}

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

