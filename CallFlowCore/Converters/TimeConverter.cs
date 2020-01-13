using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallFlowCore.Converters
{
    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int sec = 0;

            if (Int32.TryParse(value.ToString(), out sec))
            {

                TimeSpan timeSpan = TimeSpan.FromSeconds(sec);

                return timeSpan.ToString(@"hh\:mm\:ss");
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
