using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class History : Page
    {
        public History()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize the History page.\n\nDetails: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SystemControl_Click(object sender, RoutedEventArgs e)
        {
            var historyData = Global.system_control;
            NavigateToHistoryDisplay(historyData, "System Control", "⚙️");
        }

        private void WebBrowse_Click(object sender, RoutedEventArgs e)
        {
            var historyData = Global.web_browse;
            NavigateToHistoryDisplay(historyData, "Web Browse", "🌐");
        }

        private void ApplicationControl_Click(object sender, RoutedEventArgs e)
        {
            var historyData = Global.application_control;
            NavigateToHistoryDisplay(historyData, "Application Control", "🖥️");
        }

        private void FileOperation_Click(object sender, RoutedEventArgs e)
        {
            var historyData = Global.file_operation;
            NavigateToHistoryDisplay(historyData, "File Operation", "📁");
        }

        private void NavigateToHistoryDisplay(Queue<LLM_Detail> historyData, string categoryName, string categoryIcon)
        {
            try
            {
                if (NavigationService == null)
                {
                    throw new InvalidOperationException("The page cannot navigate because it is not hosted in a navigation container.");
                }

                var historyDisplayPage = new HistoryDisplay();
                historyDisplayPage.LoadHistoryData(historyData, categoryName, categoryIcon);
                NavigationService.Navigate(historyDisplayPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to display the history for '{categoryName}'.\n\nDetails: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}