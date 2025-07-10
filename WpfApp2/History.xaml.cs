using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using WpfApp2.Models;

namespace WpfApp2
{
    public partial class History : Page
    {
        public History()
        {
            InitializeComponent();
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
            var historyDisplayPage = new HistoryDisplay();
            historyDisplayPage.LoadHistoryData(historyData, categoryName, categoryIcon);
            NavigationService.Navigate(historyDisplayPage);
        }
    }
}
