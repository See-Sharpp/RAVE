using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows;
using WpfApp2.Context;

namespace WpfApp2
{
    public partial class MainWindow : MetroWindow
    {

        private ApplicationDbContext _context = new ApplicationDbContext();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Login(object sender, RoutedEventArgs e)
        {
            string user = username.Text;
            string pass = password.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Username and Password cannot be empty.");
                return;
            }

            
            var User = _context.SignUpDetails.FirstOrDefault(u => u.Username == user && u.Password==pass);
            var User2 = _context.SignUpDetails.FirstOrDefault(u => u.Email == user && u.Password == pass);

            if (User!=null)
            {
                Global.UserId = User?.Id;
                MessageBox.Show($"Logging in as {user}");
                Navbar dashboard = new Navbar();
                dashboard.Show();
                this.Close();
                return;
            }

            if (User2!=null)
            {
                Global.UserId = User?.Id;
                MessageBox.Show($"Logging in as {user}");
                Navbar dashboard = new Navbar();
                dashboard.Show();
                this.Close();
                return;
            }

            if (_context.SignUpDetails.Any(u => u.Username == user && u.Password == pass) == false)
            {
                MessageBox.Show("Invalid Username or Password.");
                return;
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Redirecting to password recovery...");
        }

        private void Sign_Up(object sender, RoutedEventArgs e)
        {
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
            this.Close();
        }

    }
}
