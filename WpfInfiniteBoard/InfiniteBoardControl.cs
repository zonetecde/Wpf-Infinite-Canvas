 using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfInfiniteBoard
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfInfiniteBoard"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfInfiniteBoard;assembly=WpfInfiniteBoard"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:InfiniteBoardControl/>
    ///
    /// </summary>
    public class InfiniteBoardControl : Control
    {
        private int CANVAS_SIZE = 10_000_000;

        private Point CenterCell { get; set; }
        private Border Cell { get; set; }

        private int cellSize = 40;

        private List<InfiniteBorderChild> InfiniteCanvasChildrent = new List<InfiniteBorderChild>();

        [Description("Cell's size"), Category("Apparence")]
        public int CellSize
        {
            get => cellSize;
            set => cellSize = value;
        }

        private double borderThickness = 0.5;

        [Description("Cell's border thickness"), Category("Apparence"), DefaultValue(50.0)]
        public double BorderThickness
        {
            get => borderThickness;
            set => borderThickness = value;
        }

        private bool allowPlaceCells = true;

        [Description("Allow to place cell?"), Category("Apparence")]
        public bool AllowPlaceCells
        {
            get => allowPlaceCells;
            set => allowPlaceCells = value;
        }

        private Brush placedCellBorderBrush = Brushes.Black;

        [Description("Placed cell's border color"), Category("Apparence")]
        public Brush PlacedCellBorderBrush
        {
            get => placedCellBorderBrush;
            set => placedCellBorderBrush = ((Brush)BrushConverter.ConvertFromString(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty)));
        }

        private Brush placedCellBackground = Brushes.Yellow;

        [Description("Color of placed cell background"), Category("Apparence")]
        public Brush PlacedCellBackground
        {
            get => placedCellBackground;
            set => placedCellBackground = ((Brush)BrushConverter.ConvertFromString(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty)));
        } 
        
        private bool placedCellHaveBorder = true;

        [Description("Does placed cell have a border"), Category("Apparence")]
        public bool PlacedCellHaveBorder
        {
            get => placedCellHaveBorder;
            set => placedCellHaveBorder = value;
        }

        BrushConverter BrushConverter = new BrushConverter();

        static InfiniteBoardControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfiniteBoardControl), new FrameworkPropertyMetadata(typeof(InfiniteBoardControl)));       
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoquer quand une nouvelle cellule est ajouté")]
        public event EventHandler<InfiniteBorderChild> CellulAdded;

        private Canvas CanvasMain;

        public override void OnApplyTemplate()
        {
            CanvasMain = GetTemplateChild("Canvas_Main") as Canvas;
            CanvasMain.Width = CANVAS_SIZE;
            CanvasMain.Height = CANVAS_SIZE;

            ApplyCellSize();
            ApplyCellBackgroundAndBorderColor(this.Background, this.Foreground);
            ApplyBorderThickness(borderThickness);

            Cell = new Border()
            {
                Width = cellSize,
                Height = cellSize,
                Background = placedCellBackground,
            };

            Cell.BorderBrush = placedCellBorderBrush;
            Cell.BorderThickness = placedCellHaveBorder == true ? Cell.BorderThickness = new Thickness(borderThickness) : new Thickness(0);

            this.Loaded += (sender, e) =>
            {
                Grid? GridConteneur = GetTemplateChild("Grid_Conteneur") as Grid;

                MovingAround ma = new MovingAround();
                ma.MovingAroundCanvasInit(CanvasMain, GridConteneur);

                // Calcul la position de la cellule au centre du Canvas
                // . Calcul du multiple le plus proche de CellSize par la width du canvas / 2
                int value = CANVAS_SIZE / 2;
                int factor = cellSize;

                int nearestMultiple = // donne la pos top left du milieu du canvas (actuellement affiché à l'écran)
                        NeareastMultiple(value, factor);

                // Ajoute la moitié de l'écran actuelle
                CenterCell = new Point(
                     nearestMultiple + NeareastMultiple(Convert.ToInt32(this.ActualWidth / 2), factor) - cellSize,
                      nearestMultiple + NeareastMultiple(Convert.ToInt32(this.ActualHeight / 2), factor) - cellSize
                    );

                CanvasMain.MouseDown += Canvas_Main_MouseDown;
                CanvasMain.MouseMove += Canvas_Main_MouseMove;
            };


        }

        private void Canvas_Main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (allowPlaceCells)
            {
                // place une cellule là où on a cliqué
                int posX, posY;
                GetCoordinateWhereToPlace(out posX, out posY);

                // vérifie que aucun n'est déjà placé là
                if (!DoesAnyCellsAlreadyExistHere(posX, posY))
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                        // Place une nouvelle cell
                        AddCell(posX, posY);
                }
                else if (e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)
                {
                    EraseCellAtCoordinate(posX, posY);
                }
            }
        }


        private void Canvas_Main_MouseMove(object sender, MouseEventArgs e)
        {
            // Passe en mode "dessine"
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (allowPlaceCells)
                {
                    // place une cellule là où on a cliqué
                    int posX, posY;
                    GetCoordinateWhereToPlace(out posX, out posY);

                    // vérifie que aucun n'est déjà placé là
                    if (!DoesAnyCellsAlreadyExistHere(posX, posY))
                    {
                        // Place une nouvelle cell
                        AddCell(posX, posY);
                    }
                }
            }
            // eraser
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                try
                {
                    // place une cellule là où on a cliqué
                    int posX, posY;
                    GetCoordinateWhereToPlace(out posX, out posY);

                    // vérifie que aucun n'est déjà placé là
                    if (DoesAnyCellsAlreadyExistHere(posX, posY))
                    {
                        EraseCellAtCoordinate(posX, posY);
                    }

                }
                catch { }
            }
        }


        private void EraseCellAtCoordinate(int posX, int posY)
        {
            InfiniteBorderChild? cell = GetCellAtCoordinate(posX, posY);
            EraseCell(cell);
        }

        private InfiniteBorderChild? GetCellAtCoordinate(int posX, int posY)
        {
            // erase la cell
            return InfiniteCanvasChildrent.FirstOrDefault(x => Convert.ToInt32(Canvas.GetLeft(x.Children)) == posX && Convert.ToInt32(Canvas.GetTop(x.Children)) == posY);
        }

        private void EraseCell(InfiniteBorderChild? cell)
        {
            CanvasMain.Children.Remove(cell.Children);
            InfiniteCanvasChildrent.Remove(cell);
        }

        private bool DoesAnyCellsAlreadyExistHere(int posX, int posY)
        {
            return InfiniteCanvasChildrent.Any(x => Convert.ToInt32(Canvas.GetLeft(x.Children)) == posX && Convert.ToInt32(Canvas.GetTop(x.Children)) == posY);
        }

        private void GetCoordinateWhereToPlace(out int posX, out int posY)
        {
            var mousePosition = Mouse.GetPosition(CanvasMain);
            posX = NeareastMultiple(Convert.ToInt32(mousePosition.X), cellSize);
            posY = NeareastMultiple(Convert.ToInt32(mousePosition.Y), cellSize);
        }

        public void PlaceCell(int xFromOrigin, int yFromOrigin)
        {
            if (!DoesAnyCellsAlreadyExistHere(Convert.ToInt32(CenterCell.X) + (xFromOrigin * cellSize), Convert.ToInt32(CenterCell.Y) + (yFromOrigin * cellSize)))
                AddCell(Convert.ToInt32(CenterCell.X) + (xFromOrigin * cellSize), Convert.ToInt32(CenterCell.Y) + (yFromOrigin * cellSize));
        }

        public List<InfiniteBorderChild> GetAllChildren()
        {
            return InfiniteCanvasChildrent;
        }

        private void ApplyCellSize()
        {
            (GetTemplateChild("Rectangle_cell") as Rectangle).Width = cellSize;
            (GetTemplateChild("Rectangle_cell") as Rectangle).Height = cellSize;
            (GetTemplateChild("visualBrush_cell") as VisualBrush).Viewport = new Rect(0, 0, cellSize, cellSize);
            (GetTemplateChild("visualBrush_cell") as VisualBrush).Viewbox = new Rect(0, 0, cellSize, cellSize);
        }


        private void ApplyCellBackgroundAndBorderColor(Brush background, Brush border)
        {
            (GetTemplateChild("Rectangle_cell") as Rectangle).Fill = background;
            (GetTemplateChild("Rectangle_cell") as Rectangle).Stroke = border;
        }

        private void ApplyBorderThickness(double borderThickness)
        {
            (GetTemplateChild("Rectangle_cell") as Rectangle).StrokeThickness = borderThickness;
        }

        private static int NeareastMultiple(int value, int factor)
        {
            return (int)Math.Round(
                    (value / (double)factor),
                    MidpointRounding.ToZero
                ) * factor;
        }

        private void AddCell(int posX, int posY)
        {
            Border copy = XamlReader.Parse(XamlWriter.Save(Cell)) as Border;
            InfiniteCanvasChildrent.Add(new InfiniteBorderChild(

                     NeareastMultiple(Convert.ToInt32(((posX - CenterCell.X))), CellSize) / CellSize,
                     NeareastMultiple(Convert.ToInt32(((posY - CenterCell.Y))), CellSize) / CellSize,

                copy));

            CanvasMain.Children.Add(copy);
            Canvas.SetLeft(copy, posX);
            Canvas.SetTop(copy, posY);

            CellulAdded(this, InfiniteCanvasChildrent.Last());
        }
    }

    public class InfiniteBorderChild : EventArgs
    {
        public InfiniteBorderChild(int xfromOrigin, int yfromOrigin, Border children)
        {
            XfromOrigin = xfromOrigin;
            YfromOrigin = yfromOrigin;
            Children = children;
        }

        public int XfromOrigin { get; }
        public int YfromOrigin { get; }
        public Border Children { get; }
    }
}
