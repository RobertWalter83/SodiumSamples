using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Shapes;
using Sodium;

namespace SodiumPong
{
    public partial class MainWindow : Window
    {
        #region constants
        const int dxBoard = 600;
        const int dyBoard = 400;
        const int dxBoardHalf = dxBoard / 2;
        const int dyBoardHalf = dyBoard / 2;
        private static readonly Vector playerVelocityAbs = new Vector(0, 200);
        #endregion

        public MainWindow()
        {
            #region initialize board visuals
            InitializeComponent();

            this.cvs.Width = dxBoard;
            this.cvs.Height = dyBoard;

            // initialize drawable elements
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
            #endregion

            // Sodium best practice: wrap your FRP code in an explicit transaction
            Transaction.RunVoid(() =>
            {
                /**
                 * we create two StreamSinks to interface with .Net events:
                 * (1) sKeyEvents: we send KeyEventArgs-messages to this sink
                 * (2) sRenderEvents: we send RenderingEventArgs-messages to this sink
                 * WHY? we need those to react to user input and time passed.
                 * The call "send" on a StreamSink makes it "fire" an event, something we can react to.
                 */
                StreamSink<KeyEventArgs> sKeyEvents = new StreamSink<KeyEventArgs>();
                StreamSink<RenderingEventArgs> sRenderEvents = new StreamSink<RenderingEventArgs>();

                // On a key event, send into (1): we are interested in both KeyDown and KeyUp events
                KeyDown += (sender, args) => sKeyEvents.Send(args);
                KeyUp += (sender, args) => sKeyEvents.Send(args);

                // here we send into (2)
                CompositionTarget.Rendering += (sender, args) => sRenderEvents.Send((RenderingEventArgs)args);

                /** 
                 * we create a Stream that provides measurements of time between rendering events whenever it fires
                 * WHY? to move objects continuously even if rendering dives. 
                 * The delta will be used as a factor later when we determine how far to move a game object
                 */
                Stream<TimeSpan> sTicks = sRenderEvents.Collect(TimeSpan.Zero,
                    (re, t) => Tuple.Create(re.RenderingTime - t, re.RenderingTime));

                /**
                 * "direction of player movement is a function of keyEvents for UP (North) and DOWN (South) movement"
                 * notice that I use North and South to indicate movement direction, to avoid confusion with key event KeyUp and KeyDown
                 * direction is encoded as INT: -1 is North, 1 is South, 0 is no movement
                 * logic is moved into method CellDirPlayer to avoid code redundancy
                 * we use a Cell since a player has to have a discrete direction at any given time
                 * WHY? we need to know the player direction later to move the gameObject correctly
                 */
                Cell<int> cDirP1 = CellDirPlayer(sKeyEvents, Key.W, Key.S);
                Cell<int> cDirP2 = CellDirPlayer(sKeyEvents, Key.Up, Key.Down);

                /**
                 * "game state is a function of itself and keyEvent SPACE"
                 * sKeyEvents fires for each KeyDown and KeyUp
                 * STEP 1: we filter for SPACE key events (we are not interested in the others in this context)
                 * STEP 2: we accumulate the gameState in the Cell cGameState, starting out with state "Paused"
                           the logic for how we accumulate it is in the function UpdateGameState. 
                 * WHY? we want to be able to pause the game
                 */
                Cell<GameState> cGameState = sKeyEvents
                                                .Filter(e => e.Key == Key.Space)
                                                .Accum(GameState.Paused, UpdateGameState);

                /**
                 * "player state is a function of itself, time passed, direction, and game state"
                 * we create initial data that represents the two players at the beginning of the game
                 * a player is defined by its position and velocity (see "GameObject")
                 * The Cells cP1 and cP2 are updated over time (sTicks) according to their 
                   - old state (position and velocity)
                   - direction (which we gathered before based on input)
                   - and game state (allows us to short-circuit the player update if game is paused)
                 * WHY? Player state needs to be updated to play the game
                 */
                GameObject p1Initial = PlayerInitial(new Point(20, dyBoardHalf), viewP1);
                GameObject p2Initial = PlayerInitial(new Point(dxBoard - 20, dyBoardHalf), viewP2);
                Cell<GameObject> cP1 = CellPlayer(p1Initial, sTicks, cDirP1, cGameState);
                Cell<GameObject> cP2 = CellPlayer(p2Initial, sTicks, cDirP2, cGameState);

                /**
                 * "ball state is a function of itself, time passed, player states, and game state"
                 * we create the initial ball state for the beginning of the game
                 * we create a CellLoop that allows us to define state (the ball) in terms of itself
                 * every time sTicks fires, we call UpdateBall, passing in the state of different cells at the given time
                    - time passed (comes from the sTicks stream, since it fired)
                    - current ball state (comes from cBall cell)
                    - current player1 state
                    - current player2 state
                    - current game state
                 * this "snapshot" yields a stream of balls, that we "hold" in cBall, starting out with the initial ball
                 * notice that this is similar to how we created the player cells, just not factored out into a dedicated method
                 * WHY? Ball state needs to be updated to play the game
                 */
                GameObject ballInitial = BallInitial(viewBall);
                CellLoop<GameObject> cBall = new CellLoop<GameObject>();
                Stream<GameObject> sBall = sTicks.Snapshot(cBall, cP1, cP2, cGameState, UpdateBall);
                cBall.Loop(sBall.Hold(ballInitial));

                /**
                 * "player points is a function of itself (we loop inside CellPoints), 
                    old ball position (cBall), current ball position (sBall), and a scoring condition (P1Scored / P2Scored)"
                 * WHY? We want to keep tracks of the player points
                 */
                Cell<int> cPoints1 = CellPoints(cBall, sBall, P1Scored);
                Cell<int> cPoints2 = CellPoints(cBall, sBall, P2Scored);

                /**
                 * EXERCISE: This is a simplified version of how to accumulate player points.
                 * It works most of the time, but it has an issue
                 * Try to define a prose sentence that describes how player points are captured by below code
                 * Try to derive from that description the issue of this code.
                 */
                //Cell<int> cPoints1 = sBall.Accum(0, (ball, points) => ball.pos.X > dxBoard ? ++points : points);
                //Cell<int> cPoints2 = sBall.Accum(0, (ball, points) => ball.pos.X <= 0 ? ++points : points);

                /**
                 * We listen to changes of player and ball cells to interface with .Net to display our game objects
                 * The reason we passed a reference to a game objects' visual representations (its "view", 
                   see where we initialize players and ball) is so that we can conveniently call the same Draw function
                 */
                cP1.Listen(Draw);
                cP2.Listen(Draw);
                cBall.Listen(Draw);

                cPoints1.Listen(score =>
                {
                    textScore1.Text = score.ToString();
                });

                cPoints2.Listen(score =>
                {
                    textScore2.Text = score.ToString();
                });

                cGameState.Listen(gameState =>
                {
                    textRunPause.Visibility = gameState == GameState.Paused ? Visibility.Visible : Visibility.Hidden;
                });
            });
        }

