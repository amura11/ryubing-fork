using Avalonia.Data.Converters;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Configuration.Hid;
using System;
using System.Globalization;
using System.Linq;

namespace Ryujinx.Ava.UI.Helpers
{
    internal class GamepadCombinationConverter : IValueConverter
    {
        public static readonly GamepadCombinationConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not GamepadCombination combo || combo.IsUnbound)
            {
                return LocaleManager.Instance[LocaleKeys.KeyboardLayout_KeyUnbound];
            }

            return string.Join(" + ", combo.Buttons.Select(b => KeyValueConverter.Instance.Convert(b, targetType, parameter, culture)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
