using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfInfiniteBoard
{
    public class MovingAround
    {
        private bool _isMoving;
        private double deltaX;
        private double deltaY;
        public TranslateTransform _currentTT;

        private Canvas Canvas;
        private Grid Conteneur;
        //private Cursor CursorGrabbing;

        private TransformGroup transformGroup = new TransformGroup();
        private InfiniteBoardControl InfiniteBoardControl;


        public void MovingAroundCanvasInit(Canvas canvas, Grid conteneur, InfiniteBoardControl infiniteBoardControl)
        {
            this.Canvas = canvas;
            InfiniteBoardControl = infiniteBoardControl;

            Conteneur = conteneur;
            //CursorGrabbing = cursor;

            transformGroup.Children.Add(new MatrixTransform());
            transformGroup.Children.Add(new TranslateTransform());

            Canvas.RenderTransform = transformGroup;

            canvas.PreviewMouseDown += Canvas_PreviewMouseDown;
            canvas.PreviewMouseUp += Canvas_PreviewMouseUp;
            canvas.PreviewMouseMove += Canvas_PreviewMouseMove;
            canvas.MouseWheel += Canvas_MouseWheel;

            // centre le canvas
            (canvas.RenderTransform as TransformGroup).Children[1] = new TranslateTransform(-canvas.Width / 2, -canvas.Width / 2);
            // applique la pos actuelle (centré)
            _currentTT = (Canvas.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
        }

        protected bool isDragging;
        private Point clickPosition;

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (InfiniteBoardControl.AllowUserToZoom)
            {
                var position = e.GetPosition(Canvas);
                var transform = (Canvas.RenderTransform as TransformGroup).Children[0] as MatrixTransform;
                var matrix = transform.Matrix;
                var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

                matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);
                (Canvas.RenderTransform as TransformGroup).Children[0] = new MatrixTransform(matrix);
            }
        }

        internal void Canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.MiddleButton == MouseButtonState.Pressed && InfiniteBoardControl.AllowUserToMoveAround)
                if (e.OriginalSource is Canvas )
                {
                    var mousePosition = Mouse.GetPosition(Conteneur);
                    deltaX = mousePosition.X;
                    deltaY = mousePosition.Y;

                    _isMoving = true;
                    //Canvas.Cursor = CursorGrabbing;
                }

        }

        internal void Canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _currentTT = (Canvas.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
            _isMoving = false;
            Canvas.Cursor = Cursors.Arrow; 
        }

        internal void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMoving) return;

            var mousePoint = Mouse.GetPosition(Conteneur);

            var offsetX = (_currentTT == null ? 0 : 0 - _currentTT.X) + deltaX - mousePoint.X;
            var offsetY = (_currentTT == null ? 0 : 0 - _currentTT.Y) + deltaY - mousePoint.Y;

            (this.Canvas.RenderTransform as TransformGroup).Children[1] = new TranslateTransform(-offsetX, -offsetY);
            
        }

        internal static void SetUpMovingAroundControl(UIElement control, UIElement? controlTrigger, Canvas MainCanvas, Action<Point> PosChange, Point CenterCell, Action<Point> isMoving)
        {
            bool isDragging = false;
            Point clickPosition;

            var movingTrigger = controlTrigger == null ? control : controlTrigger;


            movingTrigger.PreviewMouseLeftButtonDown += (sender, e) => {
                isDragging = true;
                var draggableControl = sender as UIElement;
                clickPosition = e.GetPosition(sender as UIElement);
                draggableControl.CaptureMouse();
            };
            movingTrigger.PreviewMouseLeftButtonUp += (sender, e) => {
                isDragging = false;
                var draggable = sender as UIElement;
                draggable.ReleaseMouseCapture();
            };
            movingTrigger.PreviewMouseMove += (sender, e) => {
                var draggableControl = sender as UIElement;

                if (isDragging && draggableControl != null)
                {
                    Point currentPosition = e.GetPosition(MainCanvas);


                    var transform = draggableControl.RenderTransform as TranslateTransform;
                    if (transform == null)
                    {
                        transform = new TranslateTransform();
                        draggableControl.RenderTransform = transform;
                    }

                    Point relativeLocation = new Point(0, 0);
                    if(controlTrigger != null)
                        relativeLocation = controlTrigger.TranslatePoint(new Point(0, 0), control); // pour que ça bouge depuis le controlTrigger

                    Canvas.SetLeft(control, (currentPosition.X - clickPosition.X) - relativeLocation.X);
                    Canvas.SetTop(control, (currentPosition.Y - clickPosition.Y) -relativeLocation.Y);
                    var newPos = new Point((CenterCell.X - Canvas.GetLeft(control)) * -1,( CenterCell.Y - Canvas.GetTop(control)) - 1);
                    PosChange(newPos);
                    if (isMoving != null)
                        isMoving(newPos);
                }
            };


        }
    }
}