        /**
         * First, we create a stream that filters key events to only react if the Key "keyNorth" is affected.
         * Then, we map this stream to either -1 or 0, depending on whether of not "keyNorth" is down.
         * This means we map the state of keyNorth (is down or not) to the values -1 and 0 respectively.
         * This yields a stream that fires "-1" every time "keyNorth" is down and "0" once "keyNorth" is released. 
            This behavior ("0" being only fired once) results from the behavior of the .Net events KeyDown and KeyUp
            that we registered to at the very beginning:
            .Net fires KeyDown continuously while a key is held down, and KeyUp exactly once when the key is released.
         * We do the same mapping for "keySouth", basically saying "if keySouth is pressed keep firing 1's, fire 0 once it is released"
         * In the last line, we use "OrElse" to merge both streams together, resulting in a stream that fires: 
            -1, if keyNorth is down
             1, if keySouth is down
             0, if keyNorth or keySouth is released  
         * To hold the stream's value in a Cell, initially with the value 0
         */
        private static Cell<int> CellDirPlayer(Stream<KeyEventArgs> sKeyEvents, Key keyNorth, Key keySouth)
        {
            Stream<int> sDirP1Left = sKeyEvents.Filter(e => e.Key == keyNorth).Map(e => e.IsDown ? -1 : 0);
            Stream<int> sDirP1Right = sKeyEvents.Filter(e => e.Key == keySouth).Map(e => e.IsDown ? 1 : 0);
            return sDirP1Left.OrElse(sDirP1Right).Hold(0);
        }

        /**
         * Ran whenever the SPACE key is pressed (see its usage)
         * we are only interested if the key state is down: then, we toggle between Running and Paused states
         * EXERCISE: 
            Try to predict what happens if you hold down the SPACE Key. 
            See if you are right by trying it.
            How could we improve?
         */
        private static GameState UpdateGameState(KeyEventArgs e, GameState gameState)
        {
            return e.IsDown ? GameState.Running & ~gameState : gameState;
        }

