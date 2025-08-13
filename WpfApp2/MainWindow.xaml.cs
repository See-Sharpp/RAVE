using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp2.Context;

namespace WpfApp2
{
    public partial class MainWindow : MetroWindow
    {
        private ApplicationDbContext _context;
        private string pass = "";

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _context = new ApplicationDbContext();
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
              
                Application.Current.Shutdown();
            }
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
            catch (Exception ex)
            {
               
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
            catch (Exception ex)
            {
            
            }
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            try
            {
                string user = username.Text;
                bool? remember = rememberMe.IsChecked;

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    MessageBox.Show("Username and Password cannot be empty.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var User = _context.SignUpDetails.FirstOrDefault(u => u.Username == user || u.Email == user);

                if (User == null)
                {
                    MessageBox.Show("User not found.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!PasswordHelper.VerifyPassword(pass, User.Password))
                {
                    MessageBox.Show("Invalid password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Global.UserId = User.Id;
                Properties.Settings.Default.VerificationPass = User.Password;
                Properties.Settings.Default.RememberMe = remember == true;
                if (remember == true)
                {
                    Properties.Settings.Default.UserId = User.Id;
                    Properties.Settings.Default.UserName = User.Username;
                }
                Properties.Settings.Default.Save();

                Navbar dashboard = new Navbar();
                Global.logout = false;
                dashboard.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                
            }
        }

        private void Sign_Up(object sender, RoutedEventArgs e)
        {
            try
            {
                SignUp signUpWindow = new SignUp();
                signUpWindow.Show();
                Global.SignUp = true;
                this.Close();
            }
            catch (Exception ex)
            {
                
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
          
            base.OnClosing(e);
            if (Global.UserId == null && !Global.SignUp)
            {
                Application.Current.Shutdown();
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
            try
            {
                if (Properties.Settings.Default.RememberMe && Properties.Settings.Default.UserId != 0)
                {
                    bool userExists = _context.SignUpDetails.Any(u => u.Id == Properties.Settings.Default.UserId);
                    if (userExists)
                    {
                        Global.UserId = Properties.Settings.Default.UserId;
                        Navbar dashboard = new Navbar();
                        dashboard.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
              
                Properties.Settings.Default.RememberMe = false;
                Properties.Settings.Default.Save();
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // To be implemented
        }
    }
}