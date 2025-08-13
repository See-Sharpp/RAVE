using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp2
{
    public class LastItemConverter : IMultiValueConverter
    {
        public static LastItemConverter Instance { get; } = new LastItemConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 2 ||
                    !(values[0] is FrameworkElement container) ||
                    !(values[1] is IList list))
                {
                    return false;
                }

                object currentItem = container.DataContext;

                if (currentItem != null && list.Count > 0)
                {
                    return list.IndexOf(currentItem) == list.Count - 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[LastItemConverter Error]: {ex.Message}");
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
           
            throw new NotImplementedException();
        }
    }
}