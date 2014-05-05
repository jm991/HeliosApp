using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Helios
{
    [TemplatePart(Name = "PART_Marker", Type = typeof(ContentPresenter))]
    public class MarkerIndicator : Indicator
    {
        #region Fields (protected to give access to subclasses)

        protected ContentPresenter marker;

        #endregion


        #region Properties (public)

        public Point Center { get; set; }

        #endregion


        #region Constructor

        public MarkerIndicator()
        {
            DefaultStyleKey = typeof(MarkerIndicator);
        }

        #endregion


        #region Dependency properties

        // Using a DependencyProperty as the backing store for MarkerTemplate. This enables animation, styling, binding, etc...
        public DataTemplate MarkerTemplate
        {
            get 
            { 
                return (DataTemplate)GetValue(MarkerTemplateProperty); 
            }
            set 
            {
                SetValue(MarkerTemplateProperty, value); 
            }
        }
        public static readonly DependencyProperty MarkerTemplateProperty = DependencyProperty.Register(
            "MarkerTemplate", 
            typeof(DataTemplate), 
            typeof(MarkerIndicator), 
            new PropertyMetadata(null, MarkerTemplatePropertyChanged)
            );

        #endregion


        #region Dependency properties (handlers)

        private static void MarkerTemplatePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            // Update the indicator
            MarkerIndicator ind = o as MarkerIndicator;
            if (ind != null && ind.marker != null)
            {
                ind.marker.ContentTemplate = ind.MarkerTemplate;
            }
        }
        
        #endregion
        

        #region Overrides

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            marker = GetTemplateChild("PART_Marker") as ContentPresenter;
            // Make sure ManipulationMode="All" for the graphic drawn in the DataTemplate
            marker.ManipulationDelta += marker_ManipulationDelta;
        }

        public override void Arrange(Size finalSize)
        {
            base.Arrange(DesiredSize);
            // Call method to arrange the marker
            SetIndicatorTransforms();
            PositionMarker();
        }

        protected override void OnValueChanged(double newVal, double oldVal)
        {
            PositionMarker();
        }

        #endregion


        #region Methods (private)

        // Sets the render transform of the indicator
        private void SetIndicatorTransforms()
        {
            if (RenderTransform is MatrixTransform)
            {
                TransformGroup tg = new TransformGroup();
                TranslateTransform tt = new TranslateTransform();
                RotateTransform rt = new RotateTransform();

                tg.Children.Add(rt);
                tg.Children.Add(tt);

                this.RenderTransformOrigin = new Point(0.5, 0.5);
                this.RenderTransform = tg;
            }
        }
        
        private void PositionMarker()
        {
            if (Owner == null)
            {
                return;
            }
            if (Owner is RadialScale)
            {
                RadialScale rs = (RadialScale)Owner;
                // Get the angle based on the value
                double angle = rs.GetAngleFromValue(Value);
                if (rs.SweepDirection == SweepDirection.Counterclockwise)
                {
                    angle = -angle;
                }
                // Rotate the marker by angle
                TransformGroup tg = RenderTransform as TransformGroup;
                if (tg != null)
                {
                    RotateTransform rt = tg.Children[0] as RotateTransform;
                    if (rt != null)
                    {
                        rt.Angle = angle;
                    }
                }
                // Position the marker based on the radius
                Point offset = rs.GetIndicatorOffset();
                double rad = rs.GetIndicatorRadius();

                //position the marker
                double px = offset.X + (rad - DesiredSize.Height / 2) * Math.Sin(angle * Math.PI / 180);
                double py = offset.Y - (rad - DesiredSize.Height / 2) * Math.Cos(angle * Math.PI / 180);

                // Cache the intended position of the Indicator before it's centered
                this.Center = new Point(px, py);

                // Subtract the width of the Indicator's size so that it looks centered
                px -= DesiredSize.Width / 2;
                py -= DesiredSize.Height / 2;
                if (tg != null)
                {
                    TranslateTransform tt = tg.Children[1] as TranslateTransform;
                    if (tt != null)
                    {
                        tt.X = px;
                        tt.Y = py;
                    }
                }
            }
            //else
            //{
            //    LinearScale ls = Owner as LinearScale;
            //    Point offset = ls.GetIndicatorOffset(this);
            //    //the getIndicatorOffset returns only one correct dimension
            //    //for marker indicators the other dimension will have to be calculated again
            //    if (ls.Orientation == Orientation.Horizontal)
            //    {
            //        offset.X = ls.ActualWidth * (Value - ls.Minimum) / (ls.Maximum - ls.Minimum) - DesiredSize.Width / 2;
            //    }
            //    else
            //    {
            //        offset.Y = ls.ActualHeight - ls.ActualHeight * (Value - ls.Minimum) / (ls.Maximum - ls.Minimum) - DesiredSize.Height / 2;
            //    }
            //    TransformGroup tg = RenderTransform as TransformGroup;
            //    if (tg != null)
            //    {
            //        TranslateTransform tt = tg.Children[1] as TranslateTransform;
            //        if (tt != null)
            //        {
            //            tt.X = offset.X;
            //            tt.Y = offset.Y;
            //        }
            //    }
            //}
        }

        private double GetAngle(Point touchPoint, Point circleCenter)
        {
            var _X = touchPoint.X - circleCenter.X;
            var _Y = touchPoint.Y - circleCenter.Y;
            var _Hypot = Math.Sqrt(_X * _X + _Y * _Y);
            var _Value = Math.Asin(_Y / _Hypot) * 180 / Math.PI;
            var _Quadrant = (_X >= 0) ?
                (_Y >= 0) ? Quadrants.se : Quadrants.ne :
                (_Y >= 0) ? Quadrants.sw : Quadrants.nw;
            double oldVal = _Value;
            switch (_Quadrant)
            {
                case Quadrants.ne: _Value = 90 + _Value; break;
                case Quadrants.nw: _Value = -(90 + _Value); break;
                // TODO: fix the constants added to these two cases:
                case Quadrants.sw: _Value = 270 + _Value; break;
                case Quadrants.se: _Value = 090 - _Value; break;
            }
            //Debug.WriteLine("value: " + _Value + " oldVal: " + oldVal + " quad: " + _Quadrant);
            return _Value;
        }

        #endregion


        #region Event handlers

        void marker_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            if (Owner is RadialScale)
            {
                RadialScale rs = (RadialScale)Owner;
                //Debug.WriteLine("radial angle: " + rs.GetAngleFromValue(Value));
                GetAngle(this.Center, rs.Center);
                // this.Angle = GetAngle(e.Position, this.RenderSize);
                double radius = rs.GetIndicatorRadius();
                double diameter = radius * 2;
                Size rsSize = new Size(diameter, radius);
                Point radialScaleAbsPos = (((UIElement)rs).TransformToVisual((Frame)Window.Current.Content) as GeneralTransform).TransformPoint(new Point(0, 0));
                //Debug.WriteLine("position: " + e.Position + " size: " + rsSize + " center: " + RadialScaleHelper.GetCenterPosition(rs.RadialType, rsSize, rs.MinAngle, rs.MaxAngle, rs.SweepDirection) + " angle: " + GetAngle(e.Position, new Size(diameter, diameter)));

                //Debug.WriteLine("angle: " + GetAngle(this.Center, rs.Center));
                //Debug.WriteLine("Final Val: " + rs.GetValueFromAngle(GetAngle(this.Center.Add(e.Cumulative.Translation), rs.Center)));
                Value = rs.GetValueFromAngle(GetAngle(this.Center.Add(e.Position), rs.Center));
            }
        }

        #endregion
    }
}
