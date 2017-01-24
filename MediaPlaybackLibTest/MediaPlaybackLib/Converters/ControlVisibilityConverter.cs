using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using System.Windows.Markup;
using System.Windows.Data;

namespace MediaPlaybackLib.Converters
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class ControlVisibilityConverter : IValueConverter
    {
        public ControlVisibilityConverter()
        {
            this.DefaultValue = Visibility.Collapsed;
        }

        public Visibility DefaultValue { get; set; }

        public bool Negate { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility defaultValue = this.Negate ? Visibility.Visible : this.DefaultValue;
            Visibility successResult = this.Negate ? this.DefaultValue : Visibility.Visible;

            if (value == null)
            {
                return defaultValue;
            }

            if (value is bool)
            {
                if ((bool)value)
                {
                    return successResult;
                }

                return defaultValue;
            }

            if (value is string && string.IsNullOrEmpty((string)value))
            {
                return defaultValue;
            }

            if (value is IEnumerable)
            {
                var enumerator = ((IEnumerable)value).GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return defaultValue;
                }

                if (enumerator.Current == null)
                {
                    return defaultValue;
                }

                return successResult;
            }
            
            if (value is DateTime && ((DateTime)value == DateTime.MinValue))
            {
                return defaultValue;
            }

            return successResult;
        }

        [ExcludeFromCodeCoverage]
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion
    }

    namespace Markup
    {
        [ExcludeFromCodeCoverage] // Convenience class for Xaml
        [MarkupExtensionReturnType(typeof(ControlVisibilityConverter))]
        public class ControlVisibilityConverterExtension : MarkupExtension
        {
            public ControlVisibilityConverterExtension()
            {
                this.DefaultValue = Visibility.Collapsed;
            }

            public Visibility DefaultValue { get; set; }

            public bool Negate { get; set; }

            public override object ProvideValue(IServiceProvider serviceProvider)
            {
                return new ControlVisibilityConverter() { DefaultValue = this.DefaultValue, Negate = this.Negate };
            }
        }
    }
}
