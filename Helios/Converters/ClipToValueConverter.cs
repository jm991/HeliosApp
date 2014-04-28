using System;
using System.Diagnostics;
using Windows.Media.Editing;
using Windows.UI.Xaml.Data;

namespace Helios.Converters
{
    public class ClipToValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = ((string)parameter).ToLower();

            if (value != null && value is MediaClip)
            {
                MediaClip clipVal = value as MediaClip;

                switch (param)
                {
                    case "duration":
                        return clipVal.OriginalDuration.TotalMilliseconds;
                    case "start":
                        return clipVal.TrimTimeFromStart;
                    case "end":
                        return clipVal.TrimTimeFromEnd;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                Debug.WriteLine("Clip not loaded yet. Returning value of 0.");
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value != null && value is double)
            {
                return value;
            }
            else
            {
                Debug.WriteLine("Clip not loaded yet. Returning value of 0.");
                return 0;
            }
        }

        #endregion
    }
}