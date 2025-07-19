using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfApp2
{
    public partial class SpeakerVerificationPass : Window
    {
        public string EnteredPassword { get; private set; }
        public bool IsPasswordCorrect { get; private set; }

        private string correctPassword { get; set; }
        private int attemptCount = 0;
        private const int maxAttempts = 3;

        public SpeakerVerificationPass()
        {
            InitializeComponent();
            this.Loaded += OnWindowLoaded;

            correctPassword = Properties.Settings.Default.VerificationPass;
        }

        public SpeakerVerificationPass(string password) : this()
        {
            correctPassword = password;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
           
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PasswordInput.Focus();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            VerifyPassword();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsPasswordCorrect = false;
            DialogResult = false;
            AnimateClose();
        }

        private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyPassword();
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }

          
            if (ErrorMessage.Visibility == Visibility.Visible)
            {
                HideError();
            }
        }

        private bool VerifyPassword()
        {
            EnteredPassword = PasswordInput.Password;

           

            if (string.IsNullOrEmpty(EnteredPassword))
            {
                ShowError("Please enter a password.");
                return false;
            }

            attemptCount++;

            if (PasswordHelper.VerifyPassword(EnteredPassword,correctPassword))
            {
                IsPasswordCorrect = true;
                DialogResult = true;
                AnimateSuccess();
                return true;
            }
            else
            {
                IsPasswordCorrect = false;

                if (attemptCount >= maxAttempts)
                {
                    ShowError($"Maximum attempts ({maxAttempts}) exceeded. Access denied.");

                 
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        DialogResult = false;
                        AnimateClose();
                    };
                    timer.Start();
                }
                else
                {
                    int remainingAttempts = maxAttempts - attemptCount;
                    ShowError($"Invalid password. {remainingAttempts} attempt(s) remaining.");

                 
                    AnimateShake();

                    PasswordInput.Clear();
                    PasswordInput.Focus();
                }
                return false;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;

         
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            ErrorMessage.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void HideError()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => ErrorMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void AnimateShake()
        {
            var shakeAnimation = new DoubleAnimationUsingKeyFrames();
            shakeAnimation.Duration = TimeSpan.FromMilliseconds(500);

            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));

            var transform = new TranslateTransform();
            DialogCard.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
        }

        private void AnimateSuccess()
        {
           
            var glowAnimation = new DoubleAnimation(1, 0.7, TimeSpan.FromMilliseconds(200));
            glowAnimation.AutoReverse = true;
            glowAnimation.RepeatBehavior = new RepeatBehavior(2);
            glowAnimation.Completed += (s, e) => Close();

            this.BeginAnimation(OpacityProperty, glowAnimation);
        }

        private void AnimateClose()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            var scaleOut = new DoubleAnimation(1, 0.8, TimeSpan.FromMilliseconds(250));

            fadeOut.Completed += (s, e) => Close();

            this.BeginAnimation(OpacityProperty, fadeOut);
            DialogCard.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            DialogCard.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }
}