        /**
         * "player state is a function of itself, time passed, direction, and game state"
         * we use the same pattern as we did with the ball:
         * we create CellLoop that allows us to define the player state in terms of itself
         * every time sTick fires, we update the player state by calling UpdatePlayer with the current
            - time passed (comes from sTicks)
            - player state (snapshot of cPlayer)
            - direction state (snapshot of cDir)
            - game state (snapshot of cGameState)
         * the resulting stream fires every time sTicks sends it a new player state
         * we connect our CellLoop to hold the value of sPlayer at any given time, starting out with playerInitial
         */
        private static Cell<GameObject> CellPlayer(GameObject playerInitial, Stream<TimeSpan> sTicks, Cell<int> cDir, Cell<GameState> cGameState)
        {
            CellLoop<GameObject> cPlayer = new CellLoop<GameObject>();
            Stream<GameObject> sPlayer = sTicks.Snapshot(cPlayer, cDir, cGameState, UpdatePlayer);
            cPlayer.Loop(sPlayer.Hold(playerInitial));
            return cPlayer;
        }

        /**
         * Ran whenever sTicks fires (see its usage)
         * Short circuits if game is paused, returning the current player state
         * otherwise:
            - Updates the player velocity based on the current player and the movement direction
            - Updates the player (position) based on the time passed and the current player (velocity)
            - Clamp the Y position to ensure player is still on screen
         */
        private static GameObject UpdatePlayer(TimeSpan timeSpan, GameObject player, int dir, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return player;

            return UpdatePosition(timeSpan, UpdatePlayerVelocity(player, dir)).ClampY(25, 375);
        }

        /**
         * Creates a new copy of the passed player with a velocity vector pointing either "north" or "south"
         */
        private static GameObject UpdatePlayerVelocity(GameObject player, int dir)
        {
            return player.With(playerVelocityAbs*dir);
        }

        /**
         * Creates a new copy of the passed gameObject with an updated position based on the input.
         */
        private static GameObject UpdatePosition(TimeSpan timeSpan, GameObject gameObject)
        {
            return gameObject.With(gameObject.pos + gameObject.vel*timeSpan.TotalSeconds);
        }

        /**
         * "ball state is a function of itself, time passed, both players, and game state"
         * we short circuit if the game is paused, returning the current ball state
         * otherwise, we check the ball's horizontal position to see if it is still on screen (within 0 and dxBoard)
            - if not, we return a new ball which starts in the middle of the board (with the same velocity and view as before!)
            - otherwise, 
                -- Update ball velocity based on ball (position) and the players (position)
                -- Update ball position based on time passed and ball (velocity)
         */
        private static GameObject UpdateBall(TimeSpan timeSpan, GameObject ball, GameObject p1, GameObject p2, GameState gameState)
        {
            if (gameState == GameState.Paused)
                return ball;

            if (!Within(0, dxBoard, (int)ball.pos.X))
                return ball.With(new Point(dxBoardHalf, dyBoardHalf));

            return UpdatePosition(timeSpan, UpdateBallVelocity(ball, p1, p2));
        }

        /**
         * Horizontal velocity (X): 
            - Within calls: check if we collide with player 2 ("lower collision") or player 1 ("upper collision")
              The offset (-10 / 10) is necessary to account for our render translation we define in the Ellipse that represents the ball 
              in order to have its logical origin (top left) at the _center_ of the view object
            - first call to StepV: Update X-component based of the results of the collision check
            - second call to StepV: Update Y-component based on the vertical ball position 
              (-5/5 to account for ball's render translation)  
         */
        private static GameObject UpdateBallVelocity(GameObject ball, GameObject p1, GameObject p2)
        {
            return ball.With(
                new Vector(
                    StepV(ball.vel.X, Within(p2, ball, -10), Within(p1, ball, 10)),
                    StepV(ball.vel.Y, ball.pos.Y > dyBoard - 5, ball.pos.Y < 5)));
        }

        /**
         * we pass a velocity component in (v)
         * if we have a "lower collision" (for the horizontal component X of the velocity vector, this means a "right collision" with player 2),
           we want to move "upward", or "left", respectively, so we make sure the new velocity component is negative
         * if we have a "upper collision" or "left collision", we make sure the new velocity component is positive
         * otherwise, we move on as before
         */
        private static double StepV(double v, bool lowerCollision, bool upperCollision)
        {
            if (lowerCollision)
                return -Math.Abs(v);

            if (upperCollision)
                return Math.Abs(v);

            return v;
        }

