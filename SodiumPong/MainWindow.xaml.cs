using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Sodium;

namespace SodiumPong
{
    [Flags]
    internal enum GameState
    {
        Paused = 0,
        Running = 1
    }

    public partial class MainWindow : Window
    {
        #region constants
        const int dxBoard = 600;
        const int dyBoard = 400;
        const int dxBoardHalf = dxBoard / 2;
        const int dyBoardHalf = dyBoard / 2;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.cvs.Width = dxBoard;
            this.cvs.Height = dyBoard;
            Rectangle viewP1 = RectanglePlayer();
            Rectangle viewP2 = RectanglePlayer();
            Ellipse viewBall = EllipseBall();
            Rectangle midLine = MidLine();

            TextBlock textScore1 = TextScore();
            TextBlock textScore2 = TextScore();
            TextBlock textRunPause = TextRunPause();

            this.cvs.Children.Add(midLine);
            this.cvs.Children.Add(textScore1);
            this.cvs.Children.Add(textScore2);
            this.cvs.Children.Add(textRunPause);
            this.cvs.Children.Add(viewBall);
            this.cvs.Children.Add(viewP1);
            this.cvs.Children.Add(viewP2);

            Canvas.SetLeft(midLine, dxBoardHalf);
            Canvas.SetTop(midLine, 0);

            Canvas.SetLeft(textScore1, dxBoardHalf - dxBoardHalf / 2);
            Canvas.SetTop(textScore1, 30);

            Canvas.SetLeft(textScore2, dxBoardHalf + dxBoardHalf / 2);
            Canvas.SetTop(textScore2, 30);

            Canvas.SetLeft(textRunPause, dxBoardHalf);
            Canvas.SetTop(textRunPause, dyBoard - 30);

            Transaction.RunVoid(() =>
            {
                StreamSink<KeyEventArgs> sKeyEvents = new StreamSink<KeyEventArgs>();

                KeyDown += (sender, args) => sKeyEvents.Send(args);
                KeyUp += (sender, args) => sKeyEvents.Send(args);

                StreamSink<RenderingEventArgs> sRenderEvents = new StreamSink<RenderingEventArgs>();
                CompositionTarget.Rendering += (sender, args) => sRenderEvents.Send((RenderingEventArgs)args);

                Stream<TimeSpan> sTicks = sRenderEvents.Collect(TimeSpan.Zero,
                    (re, t) => Tuple.Create(re.RenderingTime - t, re.RenderingTime));

                Cell<int> cDirP1 = CellDirPlayer(sKeyEvents, Key.W, Key.S);
                Cell<int> cDirP2 = CellDirPlayer(sKeyEvents, Key.Up, Key.Down);

                Cell<GameState> cGameState = sKeyEvents.Filter(e => e.Key == Key.Space).Accum(GameState.Paused, UpdateGameState);

                Cell<GameObject> cP1 = CellPlayer(PlayerInitial(new Point(20, dyBoardHalf), viewP1), sTicks, cDirP1, cGameState);
                Cell<GameObject> cP2 = CellPlayer(PlayerInitial(new Point(dxBoard - 20, dyBoardHalf), viewP2), sTicks, cDirP2, cGameState);

                CellLoop<GameObject> cBall = new CellLoop<GameObject>();
                Stream<GameObject> sBall = sTicks.Snapshot(cBall, cP1, cP2, cGameState, UpdateBall);
                Cell<int> cScore1 = sBall.Accum(0, (ball, score) => ball.pos.X > dxBoard ? ++score : score);
                Cell<int> cScore2 = sBall.Accum(0, (ball, score) => ball.pos.X <= 0 ? ++score : score);
                cBall.Loop(sBall.Hold(BallInitial(viewBall)));

                cP1.Listen(Draw);
                cP2.Listen(Draw);
                cBall.Listen(Draw);

                cScore1.Listen(score =>
                {
                    textScore1.Text = score.ToString();
                });

                cScore2.Listen(score =>
                {
                    textScore2.Text = score.ToString();
                });

                cGameState.Listen(gameState =>
                {
                    textRunPause.Visibility = gameState == GameState.Paused ? Visibility.Visible : Visibility.Hidden;
                });
            });
        }

        private static Cell<int> CellDirPlayer(Stream<KeyEventArgs> sKeyEvents, Key keyUp, Key keyDown)
        {
            Stream<int> sDirP1Left = sKeyEvents.Filter(e => e.Key == keyUp).Map(e => e.IsDown ? -1 : 0);
            Stream<int> sDirP1Right = sKeyEvents.Filter(e => e.Key == keyDown).Map(e => e.IsDown ? 1 : 0);
            return sDirP1Left.OrElse(sDirP1Right).Hold(0);
        }

        private static GameState UpdateGameState(KeyEventArgs e, GameState gameState)
        {
            return e.IsDown ? GameState.Running & ~gameState : gameState;
        }

