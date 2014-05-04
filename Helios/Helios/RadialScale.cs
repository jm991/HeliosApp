using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Helios
{
    public class RadialScale : Scale
    {
        public RadialType RadialType = RadialType.Quadrant;

        public double MinAngle = -120;

        public double MaxAngle = 120;

        public double Maximum = 100;

        public double Minimum = 0;

        public SweepDirection SweepDirection = SweepDirection.Clockwise;

        internal double GetAngleFromValue(double value)
        {
            //ANGLE=((maxa-mina)*VAL+mina*maxv-maxa*minv)/(maxv-minv)
            double angle = ((MaxAngle - MinAngle) * value + MinAngle * Maximum - MaxAngle * Minimum) / (Maximum - Minimum);
            return angle;
        }
        
        //radius of the owner - labels and ticks and ranges
        internal double GetIndicatorRadius()
        {
            return 100;
        }

        protected override void ArrangeTicks(Windows.Foundation.Size finalSize)
        {
        }

        protected override void ArrangeLabels(Windows.Foundation.Size finalSize)
        {
        }

        protected override void ArrangeRanges(Windows.Foundation.Size finalSize)
        {
        }

        protected override void CreateRanges()
        {
        }

        protected override void ClearRanges()
        {
        }

        protected override void RefreshRanges()
        {
        }

        internal Point GetIndicatorOffset()
        {
            return RadialScaleHelper.GetCenterPosition(RadialType, new Size(ActualWidth, ActualHeight), MinAngle, MaxAngle, SweepDirection);
        }
    }
}
