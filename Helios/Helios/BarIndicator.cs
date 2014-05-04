using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Helios
{
    public abstract class BarIndicator : Indicator
    {
        #region Dependency properties

        public int BarThickness
        {
            get { return (int)GetValue(BarThicknessProperty); }
            set { SetValue(BarThicknessProperty, value); }
        }
        public static readonly DependencyProperty BarThicknessProperty =
            DependencyProperty.Register("BarThickness", typeof(int), typeof(BarIndicator), new PropertyMetadata(3, BarThicknessPropertyChanged));

        public Brush BarBrush
        {
            get { return (Brush)GetValue(BarBrushProperty); }
            set { SetValue(BarBrushProperty, value); }
        }
        public static readonly DependencyProperty BarBrushProperty =
            DependencyProperty.Register("BarBrush", typeof(Brush), typeof(BarIndicator), new PropertyMetadata(new SolidColorBrush(Colors.White), BarBrushPropertyChanged));

        #endregion


        #region dp handlers

        private static void BarThicknessPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            BarIndicator ind = o as BarIndicator;
            if (ind != null)
            {
                ind.OnBarThicknesChanged((int)e.NewValue, (int)e.OldValue);
            }
        }

        private static void BarBrushPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) { }

        #endregion


        #region Abstract methods

        protected virtual void OnBarThicknesChanged(int newVal, int oldVal) { }

        #endregion
    }
}
