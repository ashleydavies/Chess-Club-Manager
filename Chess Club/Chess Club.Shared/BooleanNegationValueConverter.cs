using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Chess_Club {
    public class BooleanNegationValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null) return null;
            if (parameter == null) return value;
            return !((Boolean)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return Convert(value, targetType, parameter, language);
        }
    }
}