        private static Cell<GameObject> CellPlayer(GameObject playerInitial, Stream<TimeSpan> sTicks, Cell<int> cDir, Cell<GameState> cGameState)
        {
            CellLoop<GameObject> cPlayer = new CellLoop<GameObject>();
            Stream<GameObject> sPlayer = sTicks.Snapshot(cPlayer, cDir, cGameState, UpdatePlayer);
            cPlayer.Loop(sPlayer.Hold(playerInitial));
            return cPlayer;
        }

        private static GameObject UpdatePlayer(TimeSpan timeSpan, GameObject player, int dir, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return player;

            return Clamp(PhysicsUpdate(timeSpan, PlayerVeloUpdate(player, dir)));
        }

        private static GameObject Clamp(GameObject player)
        {
            return new GameObject(new Point(player.pos.X, player.pos.Y.Clamp(25, 375)), player.vel, player.view);
        }

        private static GameObject PlayerVeloUpdate(GameObject player, int dir)
        {
            return new GameObject(player.pos, new Vector(0, 200 * dir), player.view);
        }

        private static GameObject PhysicsUpdate(TimeSpan timeSpan, GameObject gameObject)
        {
            return new GameObject(gameObject.pos + gameObject.vel * timeSpan.TotalSeconds, gameObject.vel, gameObject.view);
        }

        private static GameObject UpdateBall(TimeSpan timeSpan, GameObject ball, GameObject p1, GameObject p2, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return ball;

            if (!Within(-1, dxBoard, (int)ball.pos.X))
                return new GameObject(new Point(dxBoardHalf, dyBoardHalf), ball.vel, ball.view);

            return PhysicsUpdate(timeSpan, BallVeloUpdate(ball, p1, p2));
        }

        private static GameObject BallVeloUpdate(GameObject ball, GameObject p1, GameObject p2)
        {
            return new GameObject(ball.pos,
                new Vector(
                    StepV(ball.vel.X, Within(p2, ball, -10), Within(p1, ball, 10)),
                    StepV(ball.vel.Y, ball.pos.Y > dyBoard - 5, ball.pos.Y < 5)), 
                ball.view);
        }

        private static double StepV(double v, bool lowerCollision, bool upperCollision)
        {
            if (lowerCollision)
                return -Math.Abs(v);

            if (upperCollision)
                return Math.Abs(v);

            return v;
        }

        private static bool Within(GameObject p, GameObject b, int xOffset)
        {
            int tmp = (int)(p.pos.X + xOffset);
            int right = (int)p.pos.X;
            int left = tmp < right ? tmp : right;
            right = tmp > right ? tmp : right;

            return Within(left, right, (int)b.pos.X) &&
                   Within((int)(p.pos.Y - 27.5), (int)(p.pos.Y + 27.5), (int)b.pos.Y);
        }

        private static bool Within(int left, int right, int n)
        {
            return left < n && n < right;
        }

        private static void Draw(GameObject gameObject)
        {
            Canvas.SetLeft(gameObject.view, gameObject.pos.X);
            Canvas.SetTop(gameObject.view, gameObject.pos.Y);
        }

        private static Rectangle RectanglePlayer()
        {
            return new Rectangle
            {
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.0,
                Width = 15,
                Height = 50,
                RenderTransform = new TranslateTransform(-7.5, -25)
            };
        }

        private static Ellipse EllipseBall()
        {
            return new Ellipse
            {
                Fill = new SolidColorBrush(Colors.White),
                Width = 10,
                Height = 10,
                RenderTransform = new TranslateTransform(-5, -5)
            };
        }

        private static Rectangle MidLine()
        {
            return new Rectangle
            {
                Fill = Brushes.SlateGray,
                Width = 2,
                Height = dyBoard,
                RenderTransform = new TranslateTransform(-1, 0)
            };
        }

        private static TextBlock TextScore()
        {
            return new TextBlock
            {
                Foreground = Brushes.SlateGray,
                FontSize = 32
            };
        }

        private static TextBlock TextRunPause()
        {
            var textBlock = new TextBlock
            {
                Foreground = Brushes.White,
                Text = "SPACE to run and pause, W/S and \u2191\u2193 to move",
                TextAlignment = TextAlignment.Center,
                Width = 300
            };
            textBlock.RenderTransform = new TranslateTransform(-textBlock.Width / 2, 0);
            return textBlock;
        }

        private static GameObject PlayerInitial(Point pointInitial, UIElement view)
        {
            return new GameObject(pointInitial, new Vector(0, 0), view);
        }

        private static GameObject BallInitial(UIElement view)
        {
            return new GameObject(new Point(dxBoardHalf, dyBoardHalf), new Vector(200, -200), view);
        }

        internal class GameObject
        {
            internal readonly Point pos;
            internal readonly Vector vel;
            internal readonly UIElement view;

            public GameObject(Point pos, Vector vel, UIElement view)
            {
                this.pos = pos;
                this.vel = vel;
                this.view = view;
            }
        }
    }
}
