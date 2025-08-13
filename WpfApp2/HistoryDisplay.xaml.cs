using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class HistoryDisplay : Page
    {
        public HistoryDisplay()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
             
            }
        }

        public void LoadHistoryData(Queue<LLM_Detail> historyData, string categoryName, string categoryIcon)
        {
            try
            {
                if (historyData == null)
                {
                    throw new ArgumentNullException(nameof(historyData), "The provided history data was null.");
                }

                CategoryTitle.Text = $"{categoryName} History";
                CategoryIcon.Text = categoryIcon;
                CategorySubtitle.Text = $"Last {historyData.Count} executed commands";

                HistoryItemsControl.ItemsSource = null;

                if (historyData.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    EmptyState.Visibility = Visibility.Collapsed;
                    HistoryItemsControl.ItemsSource = new List<LLM_Detail>(historyData);
                }
            }
            catch (Exception ex)
            {
               
                EmptyState.Visibility = Visibility.Visible;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService?.CanGoBack == true)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
              
            }
        }
    }
}