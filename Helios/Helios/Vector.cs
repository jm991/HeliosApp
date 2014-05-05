using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Helios
{
    public struct Vector
    {
        internal double _x;
        internal double _y;

        public static bool operator ==(Vector vector1, Vector vector2)
        {
            return ((vector1.X == vector2.X) && (vector1.Y == vector2.Y));
        }

        public static bool operator !=(Vector vector1, Vector vector2)
        {
            return !(vector1 == vector2);
        }

        public static bool Equals(Vector vector1, Vector vector2)
        {
            return (vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y));
        }

        public override bool Equals(object o)
        {
            if ((o == null) || !(o is Vector))
            {
                return false;
            }
            Vector vector = (Vector)o;
            return Equals(this, vector);
        }

        public bool Equals(Vector value)
        {
            return Equals(this, value);
        }

        public override int GetHashCode()
        {
            return (this.X.GetHashCode() ^ this.Y.GetHashCode());
        }


        public double X
        {
            get
            {
                return this._x;
            }
            set
            {
                this._x = value;
            }
        }
        public double Y
        {
            get
            {
                return this._y;
            }
            set
            {
                this._y = value;
            }
        }

        public Vector(double x, double y)
        {
            this._x = x;
            this._y = y;
        }

        public static Vector FromPoint(Point pt)
        {
            return new Vector(pt.X, pt.Y);
        }

        public double Length
        {
            get
            {
                return Math.Sqrt((this._x * this._x) + (this._y * this._y));
            }
        }
        public double LengthSquared
        {
            get
            {
                return ((this._x * this._x) + (this._y * this._y));
            }
        }
        public void Normalize()
        {
            this = (Vector)(this / Math.Max(Math.Abs(this._x), Math.Abs(this._y)));
            this = (Vector)(this / this.Length);
        }

        public static double CrossProduct(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._y) - (vector1._y * vector2._x));
        }

        public static double AngleBetween(Vector vector1, Vector vector2)
        {
            double y = (vector1._x * vector2._y) - (vector2._x * vector1._y);
            double x = (vector1._x * vector2._x) + (vector1._y * vector2._y);
            return (Math.Atan2(y, x) * 57.295779513082323);
        }

        public static Vector operator -(Vector vector)
        {
            return new Vector(-vector._x, -vector._y);
        }

        public void Negate()
        {
            this._x = -this._x;
            this._y = -this._y;
        }

        public static Vector operator +(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
        }

        public static Vector Add(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
        }

        public static Vector operator -(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
        }

        public static Vector Subtract(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
        }



        public static Vector operator *(Vector vector, double scalar)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector Multiply(Vector vector, double scalar)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector operator *(double scalar, Vector vector)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector Multiply(double scalar, Vector vector)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector operator /(Vector vector, double scalar)
        {
            return (Vector)(vector * (1.0 / scalar));
        }

        public static Vector Divide(Vector vector, double scalar)
        {
            return (Vector)(vector * (1.0 / scalar));
        }

        public static double operator *(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
        }

        public static double Multiply(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
        }

        public static double Determinant(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._y) - (vector1._y * vector2._x));
        }

        public static explicit operator Size(Vector vector)
        {
            return new Size(Math.Abs(vector._x), Math.Abs(vector._y));
        }

        public static explicit operator Point(Vector vector)
        {
            return new Point(vector._x, vector._y);
        }
    }
}
