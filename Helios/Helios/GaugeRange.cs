using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Helios
{
    public class GaugeRange : INotifyPropertyChanged
    {
        #region Fields (private)

        private double offset;
        private Color color;

        #endregion


        #region Properties (public)

        public double Offset
        {
            get { return offset; }
            set
            {
                if (offset != value)
                {
                    offset = value;
                    OnNotifyPropertyChanged("Offset");
                }
            }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                if (color != value)
                {
                    color = value;
                    OnNotifyPropertyChanged("Color");
                }
            }
        }

        #endregion


        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnNotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
