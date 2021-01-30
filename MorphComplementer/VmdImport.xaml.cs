using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MorphComplementer
{
    /// <summary>
    /// VmdImport.xaml の相互作用ロジック
    /// </summary>
    public partial class VmdImport : Window
    {
        ViewModel VM { get; }
        public string? SelectedItem { get; private set; }

        public VmdImport(ViewModel vm)
        {
            InitializeComponent();

            VM = vm;
            DataContext = VM;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = CurveList.SelectedItem as string;
            DialogResult = SelectedItem is not null;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
