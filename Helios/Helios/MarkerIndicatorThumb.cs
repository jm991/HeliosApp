using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Helios
{
    [TemplatePart(Name = ThumbPartName, Type = typeof(Thumb))]
    public class MarkerIndicatorThumb : MarkerIndicator, INotifyPropertyChanged
    {
        #region Fields (private)

        private const string ThumbPartName = "PART_THUMB";

        private double m_Angle = default(double);

        private Path thumb;

        #endregion


        #region Properties (public)

        public double Angle
        {
            get
            {
                return m_Angle;
            }
            set
            {
                SetProperty(ref m_Angle, value);
            }
        }

        #endregion


        #region Overrides

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just before a UI element displays in your app. 
        /// Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // Need to apply base template first so that marker != null
            base.OnApplyTemplate();

            //this.thumb = Template.FindName(ThumbPartName, this) as Path;
            //this.thumb = marker.FindDescendantByName(ThumbPartName) as Path;
            //var myThumb = (marker as Control).GetTemplateChild.fin(ThumbPartName, typeof(Path));
            ////this.thumb = this.GetTemplateChild(ThumbPartName) as Path;

            //// React to dragging
            //if (myThumb != null && myThumb is Path)
            //{
            //    this.thumb = myThumb as Path;
            //    // Shouldn't use Windows.UI.Xaml.Controls.Primitives.Thumb class because it describes in term of vertical and horizontal
            //    // Using a normal control and ManipulationDelta is better since it provides touch positions in Cartesian coordinates
            //    this.thumb.ManipulationDelta += Thumb_ManipulationDelta;
            //}
        }

        #endregion


        #region Event handlers
        
        /// <summary>
        /// Handles the dragging delta event of the thumb control.
        /// </summary>
        private void Thumb_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            this.Angle = GetAngle(e.Position, this.RenderSize);
            Debug.WriteLine("angle: " + Angle);
            //this.Value = (int)(this.Angle / 360 * 100);
        }

        #endregion


        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;
        
        private void SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] String propertyName = null)
        {
            if (!object.Equals(storage, value))
            {
                storage = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        #endregion


        #region Helper methods (private)

        private double GetAngle(Point touchPoint, Size circleSize)
        {
            var _X = touchPoint.X - (circleSize.Width / 2d);
            var _Y = circleSize.Height - touchPoint.Y - (circleSize.Height / 2d);
            var _Hypot = Math.Sqrt(_X * _X + _Y * _Y);
            var _Value = Math.Asin(_Y / _Hypot) * 180 / Math.PI;
            var _Quadrant = (_X >= 0) ?
                (_Y >= 0) ? Quadrants.ne : Quadrants.se :
                (_Y >= 0) ? Quadrants.nw : Quadrants.sw;
            switch (_Quadrant)
            {
                case Quadrants.ne: _Value = 090 - _Value; break;
                case Quadrants.nw: _Value = 270 + _Value; break;
                case Quadrants.se: _Value = 090 - _Value; break;
                case Quadrants.sw: _Value = 270 + _Value; break;
            }
            return _Value;
        }

        #endregion
    }
}
