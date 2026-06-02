using System;
using System.Globalization;
using System.Windows.Data;

namespace ServiceDeskDesktop.Converters
{
    public class BlockedToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isBlocked)
                return isBlocked ? "Заблокирован" : "Активен";
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}