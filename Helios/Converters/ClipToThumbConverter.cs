using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Diagnostics;
using Windows.Media.Editing;
using Windows.UI.Xaml.Data;

namespace Helios.Converters
{
    public class ClipToThumbConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string param = ((string)parameter).ToLower();

            if (value != null && value is IDictionary<string, string> && param.Equals("thumb"))
            {
                IDictionary<string, string> dict = value as IDictionary<string, string>;
                string imageUri = "";
                dict.TryGetValue("thumb", out imageUri);
                return new Uri(imageUri);
            }
            else
            {
                Debug.WriteLine("Clip not loaded yet. Returning value of 0.");
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
