using System;
using System.Collections.Generic;
using System.Windows;

namespace MorphComplementer
{
    class Model
    {
        public BezierCurve Bezier { get; set; } = new BezierCurve(new Point(0, 0), new Point(1, 1));
        public double ThinningThreshold { get; set; }

        /// <summary>
        /// 媒介変数T基準解像度によるベジェ曲線点群を得る
        /// </summary>
        /// <param name="resolution">媒介変数T基準解像度</param>
        /// <returns>ベジェ曲線点群</returns>
        public List<Point> GetCurveByT(int resolution)
        {
            var points = new List<Point>();

            for (int i = 0; i <= resolution; i++)
                points.Add(Bezier.GetPointByT(i / resolution));

            return points;
        }

        /// <summary>
        /// X座標基準解像度によるベジェ曲線点群を得る
        /// </summary>
        /// <param name="resolution">X座標基準解像度</param>
        /// <returns>ベジェ曲線点群</returns>
        public List<Point> GetCurveByX(int resolution)
        {
            var points = new List<Point>();

            for (int i = 0; i <= resolution; i++)
                points.Add(Bezier.GetPointByX(i / resolution));

            return points;
        }

        /// <summary>
        /// 曲線点群を間引く
        /// </summary>
        /// <param name="points">曲線を表す点群</param>
        /// <returns>間引き後の点群</returns>
        public List<Point> ThinningCurve(List<Point> points)
        {
            //2点間の傾き
            double GetGradient(Point A, Point B)
            {
                var p = B - A;
                return p.Y / p.X;
            }

            //起点の番号
            int focus = 0;
            while (focus < points.Count - 2)
            {
                //起点と直後の点の線分の傾きを間引き基準傾きとする
                var gradient = GetGradient(points[focus], points[focus + 1]);
                var removeIndices = new List<int>();
                //跨点の番号
                int i = focus + 2;
                //起点と跨点の線分の傾きと間引き基準傾きの差が閾値以下である場合、間に存在する点を除去する
                while (Math.Abs(gradient - GetGradient(points[focus], points[i])) < ThinningThreshold)
                {
                    removeIndices.Add(i - 1);
                    i++;
                    if (i >= points.Count)
                        break;
                }

                removeIndices.Reverse();
                foreach (var r in removeIndices)
                {
                    points.RemoveAt(r);
                }

                //起点を進める
                focus++;
            }

            return points;
        }
    }
}
