using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallFlowCore.Converters
{
    public class SpeedSliderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intVal = 0;

            Int32.TryParse(value.ToString(), out intVal);

            if (intVal >= 1000)
                return $"{intVal / 1000} sec.";
            else
                return $"{intVal} msec.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
