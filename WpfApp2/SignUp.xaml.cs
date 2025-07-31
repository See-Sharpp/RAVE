using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp2.Context;
using WpfApp2.Models;

namespace WpfApp2
{ 
    public partial class SignUp : MetroWindow
    {
        private ApplicationDbContext _context = new ApplicationDbContext();
        string pass = "";
        public SignUp()
        {
            InitializeComponent();
        }

        private void login_page(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Global.SignUp = false;
            this.Close();
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }


        }

        private async void Sign_Up(object sender, RoutedEventArgs e)
        {

            
            if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(email.Text)|| string.IsNullOrEmpty(pass))
            {
                System.Windows.MessageBox.Show("Please fill in all fields.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingUser = _context.SignUpDetails.FirstOrDefault(u => u.Username == username.Text || u.Email == email.Text);
            if(existingUser != null)
            {
                System.Windows.MessageBox.Show("Username or Email already exists. Please choose a different one.", "Signup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SignUpDetail signUpDetail = new SignUpDetail
            {
                Username = username.Text,
                Email = email.Text,
                Password = PasswordHelper.HashPassword(pass)
            };

            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new ValidationContext(signUpDetail);

            bool isValid = Validator.TryValidateObject(signUpDetail, context, validationResults, true);

            if (!isValid)
            {
             
                string errorMessages = string.Join("\n", validationResults.Select(vr => "• " + vr.ErrorMessage));
                System.Windows.MessageBox.Show(errorMessages, "Signup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ans = System.Windows.MessageBox.Show("Sign Up Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            if (ans == MessageBoxResult.OK)
            {
                _context.SignUpDetails.Add(signUpDetail);
                await _context.SaveChangesAsync();

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                Global.SignUp = false;
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Global.SignUp)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
        private void passwordText_TextChanged(object sender, TextChangedEventArgs e)
        {
            pass = passwordText.Text;
        }
        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            pass = password.Password;
        }


    }
}
