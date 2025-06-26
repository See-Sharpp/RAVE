using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace WpfApp2
{
    public partial class Settings : Page
    {
        private bool isWakeWordEnabled = false;
        private bool isAutoListenEnabled = false;
        private bool isDarkModeEnabled = true;
        private bool areNotificationsEnabled = true;
        private bool isSaveHistoryEnabled = true;
        private bool isAnalyticsEnabled = false;

        public Settings()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {

            WakeWordToggle.IsChecked = isWakeWordEnabled;
            AutoListenToggle.IsChecked = isAutoListenEnabled;
            DarkModeToggle.IsChecked = isDarkModeEnabled;
            SaveHistoryToggle.IsChecked = isSaveHistoryEnabled;
            AnalyticsToggle.IsChecked = isAnalyticsEnabled;
        }

        private void SaveSettings()
        {
            
        }

        
        private void WakeWordToggle_Checked(object sender, RoutedEventArgs e)
        {
            isWakeWordEnabled = true;
            SaveSettings();
            
        }

        private void WakeWordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isWakeWordEnabled = false;
            SaveSettings();
            
        }

        
        private void AutoListenToggle_Checked(object sender, RoutedEventArgs e)
        {
            isAutoListenEnabled = true;
            SaveSettings();
        }

        private void AutoListenToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isAutoListenEnabled = false;
            SaveSettings();
        }

       
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            isDarkModeEnabled = true;
            SaveSettings();
            
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isDarkModeEnabled = false;
            SaveSettings();
            
        }

        
        private void NotificationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            areNotificationsEnabled = true;
            SaveSettings();
        }

        private void NotificationsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            areNotificationsEnabled = false;
            SaveSettings();
        }

        
        private void SaveHistoryToggle_Checked(object sender, RoutedEventArgs e)
        {
            isSaveHistoryEnabled = true;
            SaveSettings();
        }

        private void SaveHistoryToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isSaveHistoryEnabled = false;
            SaveSettings();
        }

        
        private void AnalyticsToggle_Checked(object sender, RoutedEventArgs e)
        {
            isAnalyticsEnabled = true;
            SaveSettings();
        }

        private void AnalyticsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isAnalyticsEnabled = false;
            SaveSettings();
        }

        
        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SensitivityValue != null)
            {
                SensitivityValue.Text = $"{(int)e.NewValue}%";
                SaveSettings();
            }
        }

        public bool IsWakeWordEnabled => isWakeWordEnabled;
        public bool IsAutoListenEnabled => isAutoListenEnabled;
        public bool IsDarkModeEnabled => isDarkModeEnabled;
        public bool AreNotificationsEnabled => areNotificationsEnabled;
        public bool IsSaveHistoryEnabled => isSaveHistoryEnabled;
        public bool IsAnalyticsEnabled => isAnalyticsEnabled;
        public double VoiceSensitivity => SensitivitySlider?.Value ?? 50;
    }
}