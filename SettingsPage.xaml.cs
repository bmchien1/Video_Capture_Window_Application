using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.WebUI;
using Windows.UI.Xaml.Navigation;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace VideoRecordingCC
{
    public sealed partial class SettingsPage : Page
    {
        public event EventHandler SettingsSaved;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private ComboBoxItem FindComboBoxItemByContent(ComboBox comboBox, string content)
        {
            if (content == null) return null;
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == content)
                {
                    return item;
                }
            }
            return null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigateData myData = JsonSerializer.Deserialize<NavigateData>(e.Parameter.ToString());
            myData.AvailableResolution=myData.AvailableResolution.Distinct().ToList();
            foreach(var data in myData.AvailableResolution)
            {
                Debug.WriteLine(data);
                ComboBoxItem comboBoxItem=new ComboBoxItem();
                comboBoxItem.Content = data;
                SettingComboBox.Items.Add(comboBoxItem);
            }
            SettingComboBox.SelectedItem=FindComboBoxItemByContent(SettingComboBox,myData.CurrentResolution);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedResolution = ((ComboBoxItem)SettingComboBox.SelectedItem)?.Content.ToString();

            // Navigate back or show a confirmation message
            var dialog = new ContentDialog
            {
                Title = "Settings Saved",
                Content = "Your settings have been saved successfully.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
            };

            dialog.ShowAsync();
            Frame.Navigate(typeof(MainPage), selectedResolution);
        }

    }

    class NavigateData
    {
        public List<string> AvailableResolution {get; set;}
        public string CurrentResolution { get; set;}
    }
}
