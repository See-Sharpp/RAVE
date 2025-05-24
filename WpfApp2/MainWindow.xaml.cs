using MahApps.Metro.Controls;
using System.Windows;

namespace WpfApp2
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            
        }
        private void Login(object sender, RoutedEventArgs e)
        {
            string user = username.Text;
            string pass = password.Password;

            // Authenticate logic here
            MessageBox.Show($"Logging in as {user}");

            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Redirecting to password recovery...");
        }

    }
}