        /**
         * players and ball can only collide horizontally
         * we first check if the ball.pos.X is within a small vertical stripe of the player
         * if true, we check if the ball ALSO is within a horizontal stripe of the current player
         * if both are true, this is considered a collision
         * the offset (-27.5 / 27.5) accounts for the player's render translation  
         */
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
            return left <= n && n <= right;
        }

        /**
         * we want to count a point exactly once when the ball "trespasses" a virtual goal line on the opposing player's side
         * therefore, we create a stream sScored that fires every time this scoring condition applies
           (1) by "collecting" on sBall, we can react to each ball update (its movement)
           (2) ScoringCondition returns true when the ball "trespasses" the virtual goal line, and we collect these occurances 
               (the virtual goal line is different for player 1 and player 2, which is why we need provide a Func that can evaluate the condition)
               notice that, instead of using Collect, we could use Snapshot here as well, but Collect makes it more explicit to me that we use a 
               state machine that tells us when we transition from "before the goal line" to "beyond the goal line".
           (3) we filter to only receive events when we actually want to change the score (when our boolean Stream has the value "true", i.e. when we trespass the goal line)
           (4) we map to a "Unit" stream (we don't have to do this, but using Unit makes it explicit that we don't care what the stream's value is when it fires,
               we are only interested in the fact that it fires)
         * all we have to do now: when sScored fires, accumulate the points.
         */
        private static Cell<int> CellPoints(Cell<GameObject> cBall, Stream<GameObject> sBall, Func<GameObject, GameObject, Tuple<bool, GameObject>> ScoringCondition)
        {
            Stream<Unit> sScored = sBall.Collect(cBall.Sample(), ScoringCondition)
                .Filter(scored => scored)
                .Map(_ => Unit.Value);

            return sScored.Accum(0, (_, points) => ++points);
        }


        /**
         * Simple state machine for player 1
         * states: "before the goal line" and "beyond the goal line"
         * transition: when the old ball is "before the goal line" and the current ball is "beyond the goal line"
         */
        private static Tuple<bool, GameObject> P1Scored(GameObject ballCur, GameObject ballOld)
        {
            bool fScore = ballOld.pos.X <= dxBoard && ballCur.pos.X > dxBoard;
            return Tuple.Create(fScore, ballCur);
        }

        /**
         * Simple state machine for player 2
         * states: "before the goal line" and "beyond the goal line"
         * transition: when the old ball is "before the goal line" and the current ball is "beyond the goal line"
         */
        private static Tuple<bool, GameObject> P2Scored(GameObject ballCur, GameObject ballOld)
        {
            bool fScore = ballOld.pos.X > 0 && ballCur.pos.X <= 0;
            return Tuple.Create(fScore, ballCur);
        }

        /**
         * we "draw" by just updating the position of the game object's view on the canvas
         */
        private static void Draw(GameObject gameObject)
        {
            Canvas.SetLeft(gameObject.view, gameObject.pos.X);
            Canvas.SetTop(gameObject.view, gameObject.pos.Y);
        }

        #region UIElement_s
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
        #endregion

        #region Model
        private static GameObject PlayerInitial(Point pointInitial, UIElement view)
        {
            return GameObject.Create(pointInitial, new Vector(0, 0), view);
        }

        private static GameObject BallInitial(UIElement view)
        {
            return GameObject.Create(new Point(dxBoardHalf, dyBoardHalf), new Vector(200, -200), view);
        }
        
        internal class GameObject
        {
            internal readonly Point pos;
            internal readonly Vector vel;
            internal readonly UIElement view;

            private GameObject(Point pos, Vector vel, UIElement view)
            {
                this.pos = pos;
                this.vel = vel;
                this.view = view;
            }

            internal static GameObject Create(Point pos, Vector vel, UIElement view)
            {
                return new GameObject(pos, vel, view);
            }

            internal GameObject With(Point p)
            {
                return new GameObject(p, vel, view);
            }

            internal GameObject With(Vector v)
            {
                return new GameObject(pos, v, view);
            }

            public GameObject ClampX(double min, double max)
            {
                return this.With(new Point(this.pos.X.Clamp(min, max), this.pos.Y));
            }

            public GameObject ClampY(double min, double max)
            {
                return this.With(new Point(this.pos.X, this.pos.Y.Clamp(min, max)));
            }
        }

        [Flags]
        internal enum GameState
        {
            Paused = 0,
            Running = 1
        }
        #endregion
    }
}
