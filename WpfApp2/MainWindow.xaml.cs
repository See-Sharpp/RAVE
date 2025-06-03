using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
                System.Windows.MessageBox.Show("Username and Password cannot be empty.");
                return;
            }

            
            var User = _context.SignUpDetails.FirstOrDefault(u => u.Username == user && u.Password==pass);
            var User2 = _context.SignUpDetails.FirstOrDefault(u => u.Email == user && u.Password == pass);

            if (User!=null)
            {
                Global.UserId = User?.Id;
           
                Navbar dashboard = new Navbar();
                dashboard.Show();
                this.Close();
                return;
            }

            if (User2!=null)
            {
                Global.UserId = User2?.Id;
             
           
                Navbar dashboard = new Navbar();
                dashboard.Show();
                this.Close();
                return;
            }

            if (_context.SignUpDetails.Any(u => u.Username == user && u.Password == pass) == false)
            {
                System.Windows.MessageBox.Show("Invalid Username or Password.");
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
            Global.SignUp = true;
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(Global.UserId == null && !Global.SignUp)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

     

    }
}
