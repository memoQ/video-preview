using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MemoQ.VideoPreview
{
    class MultiVisibilityToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values,
                                Type targetType,
                                object parameter,
                                System.Globalization.CultureInfo culture)
        {
            foreach (object value in values)
                if (value is System.Windows.Visibility && (System.Windows.Visibility)value != System.Windows.Visibility.Visible)
                    return System.Windows.Visibility.Collapsed;
            
            return System.Windows.Visibility.Visible;
        }

        public object[] ConvertBack(object value,
                                    Type[] targetTypes,
                                    object parameter,
                                    System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
