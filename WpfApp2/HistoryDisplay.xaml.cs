using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class HistoryDisplay : Page
    {
        public HistoryDisplay()
        {
            InitializeComponent();
        }
        public void LoadHistoryData(Queue<LLM_Detail> historyData, string categoryName, string categoryIcon)
        {
            CategoryTitle.Text = $"{categoryName} History";
            CategorySubtitle.Text = $"Last {historyData.Count} executed commands";
            CategoryIcon.Text = categoryIcon;

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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}