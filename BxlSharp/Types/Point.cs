using System;

namespace OriginalCircuit.BxlSharp.Types
{
    public readonly struct Point : IEquatable<Point>
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y) : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"X:{X} Y:{Y}";
        }

        public bool Equals(Point other)
        {
            return Math.Abs(X - other.X) < 1e-8 && Math.Abs(Y - other.Y) < 1e-8;
        }

        public override bool Equals(object obj)
        {
            return obj is Point otherPoint && Equals(otherPoint);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
    }
}
