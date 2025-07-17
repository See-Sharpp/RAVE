using HandyControl.Tools.Extension;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using WpfApp2.Context;

namespace WpfApp2
{
    public partial class MainWindow : MetroWindow
    {

        private ApplicationDbContext _context = new ApplicationDbContext();
        string pass = "";
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainWindow_Loaded;
            AutoLogin();
        }

        private void showPassword(object sender, RoutedEventArgs e)
        {
            try
            {
                pass = password.Password;
                password.Visibility = Visibility.Hidden;
                passwordText.Visibility = Visibility.Visible;
                passwordText.Text = pass;
                eyeClosed.Visibility = Visibility.Hidden;
                eye.Visibility = Visibility.Visible;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }

        private void hidePassword(object sender, RoutedEventArgs e)
        {
            try
            {
                pass = passwordText.Text;
                password.Password = pass;
                password.Visibility = Visibility.Visible;
                passwordText.Visibility = Visibility.Hidden;
                eyeClosed.Visibility = Visibility.Visible;
                eye.Visibility = Visibility.Hidden;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }


        private void Login(object sender, RoutedEventArgs e)
        {
         
            string user = username.Text;
            bool? remember = rememberMe.IsChecked;

         

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {

                System.Windows.MessageBox.Show("Username and Password cannot be empty.");
                return;
            }

        
            var User = _context.SignUpDetails.FirstOrDefault(u => (u.Username == user || u.Email == user));

            if (User == null)
            {
                System.Windows.MessageBox.Show("User not found.");
                return;
            }


            if (User!=null)
            {
                if(!PasswordHelper.VerifyPassword(pass, User.Password))
                {
                    System.Windows.MessageBox.Show("Invalid Password. Please try again.");
                    return;
                }
                Global.UserId = User?.Id;

                if (remember == true)
                {
                    Properties.Settings.Default.UserId = User.Id;
                    Properties.Settings.Default.UserName = User.Username;
                    Properties.Settings.Default.RememberMe = true;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Properties.Settings.Default.UserId = User.Id;
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.Save();

                }
                Navbar dashboard = new Navbar();
                Properties.Settings.Default.VerificationPass = User.Password;
                System.Windows.MessageBox.Show(User.Password);
                Global.logout = false;
                dashboard.Show();
                this.Close();
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
        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            pass = password.Password;
        }

        private void passwordText_TextChanged(object sender, TextChangedEventArgs e)
        {
            pass = passwordText.Text;
        }


        private void AutoLogin()
        {
            bool userExists = _context.SignUpDetails.Any(u => u.Id == Properties.Settings.Default.UserId);

            if (Properties.Settings.Default.RememberMe && Properties.Settings.Default.UserId != 0 && userExists)
            {
                Global.UserId = Properties.Settings.Default.UserId;


                Navbar dashboard = new Navbar();
                dashboard.Show();
                dashboard.Show();
                this.Hide();         // Instantly hides current window

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.Close(); 
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }



    }
}
