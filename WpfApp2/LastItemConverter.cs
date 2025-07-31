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
                // Converters should never throw exceptions, as it can crash the application.
                // Instead, log the error for debugging purposes and return a safe default value.
                Trace.WriteLine($"[LastItemConverter Error]: {ex.Message}");
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // This method is not used, so it correctly throws an exception.
            throw new NotImplementedException();
        }
    }
}