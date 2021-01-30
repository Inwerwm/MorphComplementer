using MikuMikuMethods.Vmd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public List<Point> ComputeCurveByT(int resolution)
        {
            var points = new List<Point>();

            for (int i = 0; i <= resolution; i++)
                points.Add(Bezier.ComputePointByT(i / resolution));

            return points;
        }

        /// <summary>
        /// X座標基準解像度によるベジェ曲線点群を得る
        /// </summary>
        /// <param name="resolution">X座標基準解像度</param>
        /// <returns>ベジェ曲線点群</returns>
        public List<Point> ComputeCurveByX(int resolution)
        {
            var points = new List<Point>();

            for (int i = 0; i <= resolution; i++)
                points.Add(Bezier.ComputePointByX(i / resolution));

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
            double ComputeGradient(Point A, Point B)
            {
                var p = B - A;
                return p.Y / p.X;
            }

            //起点の番号
            int focus = 0;
            while (focus < points.Count - 2)
            {
                //起点と直後の点の線分の傾きを間引き基準傾きとする
                var gradient = ComputeGradient(points[focus], points[focus + 1]);
                var removePoints = new List<Point>();
                //中間点の番号
                int i = focus + 2;
                //起点と中間点の線分の傾きと間引き基準傾きの差が閾値以下である場合、間に存在する点を除去する
                while (Math.Abs(gradient - ComputeGradient(points[focus], points[i])) < ThinningThreshold)
                {
                    removePoints.Add(points[i - 1]);
                    i++;
                    if (i >= points.Count)
                        break;
                }

                points.RemoveAll(p =>removePoints.Contains(p));

                focus++;
            }

            return points;
        }

        public string OutputVMD(List<Point> points, int frameLength, string morphName, double startRatio, double endRatio)
        {
            string FileName = $"Complemented_{morphName}.vmd";

            VocaloidMotionData vmd = new VocaloidMotionData();
            vmd.ModelName = "Complemented Morphs";
            vmd.MorphFrames.AddRange(points.Select(p => new VmdMorphFrameData(morphName, (uint)Math.Round(p.X * frameLength), (float)(p.Y*(endRatio-startRatio)+startRatio))));

            using (BinaryWriter writer = new BinaryWriter(new FileStream(FileName, FileMode.OpenOrCreate)))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                vmd.Write(writer);
            }

            return FileName;
        }

        public (Point First, Point Second) ImportFromVMD(VocaloidMotionData vmd, InterpolationItem interpolation, bool isCamera)
        {
            VmdCameraFrameData? firstCameraFrame = vmd.CameraFrames.Count > 0 ? vmd.CameraFrames.First() : null;
            VmdMotionFrameData? firstMotionFrame = vmd.MotionFrames.Count > 0 ? vmd.MotionFrames.First() : null;

            if (isCamera && firstCameraFrame is null)
                throw new ArgumentNullException("VMDからフレームを発見出来ませんでした。");
            if(!isCamera && firstMotionFrame is null)
                throw new ArgumentNullException("VMDからフレームを発見出来ませんでした。");

            return interpolation switch
            {
                InterpolationItem.XPosition => isCamera ? IPC(firstCameraFrame?.InterpolatePointX) : IPM(firstMotionFrame?.InterpolatePointX),
                InterpolationItem.YPosition => isCamera ? IPC(firstCameraFrame?.InterpolatePointY) : IPM(firstMotionFrame?.InterpolatePointY),
                InterpolationItem.ZPosition => isCamera ? IPC(firstCameraFrame?.InterpolatePointZ) : IPM(firstMotionFrame?.InterpolatePointZ),
                InterpolationItem.Rotation => isCamera ? IPC(firstCameraFrame?.InterpolatePointR) : IPM(firstMotionFrame?.InterpolatePointR),
                InterpolationItem.Distance => IPC(firstCameraFrame?.InterpolatePointD),
                InterpolationItem.ViewAngle => IPC(firstCameraFrame?.InterpolatePointA),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// モーションの補間曲線の制御点の座標を取得する
        /// </summary>
        /// <param name="bytes">もととなるバイト列</param>
        /// <returns>補間曲線の制御点</returns>
        private (Point First, Point Second) IPM(byte[] bytes) => (new(bytes[0] / 127.0, bytes[1] / 127.0), new(bytes[2] / 127.0, bytes[3] / 127.0));
        /// <summary>
        /// カメラの補間曲線の制御点の座標を取得する
        /// </summary>
        /// <param name="bytes">もととなるバイト列</param>
        /// <returns>補間曲線の制御点</returns>
        private (Point First, Point Second) IPC(byte[] bytes) => (new(bytes[0] / 127.0, bytes[2] / 127.0), new(bytes[1] / 127.0, bytes[3] / 127.0));

        public enum InterpolationItem
        {
            /// <summary>
            /// x軸移動
            /// </summary>
            XPosition,
            /// <summary>
            /// Y軸移動
            /// </summary>
            YPosition,
            /// <summary>
            /// Z軸移動
            /// </summary>
            ZPosition,
            /// <summary>
            /// 回転
            /// </summary>
            Rotation,
            /// <summary>
            /// 距離
            /// </summary>
            Distance,
            /// <summary>
            /// 視野角
            /// </summary>
            ViewAngle,
        }

        public Dictionary<InterpolationItem, string> IPMap => new()
        {
            { InterpolationItem.XPosition, "X軸移動" },
            { InterpolationItem.YPosition, "Y軸移動" },
            { InterpolationItem.ZPosition, "Z軸移動" },
            { InterpolationItem.Rotation, "回転" },
            { InterpolationItem.Distance, "距離" },
            { InterpolationItem.ViewAngle, "視野角" }
        };
    }
}
