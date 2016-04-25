using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveScripting
{
    static class CodeProvider
    {
        internal const string helloWorld = @"var main = e.Show(""Hello World"");";
        internal const string helloArray = @"var main =
    e.Show(
        new object[] 
        {
            new [] {1,2,3,4},
            new [] {'h', 'e', 'l', 'l', 'o' }
        }
    );";

        internal const string house = @"List<Point> Roof() 
{
    return new List<Point> 
        { new Point(50, 0), 
          new Point(100, 80), 
          new Point(0, 80), 
          new Point(50, 0)
        };
}

var bluePen = new Pen(Brushes.Blue, 1d);
var redPen = new Pen(Brushes.Crimson, 1d);

Element House() 
{
    return g.Collage(600, 400,
        t.Move(
            g.Rect(100, 150).DrawWith(Brushes.LightBlue, bluePen),
            100, 150),
        t.Move(
            g.Path(Roof()).DrawWith(Brushes.MistyRose, redPen),
            100, 70)
        );
}

var main = House();";

        internal const string houseCentered = @"List<Point> Roof() 
{
    return new List<Point> 
        { new Point(50, 0), 
          new Point(100, 80), 
          new Point(0, 80), 
          new Point(50, 0)
        };
}

var bluePen = new Pen(Brushes.Blue, 1d);
var redPen = new Pen(Brushes.Crimson, 1d);

Element House() 
{
    return g.Collage(150, 230,
        t.Move(
        	g.Rect(100, 150).DrawWith(Brushes.LightBlue, bluePen),
        	0.5, 79.5),
        g.Path(Roof()).DrawWith(Brushes.MistyRose, redPen));
}

Element Center(Size containerSize, Element elementToCenter)
{
	return e.Container((int)containerSize.Width, (int)containerSize.Height,
						e.Position.middle, // try e.Position.topLeft, ..midTop, ..topRight, ..midRight, and so on
						elementToCenter);
}

var main = Screen.Size.Map(size => Center(size, House()));";

        internal const string movableFaceKeyboard =
@"var orangePen = new Pen(Brushes.Orange, 4d);

var mouthCircle = g.ToDrawing(g.Circle(50), null, orangePen);
var mask = g.ToDrawing(g.Rect(60,40), Brushes.White, null);
// mouth is a Collage made of a circle and a 'mask' that hides a portion of the arc
var mouth = g.AsDrawing(g.Collage(100, 100, mouthCircle, t.Move(mask, -3, -3)));

var head = g.ToDrawing(g.Circle(100), null, orangePen);

// a function here to easily create two eyes
Drawing Eye()
{
    return g.Oval(17,10).DrawWith(Brushes.Orange, null);
}

// face combines the drawings and moves them to the right position
var face = g.Collage(400, 300,
                t.Move(mouth, 30, 40),
                t.Move(head, 5, 5),
                t.Move(Eye(), 20, 40),
                t.Move(Eye(), 70, 40));

// move the 'face' to the current point
Element View(Point point) 
{
    return g.Collage(400, 300,
        t.Move(g.AsDrawing(face), point.X, point.Y));
}

const int vx = 3;
const int vy = vx;

// update the current point with current input state
Point NewPoint(Tuple<int, int> input, Point pointOld)
{
    return new Point(pointOld.X + input.Item1*vx, pointOld.Y + input.Item2*vy);
}

var main = 
    k.ArrowsStream.Accum(new Point(0, 0), NewPoint).Map(View);";

        internal const string movableFaceMousePos = @"var orangePen = new Pen(Brushes.Orange, 4d);

var mouthCircle = g.ToDrawing(g.Circle(50), null, orangePen);
var mask = g.ToDrawing(g.Rect(60,40), Brushes.White, null);
// mouth is a Collage made of a circle and a 'mask' that hides a portion of the arc
var mouth = g.AsDrawing(g.Collage(100, 100, mouthCircle, t.Move(mask, -3, -3)));

var head = g.ToDrawing(g.Circle(100), null, orangePen);

// a function here to easily create two eyes
Drawing Eye()
{
    return g.Oval(17,10).DrawWith(Brushes.Orange, null);
}

// face combines the drawings and moves them to the right position
var face = g.Collage(400, 300,
                t.Move(mouth, 30, 40),
                t.Move(head, 5, 5),
                t.Move(Eye(), 20, 40),
                t.Move(Eye(), 70, 40));

// move the 'face' to the current point
Element View(Point point) 
{
    return g.Collage(800, 800,
        t.Move(g.AsDrawing(face), point.X, point.Y));
}

// update the current point with current input state
Point NewPoint(Point pointMouse, Point pointOld)
{
    return pointMouse;
}

var main = 
    m.PosStream.Accum(new Point(0, 0), NewPoint).Map(View);";

        internal const string movableFaceMouseClick = @"var orangePen = new Pen(Brushes.Orange, 4d);

var mouthCircle = g.ToDrawing(g.Circle(50), null, orangePen);
var mask = g.ToDrawing(g.Rect(60,40), Brushes.White, null);
// mouth is a Collage made of a circle and a 'mask' that hides a portion of the arc
var mouth = g.AsDrawing(g.Collage(100, 100, mouthCircle, t.Move(mask, -3, -3)));

var head = g.ToDrawing(g.Circle(100), null, orangePen);

// a function here to easily create two eyes
Drawing Eye()
{
    return g.Oval(17,10).DrawWith(Brushes.Orange, null);
}

// face combines the drawings and moves them to the right position
var face = g.Collage(400, 300,
                t.Move(mouth, 30, 40),
                t.Move(head, 5, 5),
                t.Move(Eye(), 20, 40),
                t.Move(Eye(), 70, 40));

// move the 'face' to the current point
Element View(Point point) 
{
    return g.Collage(800, 800,
        t.Move(g.AsDrawing(face), point.X, point.Y));
}

// update the current point with current input state
Point NewPoint(Unit val, Point pointCur)
{
    return pointCur;
}

var main = 
    m.ButtonsStream
    	// only interested if left mouse button pressed
    	.Filter(buttons => buttons.Item1 == MouseButtonState.Pressed) 
    	// not interested in the actual value, so we map it to a Unit value
    	.Map(_ => Unit.Value)
    	// take a snapshot of the mouse position cell when left click occurs
    	.Snapshot(m.PosCell, NewPoint)
    	// start with point (0,0)
    	.Hold(new Point(0,0))
    	// view at current position
    	.Map(View);";


        internal const string pongElm =
@"
const int boardWidth = 600;
const int boardHeight = 400;
const int boardWidthHalf = boardWidth/2;
const int boardHeightHalf = boardHeight/2;

#region model
[Flags]
enum State { Paused, Running }

struct GameObject
{
	internal double x;
	internal double y;
	internal double vx;
	internal double vy;
	
	internal GameObject(double x, double y, double vx=0, double vy=0)
	{
		this.x=x;
		this.y=y;
		this.vx=vx;
		this.vy=vy;
	}
}

const string helpDefault = ""SPACE to play and pause, W/S and \u2191\u2193 to move"";

struct Game
{
    internal GameObject ball;
    internal GameObject player1;
    internal GameObject player2;
    internal int scoreP1;
    internal int scoreP2;
    internal string help;

    internal Game(GameObject ball,
                    GameObject p1,
                    GameObject p2,
                    int scoreP1 = 0,
                    int scoreP2 = 0,
                    string help = helpDefault)
    {
        this.ball = ball;
        this.player1 = p1;
        this.player2 = p2;
        this.scoreP1 = scoreP1;
        this.scoreP2 = scoreP2;
        this.help = help;
    }
}

struct Input
{
    internal State state;
    internal int dir1;
    internal int dir2;
    internal TimeSpan dt;
}

var p1 = new GameObject(10, boardHeightHalf - 20);
var p2 = new GameObject(boardWidth - 20, boardHeightHalf - 20);
var ball = new GameObject(boardWidthHalf - 5, boardHeightHalf - 5, 200, 200);
var gameInitial = new Game(ball, p1, p2);

Input NewInput(TimeSpan dt, State state, int dir1, int dir2)
{
    return new Input { dt = dt, state = state, dir1 = dir1, dir2 = dir2 };
}
#endregion

#region View
var midLine = g.Rect(2, boardHeight);
Element View(Game game, Size size)
{
    return e.Container((int)size.Width, (int)size.Height, e.Position.middle,
            g.Collage(boardWidth, boardHeight,
                g.Rect(boardWidth, boardHeight).DrawWith(Brushes.DarkSlateGray, null),
                t.Move(midLine.DrawWith(Brushes.SlateGray, null), boardWidthHalf - 1, 0),
                DrawHelp(game.help),
                DrawScore(game.scoreP1, game.scoreP2),
                Draw(g.Rect(10, 40), game.player1),
                Draw(g.Rect(10, 40), game.player2),
                Draw(g.Circle(10), game.ball)
            )
        );
}

Drawing DrawHelp(string help)
{
    return t.Move(
            g.AsDrawing(
                e.Container(boardWidth, boardHeight, e.Position.middle,
                    Text.Foreground(Text.FromString(help), Brushes.White)
                )
            ),
        0, 100);
}

Drawing Draw(Shape shape, GameObject obj)
{
    return t.Move(shape.DrawWith(Brushes.White, null), obj.x, obj.y);
}

Drawing DrawScore(int scoreP1, int scoreP2)
{
    return g.AsDrawing(
                e.Container(boardWidth, 60, e.Position.midTop,
                    g.Collage(boardWidth, 60,
                        g.AsDrawing(Text.Size(
                    Text.Foreground(
                        Text.FromString(scoreP1.ToString() + ""   "" + scoreP2),

                    Brushes.SlateGray),
                48))
                    )
                )
            );
}

Drawing DrawPoints(int points)
{
    return g.AsDrawing(Text.Size(
                    Text.Foreground(
                        Text.FromString(points.ToString()),
                    Brushes.SlateGray),
                48));
}
#endregion

#region Update
Game Update(Input input, Game game)
{
    if (input.state == State.Paused)
    {
        if (game.help == helpDefault)
            return game;

        return new Game(game.ball, game.player1, game.player2, game.scoreP1, game.scoreP2, helpDefault);
    }

    var score1 = game.ball.x > boardWidth ? 1 : 0;
    var score2 = game.ball.x < -10 ? 1 : 0;

    var newBall = UpdateBall(input.dt, game.ball, game.player1, game.player2);
    var p1 = UpdatePlayer(input.dt, input.dir1, game.player1, input.state);
    var p2 = UpdatePlayer(input.dt, input.dir2, game.player2, input.state);
    return new Game(newBall, p1, p2, game.scoreP1 + score1, game.scoreP2 + score2, String.Empty);
}

GameObject UpdatePlayer(TimeSpan timeSpan, int dir, GameObject player, State state)
{
    return ClampY(UpdatePosition(timeSpan, UpdatePlayerVelocity(player, dir)), 5, 355);
}

GameObject UpdatePlayerVelocity(GameObject player, int dir)
{
    return new GameObject(player.x, player.y, player.vx, dir * 200);
}

GameObject ClampY(GameObject g, int min, int max)
{
    return new GameObject(g.x, b.Clamp(g.y, min, max), g.vx, g.vy);
}

GameObject UpdateBall(TimeSpan dt, GameObject ball, GameObject p1, GameObject p2)
{

    if (!Within(-10, boardWidth, (int)ball.x))
        return new GameObject(boardWidthHalf - 5, boardHeightHalf - 5, ball.vx, ball.vy);

    return UpdatePosition(dt, UpdateBallVelocity(ball, p1, p2));
}

GameObject UpdateBallVelocity(GameObject ball, GameObject p1, GameObject p2)
{
    return new GameObject
            (
                ball.x,
                ball.y,
                StepV(ball.vx, Within(p2, ball, -10), Within(p1, ball, 10)),
                StepV(ball.vy, ball.y > boardHeight - 10, ball.y < 0)
            );
}

GameObject UpdatePosition(TimeSpan timeSpan, GameObject gameObject)
{
    return new GameObject
            (
                gameObject.x + gameObject.vx * timeSpan.TotalSeconds,
                gameObject.y + gameObject.vy * timeSpan.TotalSeconds,
                gameObject.vx,
                gameObject.vy
            );
}

double StepV(double v, bool lowerCollision, bool upperCollision)
{
    if (lowerCollision)
        return -Math.Abs(v);

    if (upperCollision)
        return Math.Abs(v);

    return v;
}

bool Within(GameObject p, GameObject b, int xOffset)
{
    int tmp = (int)(p.x + xOffset);
    int right = (int)p.x;
    int left = tmp < right ? tmp : right;
    right = tmp > right ? tmp : right;

    return Within(left, right, (int)b.x) &&
            Within((int)(p.y - 27.5), (int)(p.y + 27.5), (int)b.y);
}

bool Within(int left, int right, int n)
{
    return left <= n && n <= right;
}

State UpdateGameState(bool isDown, State state)
{
    return isDown ? State.Running & ~state : state;
}
#endregion

#region signals
var input =
    Time.Ticks.Snapshot(
        k.SpaceStream.Accum(State.Paused, UpdateGameState),
        k.WasdCell.Map(tuple => tuple.Item2),
        k.ArrowsCell.Map(tuple => tuple.Item2),
        NewInput);

var gameSignal =
    input.Accum(gameInitial, Update);

var main =
    gameSignal.Lift(Screen.Size, View); 
#endregion
";

        internal const string pongAlt =
@"const int boardWidth = 600;
const int boardHeight = 400;
const int boardWidthHalf = boardWidth/2;
const int boardHeightHalf = boardHeight/2;
const int v = 200;
const int vx = v;
const int vy = v;

#region model
[Flags]
enum State { Paused, Running }

struct GameObject
{
	internal double x;
	internal double y;
	internal double vx;
	internal double vy;
}

GameObject Player(double x, double y)
{
	return new GameObject { x = x, y = y };
}

const string helpDefault = ""SPACE to play and pause, W/S and \u2191\u2193 to move"";

struct Game
{
    internal State state;
    internal GameObject ball;
    internal GameObject player1;
    internal GameObject player2;
    internal int scoreP1;
    internal int scoreP2;
    internal string help;
}

Game NewGame(State s, GameObject b, GameObject p1, GameObject p2, int sP1, int sP2)
{
    return new Game { state = s, ball = b, player1 = p1, player2 = p2, scoreP1 = sP1, scoreP2 = sP2, help = s == State.Paused ? helpDefault : String.Empty };
}

GameObject DefaultBall()
{
    return
    new GameObject { x = boardWidthHalf - 5, y = boardHeightHalf - 5, vx = vx, vy = vy };
}
#endregion

#region view
Element View(Game game, Size size)
{
    return e.Container((int)size.Width, (int)size.Height, e.Position.middle,
        g.Collage(boardWidth, boardHeight,
            g.Rect(boardWidth, boardHeight).DrawWith(Brushes.DarkSlateGray, null),
            t.Move(
                g.Rect(2, boardHeight).DrawWith(Brushes.SlateGray, null),
            boardWidthHalf - 1, 0),
            DrawHelp(game.help),
            DrawScore(game.scoreP1, game.scoreP2),
            Draw(g.Rect(10, 40), game.player1),
            Draw(g.Rect(10, 40), game.player2),
            Draw(g.Circle(10), game.ball)
        )
    );
}

Drawing DrawHelp(string help)
{
    return t.Move(
            g.AsDrawing(
                e.Container(boardWidth, boardHeight, e.Position.middle,
                    Text.Foreground(Text.FromString(help), Brushes.White)
                )
            ),
        0, 100);
}

Drawing Draw(Shape shape, GameObject obj)
{
    return t.Move(shape.DrawWith(Brushes.White, null), obj.x, obj.y);
}

Drawing DrawScore(int scoreP1, int scoreP2)
{
    return g.AsDrawing(
                e.Container(boardWidth, 60, e.Position.midTop,
                    g.Collage(boardWidth, 60,
                        g.AsDrawing(Text.Size(
                    Text.Foreground(
                        Text.FromString(scoreP1.ToString() + ""   "" + scoreP2),

                    Brushes.SlateGray),
                48))
                    )
                )
            );
}

Drawing DrawPoints(int points)
{
    return g.AsDrawing(Text.Size(
                    Text.Foreground(
                        Text.FromString(points.ToString()),
                    Brushes.SlateGray),
                48));
}
#endregion

#region
GameObject UpdatePlayer(TimeSpan timeSpan, int dir, GameObject player, State state)
{
    if (state == State.Paused)
        return player;

    return ClampY(UpdatePosition(timeSpan, UpdatePlayerVelocity(player, dir)), 5, 355);
}

GameObject UpdatePlayerVelocity(GameObject player, int dir)
{
    return new GameObject { x = player.x, y = player.y, vx = player.vx, vy = dir * v };
}

GameObject ClampY(GameObject g, int min, int max)
{
    return new GameObject { x = g.x, y = b.Clamp(g.y, min, max), vx = g.vx, vy = g.vy };
}

GameObject UpdateBall(TimeSpan dt, GameObject ball, GameObject p1, GameObject p2, State state)
{
    if (state == State.Paused)
        return ball;

    if (!Within(-10, boardWidth, (int)ball.x))
        return new GameObject { x = boardWidthHalf - 5, y = boardHeightHalf - 5, vx = ball.vx, vy = ball.vy };

    return UpdatePosition(dt, UpdateBallVelocity(ball, p1, p2));
}

GameObject UpdateBallVelocity(GameObject ball, GameObject p1, GameObject p2)
{
    return new GameObject
    {
        x = ball.x,
        y = ball.y,
        vx = StepV(ball.vx, Within(p2, ball, -10), Within(p1, ball, 10)),
        vy = StepV(ball.vy, ball.y > boardHeight - 10, ball.y < 0)
    };
}

GameObject UpdatePosition(TimeSpan timeSpan, GameObject gameObject)
{
    return new GameObject
    {
        x = gameObject.x + gameObject.vx * timeSpan.TotalSeconds,
        y = gameObject.y + gameObject.vy * timeSpan.TotalSeconds,
        vx = gameObject.vx,
        vy = gameObject.vy
    };
}

double StepV(double v, bool lowerCollision, bool upperCollision)
{
    if (lowerCollision)
        return -Math.Abs(v);

    if (upperCollision)
        return Math.Abs(v);

    return v;
}

bool Within(GameObject p, GameObject b, int xOffset)
{
    int tmp = (int)(p.x + xOffset);
    int right = (int)p.x;
    int left = tmp < right ? tmp : right;
    right = tmp > right ? tmp : right;

    return Within(left, right, (int)b.x) &&
            Within((int)(p.y), (int)(p.y + 40), (int)b.y);
}

bool Within(int left, int right, int n)
{
    return left <= n && n <= right;
}

State UpdateGameState(bool isDown, State state)
{
    return isDown ? State.Running & ~state : state;
}
#endregion

#region signals
Cell<State> CellState()
{
    return k.SpaceStream.Accum(State.Paused, UpdateGameState);
}

Cell<GameObject> CellPlayer(GameObject playerInitial, Cell<int> cDir, Cell<State> cState)
{
    var cPlayer = new CellLoop<GameObject>();
    Stream<GameObject> sPlayer = Time.Ticks.Snapshot(cDir, cPlayer, cState, UpdatePlayer);
    cPlayer.Loop(sPlayer.Hold(playerInitial));
    return cPlayer;
}

Cell<int> CellPoints(Cell<GameObject> cBall, Stream<GameObject> sBall, Func<GameObject, GameObject, Tuple<bool, GameObject>> ScoringCondition)
{
    Stream<Unit> sScored = sBall.Collect(cBall.Sample(), ScoringCondition)
        .Filter(scored => scored)
        .Map(_ => Unit.Value);

    return sScored.Accum(0, (_, points) => ++points);
}

Tuple<bool, GameObject> P2Scored(GameObject ballCur, GameObject ballOld)
{
    bool fScore = ballOld.x > 0 && ballCur.x <= 0;
    return Tuple.Create(fScore, ballCur);
}

Tuple<bool, GameObject> P1Scored(GameObject ballCur, GameObject ballOld)
{
    bool fScore = ballOld.x <= boardWidth && ballCur.x > boardWidth;
    return Tuple.Create(fScore, ballCur);
}

Cell<Game> GameSignal()
{
    var cState = CellState();

    var cP1 = CellPlayer(
            Player(10, boardHeightHalf - 20),
            k.WasdCell.Map(wasd => wasd.Item2),
            cState);

    var cP2 = CellPlayer(
            Player(boardWidth - 20, boardHeightHalf - 20),
            k.ArrowsCell.Map(arrows => arrows.Item2),
            cState);

    var cBall = new CellLoop<GameObject>();
    Stream<GameObject> sBall = Time.Ticks.Snapshot(cBall, cP1, cP2, cState, UpdateBall);
    cBall.Loop(sBall.Hold(DefaultBall()));

    var cPoints1 = CellPoints(cBall, sBall, P1Scored);
    var cPoints2 = CellPoints(cBall, sBall, P2Scored);

    return cState.Lift(cBall, cP1, cP2, cPoints1, cPoints2, NewGame);
}

var main =
    GameSignal().Lift(Screen.Size, View);   
#endregion

";
    }
}
