using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace VideoRecordingCC
{
    public sealed partial class VideoEditingPage : Page
    {
        private MediaComposition _composition;
        private MediaPlayer _mediaPlayer;
        private DispatcherTimer _timer;

        public VideoEditingPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFile videoFile)
            {
                await LoadVideoAsync(videoFile);
            }
        }

        private async Task LoadVideoAsync(StorageFile videoFile)
        {
            _composition = new MediaComposition();
            MediaClip videoClip = await MediaClip.CreateFromFileAsync(videoFile);
            _composition.Clips.Add(videoClip);

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Source = MediaSource.CreateFromMediaStreamSource(_composition.GenerateMediaStreamSource());
            PreviewControl.SetMediaPlayer(_mediaPlayer);
            _mediaPlayer.Play();

            // Initialize the slider
            SeekBar.Minimum = 0;
            SeekBar.Maximum = videoClip.OriginalDuration.TotalSeconds;

            // Set up the timer to update the slider
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }


        private void Timer_Tick(object sender, object e)
        {
            if (_mediaPlayer.PlaybackSession != null)
            {
                SeekBar.Value = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
            }
        }

        private void SeekBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_mediaPlayer.PlaybackSession != null && Math.Abs(e.NewValue - e.OldValue) > 1)
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void TrimButton_Click(object sender, RoutedEventArgs e)
        {
            if (_composition.Clips.Count > 0)
            {
                _composition.Clips[0].TrimTimeFromStart = TimeSpan.FromSeconds(2);
                _composition.Clips[0].TrimTimeFromEnd = TimeSpan.FromSeconds(2);
                _mediaPlayer.Source = MediaSource.CreateFromMediaStreamSource(_composition.GenerateMediaStreamSource());
            }
        }

        private void AdjustSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.PlaybackSession != null)
            {
                _mediaPlayer.PlaybackSession.PlaybackRate = 2.0;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Pause();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Play();
        }

        private void PlayFromStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.PlaybackSession != null)
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Create the file in the Videos library with a unique name
            StorageFile resultFile = await KnownFolders.VideosLibrary.CreateFileAsync("editedVideo.mp4", CreationCollisionOption.GenerateUniqueName);

            // Create a MediaEncodingProfile for MP4 format
            //MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);

            // Render the composition to the file

            IAsyncOperationWithProgress<TranscodeFailureReason, double> saveOperation;

            saveOperation =  _composition.RenderToFileAsync(resultFile);

            //saveOperation.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>();
            //.Debug.WriteLine($"Render {result.ToString()}");

            // Show a dialog to inform the user that the video has been saved
            var dialog = new ContentDialog
            {
                Title = "Media Composition Saved",
                Content = $"Video saved to {resultFile.Path}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };

            await dialog.ShowAsync();

            // Navigate back to the main page
            Frame.Navigate(typeof(MainPage));
        }
    }

}
