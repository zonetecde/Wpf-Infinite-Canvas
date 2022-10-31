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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            InfiniteBoard.CellAdded += (sender, e) =>
            {


            };
            InfiniteBoard.PlaceControl(new Point(50, 50), new Border() { Background = Brushes.Black, Width = 200, Height = 500, }, true, null, (newPos) => { });
            Grid a = new Grid();
            a.Children.Add(new Rectangle()
            {
                Width = 50,
                Height = 50,
                Fill = Brushes.AliceBlue
            });
            InfiniteBoard.PlaceControl(new Point(-200, -500), new Border()
            {
                Background = Brushes.Black,
                Width = 200,
                Height = 500,
                Child = a
            }, true, a.Children[0]);
        }

        private void Button_EraseAll_Click(object sender, RoutedEventArgs e)
        {
            InfiniteBoard.ClearBoard();
        }
    }
}
