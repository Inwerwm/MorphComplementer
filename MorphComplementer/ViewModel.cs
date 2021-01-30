using Microsoft.Win32;
using MikuMikuMethods.Vmd;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MorphComplementer
{
    public class ViewModel : INotifyPropertyChanged, IDisposable
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

        public ReactiveCollection<string> InterpolateTypes { get; }

        public double CanvasHeight { get; set; }
        public double CanvasWidth { get; set; }

        Model model = new Model();

        Point ScaleToCanvasSize(Point target)
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

            InterpolateTypes = new ReactiveCollection<string>().AddTo(Disposable);

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
            model.Bezier.FirstCtrlPoint = new Point((Canvas.GetLeft(h) + 5) / CanvasWidth, (Canvas.GetTop(h) + 5) / CanvasHeight);
        }

        public void SetSecondCtrlPoint(Thumb h)
        {
            model.Bezier.SecondCtrlPoint = new Point((Canvas.GetLeft(h) + 5) / CanvasWidth, (Canvas.GetTop(h) + 5) / CanvasHeight);
        }

        private void RefleshThinningThreshold()
        {
            model.ThinningThreshold = ThinningThreshold.Value;
        }

        public PathGeometry MakeTheoricalBezierCurve()
        {
            if (!IsShowTCurve.Value)
                return null;

            var segments = new PathSegmentCollection();
            segments.Add(new BezierSegment(ScaleToCanvasSize(model.Bezier.FirstCtrlPoint), ScaleToCanvasSize(model.Bezier.SecondCtrlPoint), ScaleToCanvasSize(model.Bezier.EndPoint), true));

            var figures = new PathFigureCollection();
            figures.Add(new PathFigure(ScaleToCanvasSize(model.Bezier.StartPoint), segments, false));

            return new PathGeometry(figures);
        }

        public PathGeometry MakeComplementBezierCurve()
        {
            RefleshThinningThreshold();
            var points = new List<Point>();
            points.Add(ScaleToCanvasSize(model.Bezier.StartPoint));
            for (decimal i = 1; i < FrameLength.Value; i++)
            {
                points.Add(ScaleToCanvasSize(model.Bezier.ComputePointByX((double)(i / FrameLength.Value))));
            }
            points.Add(ScaleToCanvasSize(model.Bezier.EndPoint));
            List<Point> thinedPoints = new List<Point>();
            thinedPoints.AddRange(model.ThinningCurve(points));
            FrameNum.Value = thinedPoints.Count;

            var figures = new PathFigureCollection();
            figures.Add(new PathFigure(ScaleToCanvasSize(model.Bezier.StartPoint), new PathSegmentCollection(thinedPoints.Skip(1).Select(p => new LineSegment(p, true))), false));

            return new PathGeometry(figures);
        }

        public PathGeometry MakeGrid()
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
                return $"{DateTime.Now}{Environment.NewLine}ファイル出力に失敗しました。{Environment.NewLine}モーフ名を入力してください。";
            }

            RefleshThinningThreshold();
            var points = new List<Point>();
            points.Add(model.Bezier.StartPoint);
            for (decimal i = 1; i < FrameLength.Value; i++)
            {
                points.Add(model.Bezier.ComputePointByX((double)(i / FrameLength.Value)));
            }
            points.Add(model.Bezier.EndPoint);
            points = model.ThinningCurve(points);

            if (IsRound.Value)
            {
                var r = points.Select(p => new Point(2 - p.X, p.Y)).SkipLast(1).ToList();
                points.AddRange(r);
            }
            var outname = model.OutputVMD(points, FrameLength.Value, MorphName.Value, StartRatio.Value, EndRatio.Value);

            return $"{DateTime.Now}{Environment.NewLine}{outname}を出力しました。";
        }

        public (Point First, Point Second)? ImportFromVMD(MainWindow ownerWindow)
        {
            (Point First, Point Second) Scale((Point First, Point Second) target) =>
                (ScaleToCanvasSize(target.First), ScaleToCanvasSize(target.Second));

            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "VMDファイル(*.vmd)|*.vmd";
            if(openFileDialog.ShowDialog().Value)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (BinaryReader reader = new(new FileStream(openFileDialog.FileName, FileMode.Open), Encoding.GetEncoding("Shift_JIS")))
                {
                    VocaloidMotionData vmd = new(reader);
                    bool isCamera = vmd.ModelName == VocaloidMotionData.CAMERA_DATA_NAME;

                    InterpolateTypes.Clear();
                    InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.XPosition]);
                    InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.YPosition]);
                    InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.ZPosition]);
                    InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.Rotation]);
                    if (isCamera)
                    {
                        InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.Distance]);
                        InterpolateTypes.Add(model.IPMap[Model.InterpolationItem.ViewAngle]);
                    }

                    VmdImport vmdImportWindow = new(this);
                    vmdImportWindow.Owner = ownerWindow;
                    if (vmdImportWindow.ShowDialog().Value)
                        return Scale(model.ImportFromVMD(vmd, model.IPMap.First(p => p.Value == vmdImportWindow.SelectedItem).Key, isCamera));
                }
            }

            return null;
        }

        public void Dispose()
        {
            Disposable.Dispose();
        }
    }
}
