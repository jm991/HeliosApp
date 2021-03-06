﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Helios
{
    public class RadialScale : Scale
    {
        #region Fields (private)

        private List<Path> ranges = new List<Path>();
        private Path def = new Path();
        // TODO: figure out why this can't be a DP w/o XamlParseException
        // This property can be used to minimize the space used by radial gauges when the angle span is within certain intervals. 
        // Span <= 90 degrees: use Quarter to constrain the control to draw only a quadrant of the entire circle
        // Span <= 180 degrees: use Semicircle to constrain the control to draw only half of an circle
        // For everything else, use Circle
        private RadialType radialType = RadialType.Semicircle;

        #endregion


        #region Properties (public)

        public Point Center { get; set; }

        #endregion


        #region Dependency properties

        public double MinAngle
        {
            get { return (double)GetValue(MinAngleProperty); }
            set { SetValue(MinAngleProperty, value); }
        }
        public static readonly DependencyProperty MinAngleProperty = DependencyProperty.Register(
            "MinAngle", 
            typeof(double), 
            typeof(RadialScale), 
            new PropertyMetadata(60.0, AnglePropertiesChanged)
            );

        public double MaxAngle
        {
            get { return (double)GetValue(MaxAngleProperty); }
            set { SetValue(MaxAngleProperty, value); }
        }
        public static readonly DependencyProperty MaxAngleProperty = DependencyProperty.Register(
            "MaxAngle", 
            typeof(double), 
            typeof(RadialScale), 
            new PropertyMetadata(300.0, AnglePropertiesChanged)
            );

        public SweepDirection SweepDirection
        {
            get { return (SweepDirection)GetValue(SweepDirectionProperty); }
            set { SetValue(SweepDirectionProperty, value); }
        }
        public static readonly DependencyProperty SweepDirectionProperty = DependencyProperty.Register(
            "SweepDirection", 
            typeof(SweepDirection), 
            typeof(RadialScale), 
            new PropertyMetadata(SweepDirection.Clockwise, SweepDirectionPropertyChanged)
            );

        //public RadialTickPlacement TickPlacement
        //{
        //    get { return (RadialTickPlacement)GetValue(TickPlacementProperty); }
        //    set { SetValue(TickPlacementProperty, value); }
        //}
        //public static readonly DependencyProperty TickPlacementProperty =
        //    DependencyProperty.Register("TickPlacement", typeof(RadialTickPlacement), typeof(RadialScale), new PropertyMetadata(RadialTickPlacement.Outward, TickPlacementPropertyChanged));
        
        public RadialType RadialType
        {
            get 
            {
                return radialType;
                //return (RadialType)GetValue(RadTypeProperty); 
            }
            set 
            {
                radialType = value;
                //SetValue(RadTypeProperty, value); 
            }
        }
        //public static readonly DependencyProperty RadialTypeProperty = DependencyProperty.Register(
        //    "RadialType", 
        //    typeof(RadialType), 
        //    typeof(RadialScale), 
        //    new PropertyMetadata(RadialType.Quadrant, RadialTypePropertyChanged)
        //    );
        
        //public bool EnableLabelRotation
        //{
        //    get { return (bool)GetValue(EnableLabelRotationProperty); }
        //    set { SetValue(EnableLabelRotationProperty, value); }
        //}
        //public static readonly DependencyProperty EnableLabelRotationProperty = DependencyProperty.Register(
        //    "EnableLabelRotation", 
        //    typeof(bool), 
        //    typeof(RadialScale), 
        //    new PropertyMetadata(true, LabelRotationPropertyChanged)
        //    );
        
        #endregion


        #region Dependency properties (handlers)

        private static void AnglePropertiesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            RadialScale scale = o as RadialScale;
            if (scale != null)
            {
                scale.RefreshLayout();
            }
        }

        private static void SweepDirectionPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            RadialScale scale = o as RadialScale;
            if (scale != null)
            {
                scale.RefreshLayout();
            }
        }

        //private static void TickPlacementPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        //{
        //    RadialScale scale = o as RadialScale;
        //    if (scale != null)
        //    {
        //        scale.RefreshLayout();
        //    }
        //}

        private static void RadialTypePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            RadialScale scale = o as RadialScale;
            if (scale != null)
            {
                scale.RefreshLayout();
            }
        }

        //private static void LabelRotationPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        //{
        //    RadialScale scale = o as RadialScale;
        //    if (scale != null)
        //    {
        //        scale.RefreshLayout();
        //    }
        //}

        #endregion


        #region Constructors

        public RadialScale()
        {
            CreateRanges();
        }

        #endregion


        #region Helper methods (private)

        private void RefreshLayout()
        {
            //update layout here
            RefreshLabels();
            RefreshTicks();
            RefreshRanges();
            RefreshIndicators();
        }

        //private void PositionTick(Tick tick, double x, double y, double rad)
        //{
        //    // Tick tick = ticks[i];
        //    double tickW = tick.DesiredSize.Width;
        //    double tickH = tick.DesiredSize.Height;

        //    double angle = GetAngleFromValue(tick.Value);
        //    if (SweepDirection == SweepDirection.Counterclockwise)
        //        angle = -angle;
        //    //position the tick
        //    double px = x + (rad) * Math.Sin(angle * Math.PI / 180);
        //    double py = y - (rad) * Math.Cos(angle * Math.PI / 180);
        //    px -= tickW / 2;
        //    py -= tickH / 2;
        //    tick.Arrange(new Rect(new Point(px, py), tick.DesiredSize));

        //    //rotate the tick (if not label or if it is label and has rotation enabled)
        //    if ((EnableLabelRotation && tick.TickType == TickType.Label) || tick.TickType != TickType.Label)
        //    {
        //        RotateTransform tr = new RotateTransform();
        //        tr.Angle = angle;
        //        tick.RenderTransformOrigin = new Point(0.5, 0.5);
        //        tick.RenderTransform = tr;
        //    }
        //}

        internal double GetAngleFromValue(double value)
        {
            // ANGLE = ((maxa - mina) * VAL + (mina * maxv) - (maxa * minv)) / (maxv - minv)
            double angle = ((MaxAngle - MinAngle) * value + (MinAngle * Maximum) - (MaxAngle * Minimum)) / (Maximum - Minimum);
            return angle;
        }

        internal double GetValueFromAngle(double angle)
        {
            // VALUE = ((maxv - minv) * angle - (mina * maxv) + (maxa * minv)) / (maxa - mina)
            double value = ((Maximum - Minimum) * angle - (MinAngle * Maximum) + (MaxAngle * Minimum)) / (MaxAngle - MinAngle);
            return value;
        }

        internal Point GetIndicatorOffset()
        {
            return RadialScaleHelper.GetCenterPosition(RadialType, this.DesiredSize/*new Size(ActualWidth, ActualHeight)*/, MinAngle, MaxAngle, SweepDirection);
        }

        #endregion


        #region Overrides

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0.0;
            double height = 0.0;
            if (!double.IsInfinity(availableSize.Width))
            {
                width = availableSize.Width;
            }
            if (!double.IsInfinity(availableSize.Height))
            {
                height = availableSize.Height;
            }
            Size size = new Size(width, height);

            // Measure all the children
            //foreach (Tick label in GetLabels())
            //{
            //    label.Measure(availableSize);
            //}
            //foreach (Tick tick in GetTicks())
            //{
            //    tick.Measure(availableSize);
            //}
            foreach (Path path in ranges)
            {
                path.Measure(availableSize);
            }
            if (Indicators != null)
            {
                foreach (UIElement ind in Indicators)
                {
                    ind.Measure(availableSize);
                }
            }

            // Return the available size as everything else will be 
            // arranged to fit inside.
            return size;
        }

        protected override void ArrangeTicks(Size finalSize)
        {
            //double maxRad = RadialScaleHelper.GetRadius(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);
            //Point center = RadialScaleHelper.GetCenterPosition(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);
            //double x = center.X;
            //double y = center.Y;

            //var ticks = GetTicks();
            //var labels = GetLabels();

            //double rad = maxRad - labels.Max(p => p.DesiredSize.Height) - ticks.Max(p => p.DesiredSize.Height) - 1;
            //if (TickPlacement == RadialTickPlacement.Inward)
            //{
            //    rad = maxRad;
            //    //no matter how thick the range is, don't use it if it is disabled
            //    if (UseDefaultRange || Ranges.Count > 0)
            //        rad -= RangeThickness;
            //}

            //for (int i = 0; i < ticks.Count; i++)
            //{
            //    if (TickPlacement == RadialTickPlacement.Outward)
            //        PositionTick(ticks[i], x, y, rad + ticks[i].DesiredSize.Height / 2);
            //    else
            //        PositionTick(ticks[i], x, y, rad - ticks[i].DesiredSize.Height / 2);
            //}
        }

        protected override void ArrangeLabels(Size finalSize)
        {
            //double maxRad = RadialScaleHelper.GetRadius(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);
            //Point center = RadialScaleHelper.GetCenterPosition(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);
            //double x = center.X;
            //double y = center.Y;
            //double rad = maxRad;
            //if (TickPlacement == RadialTickPlacement.Inward)
            //{
            //    rad = maxRad - GetTicks().Max(p => p.DesiredSize.Height);
            //    //no matter how thick the range is, don't use it if it is disabled
            //    if (UseDefaultRange || Ranges.Count > 0)
            //        rad -= RangeThickness;
            //}
            //var labels = GetLabels();

            //for (int i = 0; i < labels.Count; i++)
            //{
            //    PositionTick(labels[i], x, y, rad - labels[i].DesiredSize.Height / 2);
            //}
        }

        protected override void ArrangeRanges(Size finalSize)
        {
            try
            {
                double maxRad = RadialScaleHelper.GetRadius(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);
                Point center = RadialScaleHelper.GetCenterPosition(RadialType, finalSize, MinAngle, MaxAngle, SweepDirection);

                // Cache the center of the RadialScale
                this.Center = center;

                double x = center.X;
                double y = center.Y;
                // Calculate the ranges' radius
                double rad = maxRad;
                //if (TickPlacement == RadialTickPlacement.Outward)
                //{
                //    rad = maxRad - GetLabels().Max(p => p.DesiredSize.Height) - GetTicks().Max(p => p.DesiredSize.Height) - 1;
                //}
                // Draw the default range
                if (UseDefaultRange)
                {
                    double min = MinAngle, max = MaxAngle;
                    if (SweepDirection == SweepDirection.Counterclockwise)
                    {
                        min = -min;
                        max = -max;
                    }
                    // The null check needs to be done because otherwise the arrange pass will be called 
                    // recursevely as I set new content for the path in every call
                    Geometry geom = RadialScaleHelper.CreateArcGeometry(min, max, rad, RangeThickness, SweepDirection);
                    if (def.Data == null || def.Data.Bounds != geom.Bounds)
                    {
                        def.Data = geom;
                    }
                    // Arrange the default range. move the start point of the 
                    // figure (0, 0) in the center point (center)
                    def.Arrange(new Rect(center, finalSize));
                }

                // Arrange the rest of the ranges
                double prevAngle = MinAngle;
                if (SweepDirection == SweepDirection.Counterclockwise)
                {
                    prevAngle = -prevAngle;
                }

                for (int i = 0; i < ranges.Count; i++)
                {
                    Path range = ranges[i];
                    GaugeRange rng = Ranges[i];
                    double nextAngle = GetAngleFromValue(rng.Offset);
                    if (SweepDirection == SweepDirection.Counterclockwise)
                    {
                        nextAngle = -nextAngle;
                    }

                    range.Fill = new SolidColorBrush(rng.Color);
                    Geometry geom = RadialScaleHelper.CreateArcGeometry(prevAngle, nextAngle, rad, RangeThickness, SweepDirection);
                    // Check to avoid infinite loop
                    if (range.Data == null || range.Data.Bounds != geom.Bounds)
                        range.Data = geom;
                    // Arrange the default range. move the start point of the 
                    // figure (0, 0) in the center point (center)
                    range.Arrange(new Rect(center, finalSize));
                    prevAngle = nextAngle;
                }
            }
            catch { /* In the designer this throws an argument exception. I don't know why. */}
        }

        protected override void CreateRanges()
        {
            // Insert the default range
            if (UseDefaultRange)
            {
                def = new Path();
                def.Fill = new SolidColorBrush(DefaultRangeColor);
                Children.Add(def);
            }
            foreach (GaugeRange r in Ranges)
            {
                Path path = new Path();
                path.Fill = new SolidColorBrush(r.Color);

                ranges.Add(path);
                Children.Add(path);
            }
        }

        protected override void ClearRanges()
        {
            // Remove the default range. Tis should be removed all the time
            // only the range creation method decides whether to add it or not.
            Children.Remove(def);
            def = null;
            for (int i = 0; i < ranges.Count; i++)
            {
                Children.Remove(ranges[i]);
            }
            ranges.Clear();
        }

        protected override void RefreshRanges()
        {
            ClearRanges();
            CreateRanges();
        }

        #endregion


        #region Method

        // Radius of the owner - labels and ticks and ranges
        public double GetIndicatorRadius()
        {
            double maxRad = RadialScaleHelper.GetRadius(RadialType, /*new Size(ActualWidth, ActualHeight)*/this.DesiredSize, MinAngle, MaxAngle, SweepDirection);
            double rad = maxRad;// -GetLabels().Max(p => p.DesiredSize.Height) - GetTicks().Max(p => p.DesiredSize.Height) - 3;
            // Only if we have ranges
            if (UseDefaultRange || Ranges.Count > 0)
            {
                rad -= RangeThickness;
            }
            return rad;
        }

        #endregion
    }
}
