using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Helios
{
    public abstract class Indicator : Control
    {
        public Indicator()
        {
            this.DefaultStyleKey = typeof(Indicator);
        }

        #region Fields (private)

        private Scale owner;

        #endregion

        #region Properties (public)

        public Scale Owner
        {
            get
            {
                return this.owner;
            }
            internal set
            {
                if (this.owner != value)
                {
                    this.owner = value;
                    UpdateIndicator(owner);
                }
            }
        }

        #endregion

        #region Dependency properties

        public double Value
        {
            get 
            { 
                return (double)GetValue(ValueProperty); 
            }
            set
            {
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", 
            typeof(double), 
            typeof(Indicator), 
            new PropertyMetadata(0.0, ValuePropertyChanged)
            );

        private static void ValuePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Indicator ind = o as Indicator;

            if (ind != null)
            {
                if (ind.Owner != null && ind.Value >= ind.Owner.Minimum && ind.Value <= ind.Owner.Maximum)
                {
                    ind.OnValueChanged((double)e.NewValue, (double)e.OldValue);
                }
            }
        }

        #endregion


        #region Abstract methods

        protected virtual void OnValueChanged(double newVal, double oldVal) { }

        protected virtual void UpdateIndicatorOverride(Scale owner) { }

        // This wasn't virtual in the previous version
        public virtual void Arrange(Size finalSize)
        {
            base.Arrange(new Rect(new Point(), finalSize));
        }

        #endregion


        #region Helper methods (private)

        private void UpdateIndicator(Scale owner)
        {
            if (owner != null)
            {
                if (Value < owner.Minimum)
                    Value = owner.Minimum;
                if (Value > owner.Maximum)
                    Value = owner.Maximum;
            }
            UpdateIndicatorOverride(owner);
        }

        #endregion


        #region Overrides

        protected override Size MeasureOverride(Size availableSize)
        {
            // The main purpose of this override is to set the owner for the 
            // indicator. The actual measuring calculation will be done in 
            // the derived classes
            DependencyObject parent = base.Parent;
            while (parent != null)
            {
                Scale scale = parent as Scale;
                if (scale != null)
                {
                    this.Owner = scale;
                    break;
                }
                FrameworkElement el = parent as FrameworkElement;
                if (el != null)
                {
                    parent = el.Parent;
                }
            }
            return base.MeasureOverride(availableSize);
        }

        #endregion
    }
}
