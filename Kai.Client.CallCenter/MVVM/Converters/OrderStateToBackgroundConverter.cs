using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Kai.Client.CallCenter.MVVM.Converters;

public class OrderStateToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string orderState)
        {
            return orderState switch
            {
                "접수" => (Brush)Application.Current.Resources["AppBrushLightReceipt"],
                "대기" => (Brush)Application.Current.Resources["AppBrushLightWait"],
                "배차" => (Brush)Application.Current.Resources["AppBrushLightAlloc"],
                "예약" => (Brush)Application.Current.Resources["AppBrushLightReserve"],
                "운행" => (Brush)Application.Current.Resources["AppBrushLightRun"],
                "완료" => (Brush)Application.Current.Resources["AppBrushLightFinish"],
                "취소" => (Brush)Application.Current.Resources["AppBrushLightCancel"],
                _ => (Brush)Application.Current.Resources["AppBrushLightTotal"],
            };
        }
        return (Brush)Application.Current.Resources["AppBrushLightReceipt"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
