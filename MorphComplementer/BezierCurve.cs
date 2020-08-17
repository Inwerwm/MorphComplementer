using System;
using System.Windows;

namespace MorphComplementer
{
    class BezierCurve
    {
        readonly double COORDINATE_ACCURACY = 1e-8;

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public Point FirstCtrlPoint { get; set; }
        public Point SecondCtrlPoint { get; set; }

        public BezierCurve(Point startPoint = new Point(), Point endPoint = new Point(), Point firstCtrlPoint = new Point(), Point secondCtrlPoint = new Point())
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            FirstCtrlPoint = firstCtrlPoint;
            SecondCtrlPoint = secondCtrlPoint;
        }

        public Point ComputePointByT(double t)
        {
            return
                StartPoint.Mul(-1)
                .Plus(
                    FirstCtrlPoint.Mul(3)
                )
                .Minus(
                    SecondCtrlPoint.Mul(3)
                )
                .Plus(EndPoint)
                .Mul(t * t * t)
                .Plus(
                    StartPoint.Minus(
                        FirstCtrlPoint.Mul(2)
                    )
                .Plus(SecondCtrlPoint)
                .Mul(3 * t * t)
                )
                .Plus(
                    StartPoint
                    .Mul(-1)
                    .Plus(FirstCtrlPoint)
                    .Mul(3 * t)
                )
                .Plus(StartPoint)
            ;
        }

        public Point ComputePointByX(double x)
        {
            var t = x;
            Point p = new Point();
            for (int i = 0; i < 20; i++)
            {
                p = ComputePointByT(t);
                var ft = p.X - x;
                if (Math.Abs(ft) <= COORDINATE_ACCURACY)
                    break;
                t -= ft / 2;
            }
            return p;
        }

    }
}
