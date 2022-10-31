using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
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

        internal Point CenterCell { get; set; }
        private Rectangle CellTemplate { get; set; }

        private long CellCounter = 0;

        private Dictionary<Point, Rectangle> InfiniteCanvasChildren_Cell = new Dictionary<Point, Rectangle>();
        private List<PlacedControl> InfiniteCanvasChildren_Control = new List<PlacedControl>();

        /// <summary>
        /// La taille des cases
        /// </summary>
        private int cellSize = 40;
        [Description("Cell's size"), Category("Apparence")]
        public int CellSize
        {
            get => cellSize;
            set => cellSize = value;
        }

        /// <summary>
        /// La bordure des cases
        /// </summary>
        private double borderThickness = 0.5;
        [Description("Cell's border thickness"), Category("Apparence"), DefaultValue(50.0)]
        public double BorderThickness
        {
            get => borderThickness;
            set => ChangeBorderThickness(value);
        }

        /// <summary>
        /// Est-ce que l'utilisateur peut placer des cases
        /// </summary>
        private bool allowUserToPlaceCells = true;
        [Description("Allow user to place cell?"), Category("Apparence")]
        public bool AllowUserToPlaceCells
        {
            get => allowUserToPlaceCells;
            set => allowUserToPlaceCells = value;
        }

        /// <summary>
        /// Est-ce que l'utilisateur peut naviguer dans le contrôle ?
        /// </summary>
        private bool allowUserToMoveAround = true;
        [Description("Allow to move around the board?"), Category("Apparence")]
        public bool AllowUserToMoveAround
        {
            get => allowUserToMoveAround;
            set => allowUserToMoveAround = value;
        }

        /// <summary>
        /// Est-ce que l'utilisateur peut zoomer dans le contrôle ?
        /// </summary>
        private bool allowUserToZoom = true;
        [Description("Allow to zoom in and out?"), Category("Apparence")]
        public bool AllowUserToZoom
        {
            get => allowUserToZoom;
            set =>  allowUserToZoom = value;
        }

        /// <summary>
        /// Couleur de la bordure des cases placées
        /// </summary>
        private Brush placedCellBorderBrush = Brushes.Black;
        [Description("Placed cell's border color"), Category("Apparence")]
        public Brush PlacedCellBorderBrush
        {
            get => placedCellBorderBrush;
            set => placedCellBorderBrush = ((Brush)BrushConverter.ConvertFromString(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty)));
        }

        /// <summary>
        /// Couleur des cases placées
        /// </summary>
        private Brush placedCellBackground = Brushes.Yellow;
        [Description("Color of placed cell background"), Category("Apparence")]
        public Brush PlacedCellBackground
        {
            get => placedCellBackground;
            set => placedCellBackground = ((Brush)BrushConverter.ConvertFromString(value.ToString().Replace("{", string.Empty).Replace("}", string.Empty)));
        } 
        
        /// <summary>
        /// Est-ce que les cases placé ont une bordure?
        /// </summary>
        private bool? placedCellHaveBorder = true;
        [Description("Does placed cell have a border"), Category("Apparence")]
        public bool? PlacedCellHaveBorder
        {
            get => placedCellHaveBorder;
            set => ChangeDoesPlacedCellHaveBorder(value);
        } 
        
        /// <summary>
        /// Afficher le quadrillage?
        /// </summary>
        private bool? showQuartering = true;
        [Description("Is the quartering visible ?"), Category("Apparence")]
        public bool? ShowQuartering
        {
            get => showQuartering;
            set => ChangeShowQuartering(value);
        }

        private void ChangeShowQuartering(bool? value)
        {
            showQuartering = value;

            try
            {
                if (showQuartering == false)
                {
                    (GetTemplateChild("Grid_Conteneur") as Grid).Background = Background;
                    CanvasMain.Background.Opacity = 0;
                }
                else
                {
                    CanvasMain.Background.Opacity = 1;
                }
            }
            catch { }
        }

        /// <summary>
        /// Change le paramètre "placedCellHaveBorder" visuellement et dans le code
        /// </summary>
        /// <param name="value">Oui/Non</param>
        private void ChangeDoesPlacedCellHaveBorder(bool? value)
        {
            placedCellHaveBorder = value;

            if(CellTemplate != null)
                CellTemplate.StrokeThickness = placedCellHaveBorder == false ? 0 : borderThickness;
        }

        /// <summary>
        /// Event appelé lorsqu'une case est ajouté
        /// </summary>
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoquer quand une nouvelle case est ajoutée")]
        public event EventHandler<Rectangle> CellAdded;

        // Convertisseur du code hex en Brush
        BrushConverter BrushConverter = new BrushConverter();

        /// <summary>
        /// Constructeur
        /// </summary>
        static InfiniteBoardControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfiniteBoardControl), new FrameworkPropertyMetadata(typeof(InfiniteBoardControl)));       
        }

        // Canvas principal
        internal Canvas CanvasMain;

        // MovingAround object
        private MovingAround Ma = new MovingAround();

        /// <summary>
        /// Contrôle loaded
        /// </summary>
        public override void OnApplyTemplate()
        {
            // Récupère la référence de Canvas_Main
            CanvasMain = GetTemplateChild("Canvas_Main") as Canvas;

            // Set la width & height du Canvas principal 
            CanvasMain.Width = CANVAS_SIZE;
            CanvasMain.Height = CANVAS_SIZE;

            // Set un template d'une case posée dans le board
            CellTemplate = new Rectangle()
            {
                Width = cellSize + 0.1, 
                Height = cellSize + 0.1,
                Fill = placedCellBackground,
            };

            // Application des propriétés 
            ApplyCellSize();
            ChangeBackgroundAndBorderColor(this.Background, this.Foreground);
            ChangeBorderThickness(borderThickness);
            ChangeShowQuartering(showQuartering);
            CellTemplate.Stroke = placedCellBorderBrush;
            CellTemplate.StrokeThickness = placedCellHaveBorder == true ? borderThickness : 0;

            // Quand tout est loaded avec ActualHeight & ActualWidth avec une valeur.
            this.Loaded += (sender, e) =>
            {
                // Ref au Grid_Conteneur
                Grid? GridConteneur = GetTemplateChild("Grid_Conteneur") as Grid;

                // Init Moving Around object
                Ma.MovingAroundCanvasInit(CanvasMain, GridConteneur, this);

                // Calcul la position de la cellule au centre du Canvas
                // Calcul du multiple le plus proche de CellSize par la width du canvas / 2
                int value = CANVAS_SIZE / 2;
                int factor = cellSize;

                int nearestMultiple = // donne la pos top left du milieu du canvas (actuellement affiché à l'écran)
                        NeareastMultiple(value, factor);

                // Ajoute la moitié de l'écran actuelle
                CenterCell = new Point(
                     nearestMultiple + NeareastMultiple(Convert.ToInt32(this.ActualWidth / 2), factor) - cellSize,
                      nearestMultiple + NeareastMultiple(Convert.ToInt32(this.ActualHeight / 2), factor) - cellSize
                    );

                // Event
                CanvasMain.MouseDown += Canvas_Main_MouseDown;
                CanvasMain.MouseMove += Canvas_Main_MouseMove;
            };
        }

        /// <summary>
        /// Après un clique sur le contrôle,
        /// Ajoute une case ou supprime une case si l'ont est autorisé à le faire
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_Main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (allowUserToPlaceCells && Mouse.DirectlyOver == sender) // Sinon est sur un contrôle dans le Canvas
            {
                // place une cellule là où on a cliqué
                int posX, posY;
                GetCoordinateWhereToPlace(out posX, out posY);

                // vérifie que aucun n'est déjà placé là
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Place une nouvelle cell
                    AddCell(posX, posY);
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    EraseCellAtCoordinate(posX, posY);
                }
            }
        }

        /// <summary>
        /// Ajoute/Supprime toutes les cases en "dessinant"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_Main_MouseMove(object sender, MouseEventArgs e)
        {
            // Passe en mode "dessine"
            if (e.LeftButton == MouseButtonState.Pressed && allowUserToPlaceCells && Mouse.DirectlyOver == sender)
            {
                
                // place une cellule là où on a cliqué
                int posX, posY;
                GetCoordinateWhereToPlace(out posX, out posY);

                // vérifie que aucun n'est déjà placé là

                // Place une nouvelle cell
                AddCell(posX, posY);
                
                
            }
            // eraser
            else if (e.RightButton == MouseButtonState.Pressed && allowUserToPlaceCells)
            {
                try
                {
                    // place une cellule là où on a cliqué
                    int posX, posY;
                    GetCoordinateWhereToPlace(out posX, out posY);

                    // vérifie que aucun n'est déjà placé là

                    EraseCellAtCoordinate(posX, posY);
                    

                }
                catch { }
            }
        }

        /// <summary>
        /// Supprime la cellule aux coordonnées du Canvas
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        private void EraseCellAtCoordinate(int posX, int posY)
        {
            Point co = CoordinateToCoordinateFromOrigin(posX, posY);

            EraseCell(co.X, co.Y);
        }

        /// <summary>
        /// Get la case qui est dans les coordonnées à l'origine
        /// </summary>
        /// <param name="xFromOrigin"></param>
        /// <param name="yFromOrigin"></param>
        /// <returns></returns>
        private Rectangle? GetCellAtCoordinate(double xFromOrigin, double yFromOrigin)
        {
            return InfiniteCanvasChildren_Cell[new Point(xFromOrigin, yFromOrigin)];
        }

        /// <summary>
        /// Supprime la case aux coordonnées à l'origine
        /// </summary>
        /// <param name="xFromOrigin"></param>
        /// <param name="yFromOrigin"></param>
        public void EraseCell(double xFromOrigin, double yFromOrigin)
        {
            Rectangle b;
            if (InfiniteCanvasChildren_Cell.TryGetValue(new Point(xFromOrigin, yFromOrigin), out b))
            {
                CanvasMain.Children.Remove(b);
                InfiniteCanvasChildren_Cell.Remove(
                    new Point(xFromOrigin, yFromOrigin)
                );
            }

        }

        /// <summary>
        /// Est-ce que une case existe dans ses coordonné (par apport à l'origine)
        /// </summary>
        /// <returns></returns>
        public bool DoesAnyCellsAlreadyExistHere(int xFromOrigin, int yFromOrigin)
        {
            return InfiniteCanvasChildren_Cell.ContainsKey(new Point(xFromOrigin, yFromOrigin));
        }

        /// <summary>
        /// Convertis les coordonnées relatives en coordonnées à l'origine
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        private void GetCoordinateWhereToPlace(out int posX, out int posY)
        {
            var mousePosition = Mouse.GetPosition(CanvasMain);
            posX = NeareastMultiple(Convert.ToInt32(mousePosition.X), cellSize);
            posY = NeareastMultiple(Convert.ToInt32(mousePosition.Y), cellSize);
        }

        /// <summary>
        /// Place une cellule par apport aux coordonnées (à l'origine)
        /// </summary>
        /// <param name="xFromOrigin"></param>
        /// <param name="yFromOrigin"></param>
        public void PlaceCell(int xFromOrigin, int yFromOrigin)
        {
            int x = (int)CenterCell.X + (xFromOrigin * cellSize);
            int y = (int)CenterCell.Y + (yFromOrigin * cellSize);

            AddCell(x, y);
        }
        
        /// <summary>
        /// Retourne toutes les cases placées
        /// </summary>
        /// <returns></returns>
        public Dictionary<Point, Rectangle> GetAllColouredCell()
        {
            return InfiniteCanvasChildren_Cell;
        }

        /// <summary>
        /// Retourne tous les contrôles placés
        /// </summary>
        /// <returns></returns>
        public List<PlacedControl> GetAllControl()
        {
            return InfiniteCanvasChildren_Control;
        }

        /// <summary>
        /// Applique la taille des cellules
        /// </summary>
        private void ApplyCellSize()
        {
            (GetTemplateChild("Rectangle_cell") as Rectangle).Width = cellSize;
            (GetTemplateChild("Rectangle_cell") as Rectangle).Height = cellSize;
            (GetTemplateChild("visualBrush_cell") as VisualBrush).Viewport = new Rect(0, 0, cellSize, cellSize);
            (GetTemplateChild("visualBrush_cell") as VisualBrush).Viewbox = new Rect(0, 0, cellSize, cellSize);
        }

        /// <summary>
        /// Applique la couleur du contrôle et de son quadrillage 
        /// </summary>
        /// <param name="background"></param>
        /// <param name="border"></param>
        public void ChangeBackgroundAndBorderColor(Brush background, Brush border)
        {
            (GetTemplateChild("Rectangle_cell") as Rectangle).Fill = background;
            (GetTemplateChild("Rectangle_cell") as Rectangle).Stroke = border;
        }

        /// <summary>
        /// Change l'épaisseur du quadrillage
        /// </summary>
        /// <param name="borderThickness"></param>
        private void ChangeBorderThickness(double borderThickness)
        {
            this.borderThickness = borderThickness;

            (GetTemplateChild("Rectangle_cell") as Rectangle).StrokeThickness = borderThickness;
            CellTemplate.StrokeThickness = borderThickness;
        }

        private static int NeareastMultiple(int value, int factor)
        {
            return (int)Math.Round(
                    (value / (double)factor),
                    MidpointRounding.ToZero
                ) * factor;
        }

        /// <summary>
        /// Ajoute une cellule aux coordonnées
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        private void AddCell(int posX, int posY)
        {
            Rectangle d = new Rectangle()
            {
                Width = cellSize + 0.1,
                Height = cellSize + 0.1,
                Fill = placedCellBackground,
                Stroke = placedCellBorderBrush,
                StrokeThickness = placedCellHaveBorder == true ? borderThickness :0,
                Tag = CellCounter
            };

            CellCounter++;

            if (InfiniteCanvasChildren_Cell.TryAdd(
                    CoordinateToCoordinateFromOrigin(posX, posY),
                d))
            {

                CanvasMain.Children.Add(d);
                Canvas.SetLeft(d, posX);
                Canvas.SetTop(d, posY);

                if (CellAdded != null)
                    CellAdded(this, d);
            }
            
        }

        /// <summary>
        /// Convertis des coordonnées relatives aux coordonnées à l'origine
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Point CoordinateToCoordinateFromOrigin(int x, int y)
        {
            return new Point(           
               (x - CenterCell.X) / CellSize,
                (y - CenterCell.Y) / CellSize
            );
        }

        /// <summary>
        /// Supprime toutes les cases placées
        /// </summary>
        public void ClearBoard()
        {
            CanvasMain.Children.Clear();
            InfiniteCanvasChildren_Cell.Clear();
            InfiniteCanvasChildren_Control.Clear();
        }

        /// <summary>
        /// Place un contrôle aux coordonnées donnés
        /// </summary>
        /// <param name="posFromorigin">Position du contrôle à partir du centre du contrôle</param>
        /// <param name="control">Le contrôle a ajouté</param>
        /// <param name="moveable">Est-ce que l'on peut déplacer le contrôle dans l'InfiniteBoard à l'aide d'un clique gauche ?</param>
        /// <param name="moveableTrigger">L'enfant du contrôle a cliqué pour pouvoir le déplacer, null = tout le contrôle</param>
        /// <returns>Réussite de l'opération (false = contrôle déjà placé quelque part)</returns>
        public bool PlaceControl(Point posFromorigin, UIElement control, bool moveable = true, UIElement moveableTrigger = null, Action<Point> IsMoving = null)
        {
            if (!InfiniteCanvasChildren_Control.Any(x => x.Control == control))
            {
                var placedControl = new PlacedControl(posFromorigin, control);
                InfiniteCanvasChildren_Control.Add(placedControl);

                CanvasMain.Children.Add(placedControl.Control);
                Canvas.SetLeft(placedControl.Control, CenterCell.X + posFromorigin.X);
                Canvas.SetTop(placedControl.Control, CenterCell.Y + posFromorigin.Y);

                if(moveable)
                {
                    MovingAround.SetUpMovingAroundControl(control, moveableTrigger, CanvasMain, (Point newPos) => placedControl.PositionFromOrigin = newPos, CenterCell, IsMoving);              
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Change la position d'un contrôle 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="newPosFromOrigin"></param>
        /// <returns>Réussite de l'opération</returns>
        public bool ChangeControlPosition(UIElement control, Point newPosFromOrigin)
        {
            var temp = InfiniteCanvasChildren_Control.FirstOrDefault(x => x.Control == control);

            if (temp != default)
            {
                Canvas.SetLeft(temp.Control, CenterCell.X + newPosFromOrigin.X);
                Canvas.SetTop(temp.Control, CenterCell.Y + newPosFromOrigin.Y);
                temp.PositionFromOrigin = newPosFromOrigin;
                return true;
            }
            else
                return false;
        }
        
        /// <summary>
        /// Renvois la position du contrôle donné
        /// </summary>
        /// <param name="control"></param>
        /// <returns>Renvois 0;0 si pas trouvé</returns>
        public Point GetControlPosition(UIElement control)
        {
            var temp = InfiniteCanvasChildren_Control.FirstOrDefault(x => x.Control == control);

            if (temp != default)
            {
                return temp.PositionFromOrigin;
            }
            else
                return new Point(0,0);
        }

        /// <summary>
        /// Supprime le contrôle aux coordonnées donnés. 
        /// </summary>
        /// <param name="posFromOrigin"></param>
        /// <returns>Réussite de l'opération (false = aucun contrôle à ses coordonnées)</returns>
        public bool EraseControlFromCoordinate(Point posFromOrigin)
        {
            var temp = InfiniteCanvasChildren_Control.FirstOrDefault(x => x.PositionFromOrigin == posFromOrigin);
            if (temp != default)
            {
                CanvasMain.Children.Remove(temp.Control);
                InfiniteCanvasChildren_Control.Remove(temp);
                return true;
            }
            else
                return false;
        }
        
        /// <summary>
        /// Supprime le contrôle données
        /// </summary>
        /// <param name="posFromOrigin"></param>
        /// <returns>Réussite de l'opération (false = aucun contrôle à ses coordonnées)</returns>
        public bool EraseControl(UIElement control)
        {
            var temp = InfiniteCanvasChildren_Control.FirstOrDefault(x => x.Control == control);
            if (temp != default)
            {
                CanvasMain.Children.Remove(temp.Control);
                InfiniteCanvasChildren_Control.Remove(temp);
                return true;
            }
            else
                return false;
        }
    }

    public class PlacedControl
    {
        public PlacedControl(Point positionFromOrigin, UIElement control)
        {
            PositionFromOrigin = positionFromOrigin;
            Control = control;
        }

        public Point PositionFromOrigin { get; set; }
        public UIElement Control { get; set; }
    }
}
