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

        private readonly StreamSink<KeyEventArgs> sKeyEvents = new StreamSink<KeyEventArgs>();
        private readonly StreamSink<RenderingEventArgs> sRenderEvents = new StreamSink<RenderingEventArgs>();

        public MainWindow()
        {
            InitializeComponent();
            this.cvs.Width = dxBoard;
            this.cvs.Height = dyBoard;
            Rectangle rectP1 = RectanglePlayer();
            Rectangle rectP2 = RectanglePlayer();
            Ellipse ellipseBall = EllipseBall();
            Rectangle midLine = MidLine();

            TextBlock textScore1 = TextScore();
            TextBlock textScore2 = TextScore();
            TextBlock textRunPause = TextRunPause();

            this.cvs.Children.Add(midLine);
            this.cvs.Children.Add(textScore1);
            this.cvs.Children.Add(textScore2);
            this.cvs.Children.Add(textRunPause);
            this.cvs.Children.Add(ellipseBall);
            this.cvs.Children.Add(rectP1);
            this.cvs.Children.Add(rectP2);

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
                KeyDown += (sender, args) => sKeyEvents.Send(args);
                KeyUp += (sender, args) => sKeyEvents.Send(args);

                CompositionTarget.Rendering += (sender, args) => sRenderEvents.Send((RenderingEventArgs)args);

                Stream<TimeSpan> sTicks = sRenderEvents.Collect(TimeSpan.Zero,
                    (re, t) => Tuple.Create(re.RenderingTime - t, re.RenderingTime));

                Cell<GameState> cGameState = sKeyEvents.Accum(GameState.Paused, (keyEventArgs, gameState) =>
                {
                    if (keyEventArgs.Key == Key.Space && keyEventArgs.IsDown)
                        return GameState.Running & ~gameState;

                    return gameState;
                });

                Cell<GameObject> cP1 = CellPlayer(PlayerInitial(new Point(20, dyBoardHalf)), sTicks, Key.W,
                    Key.S, cGameState);

                Cell<GameObject> cP2 = CellPlayer(PlayerInitial(new Point(dxBoard - 20, dyBoardHalf)), sTicks,
                    Key.Up, Key.Down, cGameState);

                CellLoop<GameObject> cBall = new CellLoop<GameObject>();
                Stream<GameObject> sBall = sTicks.Snapshot(cBall, cP1, cP2, cGameState, UpdateBall);
                Cell<int> cScore1 = sBall.Accum(0, (ball, score) => ball.pos.X > dxBoard ? ++score : score);
                Cell<int> cScore2 = sBall.Accum(0, (ball, score) => ball.pos.X <= 0 ? ++score : score);
                cBall.Loop(sBall.Hold(BallInitial()));

                cP1.Listen(player =>
                {
                    Canvas.SetLeft(rectP1, player.pos.X);
                    Canvas.SetTop(rectP1, player.pos.Y);
                });

                cP2.Listen(player =>
                {
                    Canvas.SetLeft(rectP2, player.pos.X);
                    Canvas.SetTop(rectP2, player.pos.Y);
                });

                cBall.Listen(ball =>
                {
                    Canvas.SetLeft(ellipseBall, ball.pos.X);
                    Canvas.SetTop(ellipseBall, ball.pos.Y);
                });

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

        private Cell<GameObject> CellPlayer(GameObject playerInitial, Stream<TimeSpan> sTicks, Key keyUp, Key keyDown, Cell<GameState> cGameState)
        {
            CellLoop<GameObject> cPlayer = new CellLoop<GameObject>();
            Cell<int> cDir = sKeyEvents.Accum(0, (e, dir) => UpdateDir(dir, keyUp, keyDown, e));
            Stream<GameObject> sPlayer = sTicks.Snapshot(cPlayer, cDir, cGameState, UpdatePlayer);
            cPlayer.Loop(sPlayer.Hold(playerInitial));
            return cPlayer;
        }

        private GameObject PlayerInitial(Point pointInitial)
        {
            return new GameObject(pointInitial, new Vector(0, 0));
        }

        private static int UpdateDir(int dir, Key keyUp, Key keyDown, KeyEventArgs e)
        {
            if (e.Key != keyUp && e.Key != keyDown)
                return dir;

            if (e.IsDown)
                return e.Key == keyUp ? -1 : 1;

            return 0;
        }

        private GameObject UpdatePlayer(TimeSpan timeSpan, GameObject player, int dir, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return player;

            return Clamp(PhysicsUpdate(timeSpan, PlayerVeloUpdate(player, dir)));
        }

        private GameObject Clamp(GameObject player)
        {
            return new GameObject(new Point(player.pos.X, player.pos.Y.Clamp(25, 375)), player.vel);
        }

        private GameObject PlayerVeloUpdate(GameObject player, int dir)
        {
            return new GameObject(player.pos, new Vector(0, 200 * dir));
        }

        private GameObject PhysicsUpdate(TimeSpan timeSpan, GameObject gameObject)
        {
            return new GameObject(gameObject.pos + gameObject.vel * timeSpan.TotalSeconds, gameObject.vel);
        }

        private GameObject BallInitial()
        {
            return new GameObject(new Point(dxBoardHalf, dyBoardHalf), new Vector(200, -200));
        }

        private GameObject UpdateBall(TimeSpan timeSpan, GameObject ball, GameObject p1, GameObject p2, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return ball;

            if (!Within(-1, dxBoard, (int)ball.pos.X))
                return new GameObject(new Point(dxBoardHalf, dyBoardHalf), ball.vel);

            return PhysicsUpdate(timeSpan, BallVeloUpdate(ball, p1, p2));
        }

        private GameObject BallVeloUpdate(GameObject ball, GameObject p1, GameObject p2)
        {
            return new GameObject(ball.pos,
                new Vector(
                    StepV(ball.vel.X, Within(p2, ball, -10), Within(p1, ball, 10)),
                    StepV(ball.vel.Y, ball.pos.Y > dyBoard - 5, ball.pos.Y < 5)));
        }

        private double StepV(double v, bool lowerCollision, bool upperCollision)
        {
            if (lowerCollision)
                return -Math.Abs(v);

            if (upperCollision)
                return Math.Abs(v);

            return v;
        }

        private bool Within(GameObject p, GameObject b, int xOffset)
        {
            int tmp = (int)(p.pos.X + xOffset);
            int right = (int)p.pos.X;
            int left = tmp < right ? tmp : right;
            right = tmp > right ? tmp : right;

            return Within(left, right, (int)b.pos.X) &&
                   Within((int)(p.pos.Y - 27.5), (int)(p.pos.Y + 27.5), (int)b.pos.Y);
        }

        private bool Within(int left, int right, int n)
        {
            return left < n && n < right;
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

        internal class GameObject
        {
            internal readonly Point pos;
            internal readonly Vector vel;

            public GameObject(Point pos, Vector vel)
            {
                this.pos = pos;
                this.vel = vel;
            }
        }
    }
}
