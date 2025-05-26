using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    /// <summary>
    /// Interaction logic for SignUp.xaml
    /// </summary>
    public partial class SignUp : MetroWindow
    {
        private ApplicationDbContext _context = new ApplicationDbContext();
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

        private async void Sign_Up(object sender, RoutedEventArgs e)
        {
            SignUpDetail signUpDetail = new SignUpDetail
            {
                Username = username.Text,
                Email = email.Text,
                Password = password.Password.ToString()
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

    }
}
