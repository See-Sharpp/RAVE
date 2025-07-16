using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using System.Collections; // Required for IList

namespace WpfApp2
{
    public class LastItemConverter : IMultiValueConverter
    {
        public static LastItemConverter Instance { get; } = new LastItemConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // --- REVISED LOGIC ---
            // The previous logic used the ItemContainerGenerator, which can be unreliable due to timing.
            // This new logic works directly with the data, which is much more robust.

            // values[0] is the ContentPresenter (the item's container)
            // values[1] is the ItemsSource collection from the ItemsControl

            if (values.Length < 2 ||
                !(values[0] is FrameworkElement container) || // We need the container to get the DataContext
                !(values[1] is IList list)) // We cast to IList to get the Count and use IndexOf
            {
                return false;
            }

            // Get the data object for the current row (e.g., your command history object)
            object currentItem = container.DataContext;

            if (currentItem != null && list.Count > 0)
            {
                // Check if the current data item is the last one in the source collection.
                // This is more reliable than checking the visual container's index.
                return list.IndexOf(currentItem) == list.Count - 1;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
