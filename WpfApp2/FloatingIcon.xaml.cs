using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp2
{
    public partial class FloatingIcon : Window
    {
        private Point _startPoint;
        private bool _isDragging = false;
        public FloatingIcon()
        {
            InitializeComponent();

            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;

            double right = 10;
            double bottom = 50;

            this.Left = screenWidth-this.Width-right;
            this.Top = screenHeight - this.Height - bottom;
          
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isDragging = false;

            (sender as UIElement)?.CaptureMouse();
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                if (!_isDragging && (Math.Abs(currentPoint.X - _startPoint.X) > 4 || Math.Abs(currentPoint.Y - _startPoint.Y) > 4))
                {
                    _isDragging = true;
                    DragMove();
                }
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement)?.ReleaseMouseCapture();

            if (!_isDragging)
            {
                OnMicClick();
            }

            _isDragging = false;
        }

        private void OnMicClick()
        {
            const double durationSeconds = 5;
            const double radius = 36;
            Point center = new Point(40, 40); 

           
            var figure = new PathFigure { StartPoint = new Point(center.X + radius, center.Y) };
            var arcSegment = new ArcSegment
            {
                Size = new Size(radius, radius),
                Point = figure.StartPoint,
                IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise
            };

            figure.Segments.Add(arcSegment);
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            RecordingArc.Data = geometry;
            RecordingArc.Visibility = Visibility.Visible;

          
            DateTime startTime = DateTime.Now;
            EventHandler handler = null;

            handler = (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                var progress = Math.Min(elapsed / durationSeconds, 1);
                var angle = 360 * progress;
                var radians = angle * Math.PI / 180;

               
                Point endPoint = new Point(
                    center.X + radius * Math.Cos(radians),
                    center.Y + radius * Math.Sin(radians)
                );

                arcSegment.Point = endPoint;
                arcSegment.IsLargeArc = angle > 180;

                if (progress >= 1)
                {
                    CompositionTarget.Rendering -= handler;
                    RecordingArc.Visibility = Visibility.Collapsed;
                }
            };

            CompositionTarget.Rendering += handler;
        }

    }
}
