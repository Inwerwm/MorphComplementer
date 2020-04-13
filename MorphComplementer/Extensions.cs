using System.Windows;

namespace MorphComplementer
{
    static class Extensions
    {
        public static Point Plus(this Point A, Point B)
        {
            return new Point(A.X + B.X, A.Y + B.Y);
        }

        public static Point Minus(this Point A, Point B)
        {
            return new Point(A.X - B.X, A.Y - B.Y);
        }

        public static Point Mul(this Point A, double B)
        {
            return new Point(A.X * B, A.Y * B);
        }
    }
}
