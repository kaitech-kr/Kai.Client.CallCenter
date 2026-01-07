using System.Globalization;
using System.Windows.Data;
using Kai.Common.StdDll_Common;

namespace Kai.Client.CallCenter.MVVM.Converters;

public class WonFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return StdConvert.IntToStringWonFormat(intValue);
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
            return StdConvert.StringWonFormatToInt(strValue);
        return 0;
    }
}
