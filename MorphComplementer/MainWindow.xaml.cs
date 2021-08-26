using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MorphComplementer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int BoundY { get => (int)Math.Round(CurveCanvas.Height); }
        int BoundX { get => (int)Math.Round(CurveCanvas.Width); }

        ViewModel VM { get; } = new ViewModel();

        (Point First, Point Second) InitialPoints = (new(100, 100), new(300, 300));
        readonly int FRAME_LENGTH_LIMIT = 999;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = VM;
            VM.CanvasHeight = CurveCanvas.Height;
            VM.CanvasWidth = CurveCanvas.Width;
            RefleshCtrlPointDepends(Ctrl.First, FirstCtrlPoint);
            RefleshCtrlPointDepends(Ctrl.Second, SecondCtrlPoint);
            Grid.Data = VM.MakeGrid();
        }

        private void RefleshCurve()
        {
            CCurve.Data = VM.MakeComplementBezierCurve();
            TCurve.Data = VM.MakeTheoricalBezierCurve();
        }

        private enum Ctrl
        {
            First = 1,
            Second = 2
        }
        private void RefleshCtrlPointDepends(Ctrl pointnum, Thumb point)
        {
            switch (pointnum)
            {
                case Ctrl.First:
                    VM.SetFirstCtrlPoint(point);
                    FirstCtrlLine.X2 = Canvas.GetLeft(point) + 5;
                    FirstCtrlLine.Y2 = Canvas.GetTop(point) + 5;
                    break;
                case Ctrl.Second:
                    VM.SetSecondCtrlPoint(point);
                    SecondCtrlLine.X2 = Canvas.GetLeft(point) + 5;
                    SecondCtrlLine.Y2 = Canvas.GetTop(point) + 5;
                    break;
                default:
                    break;
            }
            RefleshCurve();
        }

        private void CtrlPoint_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var h = sender as Thumb;
            if (h == null) return;

            Canvas.SetLeft(h, Canvas.GetLeft(h) + e.HorizontalChange);
            Canvas.SetTop(h, Canvas.GetTop(h) + e.VerticalChange);

            if (Canvas.GetLeft(h) < -5)
                Canvas.SetLeft(h, -5);
            if (Canvas.GetTop(h) < -5)
                Canvas.SetTop(h, -5);

            if (Canvas.GetLeft(h) > BoundX - 5)
                Canvas.SetLeft(h, BoundX - 5);
            if (Canvas.GetTop(h) > BoundY - 5)
                Canvas.SetTop(h, BoundY - 5);
        }

        private void FirstCtrlPoint_DragDelta(object sender, DragDeltaEventArgs e)
        {
            CtrlPoint_DragDelta(sender, e);
            RefleshCtrlPointDepends(Ctrl.First, sender as Thumb);
        }

        private void SecondCtrlPoint_DragDelta(object sender, DragDeltaEventArgs e)
        {
            CtrlPoint_DragDelta(sender, e);
            RefleshCtrlPointDepends(Ctrl.Second, sender as Thumb);
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Grid.Data = VM.MakeGrid();
            RefleshCurve();
        }

        private void SetCtrlPoints((Point First, Point Second) positons)
        {
            Canvas.SetLeft(FirstCtrlPoint, positons.First.X - 5);
            Canvas.SetTop(FirstCtrlPoint, positons.First.Y - 5);
            Canvas.SetLeft(SecondCtrlPoint, positons.Second.X - 5);
            Canvas.SetTop(SecondCtrlPoint, positons.Second.Y - 5);

            RefleshCtrlPointDepends(Ctrl.First, FirstCtrlPoint);
            RefleshCtrlPointDepends(Ctrl.Second, SecondCtrlPoint);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SetCtrlPoints(InitialPoints);
        }


        private void IntegerOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !new Regex("[0-9]").IsMatch(e.Text);
        }

        private void FloatOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !new Regex("[0-9.e-]").IsMatch(e.Text);
        }

        private void CurveSettingTextBox_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (VM.FrameLength.Value > FRAME_LENGTH_LIMIT)
                VM.FrameLength.Value = FRAME_LENGTH_LIMIT;
            if (VM.FrameLength.Value < 0)
                VM.FrameLength.Value = 2;
            RefleshCurve();
            Grid.Data = VM.MakeGrid();
        }

        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogBlock.Text = VM.Write().Replace("_", "__");
            }
            catch (System.IO.IOException ex)
            {
                MessageBox.Show($"VMDファイルの出力に失敗しました。", "出力エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                LogBlock.Text = $"VMDファイルの出力に失敗しました。{Environment.NewLine}{ex.GetType()}:{Environment.NewLine}{ex.Message}";
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                (Point First, Point Second) impotedPoints = VM.ImportFromVMD(this) ?? InitialPoints;
                SetCtrlPoints(impotedPoints);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
