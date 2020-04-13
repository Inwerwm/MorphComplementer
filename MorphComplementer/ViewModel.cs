using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MorphComplementer
{
    class ViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();

        public ReactiveProperty<string> MorphName { get; }
        public ReactiveProperty<int> FrameLength { get; }
        public ReactiveProperty<int> FrameNum { get; }
        public ReactiveProperty<double> StartRatio { get; }
        public ReactiveProperty<double> EndRatio { get; }
        public ReactiveProperty<double> ThinningThreshold { get; }

        public ReactiveProperty<bool> IsShowGrid { get; }
        public ReactiveProperty<bool> IsShowTCurve { get; }
        public ReactiveProperty<bool> IsRound { get; }

        public double CanvasHeight { get; set; }
        public double CanvasWidth { get; set; }

        Model M = new Model();

        Point Scale(Point target)
        {
            return new Point(target.X * CanvasWidth, target.Y * CanvasHeight);
        }

        public ViewModel()
        {
            MorphName = new ReactiveProperty<string>().AddTo(Disposable);
            FrameLength = new ReactiveProperty<int>().AddTo(Disposable);
            FrameNum = new ReactiveProperty<int>().AddTo(Disposable);
            StartRatio = new ReactiveProperty<double>().AddTo(Disposable);
            EndRatio = new ReactiveProperty<double>().AddTo(Disposable);
            ThinningThreshold = new ReactiveProperty<double>().AddTo(Disposable);

            IsShowGrid = new ReactiveProperty<bool>().AddTo(Disposable);
            IsShowTCurve = new ReactiveProperty<bool>().AddTo(Disposable);
            IsRound = new ReactiveProperty<bool>().AddTo(Disposable);

            MorphName.Value = "";
            FrameLength.Value = 2;
            StartRatio.Value = 0;
            EndRatio.Value = 1;
            ThinningThreshold.Value = 0.025;

            IsShowGrid.Value = true;

            RefleshThinningThreshold();
        }

        public void SetFirstCtrlPoint(Thumb h)
        {
            M.Bezier.FirstCtrlPoint = new Point((Canvas.GetLeft(h) + 5) / CanvasWidth, (Canvas.GetTop(h) + 5) / CanvasHeight);
        }

        public void SetSecondCtrlPoint(Thumb h)
        {
            M.Bezier.SecondCtrlPoint = new Point((Canvas.GetLeft(h) + 5) / CanvasWidth, (Canvas.GetTop(h) + 5) / CanvasHeight);
        }

        private void RefleshThinningThreshold()
        {
            M.ThinningThreshold = ThinningThreshold.Value;
        }

        public PathGeometry GetTheoricalBezierCurve()
        {
            if (!IsShowTCurve.Value)
                return null;

            var segments = new PathSegmentCollection();
            segments.Add(new BezierSegment(Scale(M.Bezier.FirstCtrlPoint), Scale(M.Bezier.SecondCtrlPoint), Scale(M.Bezier.EndPoint), true));

            var figures = new PathFigureCollection();
            figures.Add(new PathFigure(Scale(M.Bezier.StartPoint), segments, false));

            return new PathGeometry(figures);
        }

        public PathGeometry GetComplementBezierCurve()
        {
            RefleshThinningThreshold();
            var points = new List<Point>();
            points.Add(Scale(M.Bezier.StartPoint));
            for (decimal i = 1; i < FrameLength.Value; i++)
            {
                points.Add(Scale(M.Bezier.GetPointByX((double)(i / FrameLength.Value))));
            }
            points.Add(Scale(M.Bezier.EndPoint));
            List<Point> thinedPoints = new List<Point>();
            thinedPoints.AddRange(M.ThinningCurve(points));
            FrameNum.Value = thinedPoints.Count;

            var figures = new PathFigureCollection();
            figures.Add(new PathFigure(Scale(M.Bezier.StartPoint), new PathSegmentCollection(thinedPoints.Skip(1).Select(p => new LineSegment(p, true))), false));

            return new PathGeometry(figures);
        }

        public PathGeometry GetGrid()
        {
            if (!IsShowGrid.Value)
                return null;

            var points = new List<Point>();
            for (int i = 1; i < FrameLength.Value; i++)
            {
                var x = CanvasWidth * i / FrameLength.Value;
                points.Add(new Point(x, 0));
                points.Add(new Point(x, CanvasHeight));
            }

            var figures = new PathFigureCollection();
            figures.Add(new PathFigure(new Point(0, 0), new PathSegmentCollection(points.Select((p, i) => new LineSegment(p, i % 2 == 1))), false));

            return new PathGeometry(figures);
        }

        public string Write()
        {
            if (MorphName.Value == "")
            {
                return $"ファイル出力に失敗しました{Environment.NewLine}モーフ名を入力してください。";
            }

            RefleshThinningThreshold();
            var points = new List<Point>();
            points.Add(M.Bezier.StartPoint);
            for (decimal i = 1; i < FrameLength.Value; i++)
            {
                points.Add(M.Bezier.GetPointByX((double)(i / FrameLength.Value)));
            }
            points.Add(M.Bezier.EndPoint);
            points = M.ThinningCurve(points);

            if (IsRound.Value)
            {
                var r = points.Select(p => new Point(2 - p.X, p.Y)).SkipLast(1).ToList();
                points.AddRange(r);
            }
            var outname = M.OutputVMD(points, FrameLength.Value, MorphName.Value, StartRatio.Value, EndRatio.Value);

            return $"{DateTime.Now}{Environment.NewLine}{outname}を出力しました。";
        }

        public void Dispose()
        {
            Disposable.Dispose();
        }
    }
}
