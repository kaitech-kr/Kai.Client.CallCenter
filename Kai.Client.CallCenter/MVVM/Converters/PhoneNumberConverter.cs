using System.Globalization;
using System.Windows.Data;
using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.MVVM.Converters;

public class PhoneNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
            return StdConvert.ToPhoneNumberFormat(strValue);
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
            return StdConvert.MakePhoneNumberToDigit(strValue);
        return "";
    }
}
