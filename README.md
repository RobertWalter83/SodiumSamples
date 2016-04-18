# SodiumSamples

This repository is a collection of examples using [FRP library Sodium](https://github.com/SodiumFRP/sodium) and other technologies.
The purpose is to discuss the examples, their weaknesses and their potential, to learn Sodium together, and maybe to identify APIs on top of Sodium, to capture best practices.

## PongSodium

This is a simple exercise implementing the game Pong with the [C# implementation](https://github.com/SodiumFRP/sodium/tree/master/c%23) of Sodium.
In order to run this WPF application, you need a reference to the Sodium library.

The code provides explanatory comments to guide readers through its logic. As the implementation itself, these comments are subject to discussion, so feel free to provide feedback.

Note that this implementation of Pong makes heavy use of the [Pong example provided by Elm](http://elm-lang.org/examples/pong).
Also, the user [Ziriax](https://github.com/Ziriax) made a significant contribution to how to move the paddles.

## LiveScripting

This project uses the C# Scripting package of the [.NET Compiler Platform (a.k.a Roslyn)](https://github.com/dotnet/roslyn), Sodium, and the [AvalonEdit package](https://github.com/icsharpcode/AvalonEdit). 
You can get these packages installed via NuGet:
- [CSharp.Scripting (used version: 1.2.1)](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting).
- [AvalonEdit (used version: 5.0.3.0)](https://www.nuget.org/packages/AvalonEdit)

This is a work-in-progress live scripting application, heavily inspired by Bret Victor's talk on [Inventing on Principle](https://www.youtube.com/watch?v=EGqwXt90ZqA), and the Elm language API.
Yet, this project isn't close to the capabilities of its inspirations, it might still be fun for some people. 

You might want to check out some screen casts showing [basic examples](https://www.youtube.com/watch?v=i9X3h6P1S68&list=PLqDDIFifPR4X7wi_gOcBbSZp2sdZgfIwy).

The contract is that the C# script in the app contains a "main" variable that is of type "LiveScripting.Element" or "Cell<LiveScripting.Element>", in order to see something in the result area.
To get started, here are some scripts you can try out. Let me know if they don't work.

Notice the following aliases to access the static API:
```csharp
using g = LiveScripting.Graphics;
using e = LiveScripting.Graphics.Element;
using t = LiveScripting.Transform;
using m = LiveScripting.Mouse;
using k = LiveScripting.Keyboard;
```

### 1. Hello World
```csharp
var main = 
    e.Show("Hello World"); 
```
- Change the string and see the effect :)
- in general, edit the examples and see what happens in the result area
- Show is, like in Elm, a sort of "ToString()". You can pass whatever, and it will convert it in a string and display it. It is a tiny bit more sophisticated when it comes to arrays, though. See next example.

### 1.5 Show arrays
```csharp
var main =
	e.Show(
		new object[] 
		{
			new [] {1,2,3,4},
			new [] {'h', 'e', 'l', 'l', 'o' }
		}
	);
```

### 2. Show an image (make sure there is the file "problem1.png" in the root directory of the application)
```csharp
Element Image() 
{
    return e.Image(300, 200, @"problem1.png");
}

var main = Image();
```
- Fiddle around with the numbers to see their pupose


### 3. Add some annotations to example 2.
```csharp
Element Annotated(Element ele) 
{
	return g.Collage(300, 200,
		g.AsDrawing(ele),
		t.Move(
			g.Circle(10).DrawWith(null, new Pen(Brushes.Green, 2d)),
			28, 36),
		t.Move(
			g.AsDrawing(e.Text("I'm an annotation")),
			10, 0)
		);
}

Element Image() 
{
	return e.Image(300, 200, @"problem1.png");
}

var main = Annotated(Image());
```
- again, you can change the scripts and see the effects immediately


### 4. Draw a house
```csharp
 List<Point> Triangle() 
{
    return new List<Point> 
        { new Point(15, 0), 
          new Point(30, 30), 
          new Point(0, 30), 
          new Point(15, 0)
        };
}

Element House() 
{
    return g.Collage(300, 200,
        t.Move(
            g.Rect(30, 40).DrawWith(Brushes.Blue, null),
            40, 50),
        t.Move(
            g.Path(Triangle()).DrawWith(null, new Pen(Brushes.Red, 1d)),
            40, 20)
        );
}

var main = House();
```

### 5. Get mouse position (reactive, yay!)
```csharp
var main =
	m.MousePos.Map(e.Show);
```
- Move your mouse over the result view (right half)


### 6. Keyboard state
```csharp
var main =
	k.Arrows.Map(e.Show);
```
- Set focus into the result area and press the arrow keys
- Replace the "Arrows" by "Wasd"

There is a known issue where the text editor sometimes doesn't show a caret anymore once you switched focus. If that happens, you have to restart.

### 7. Move a Drawing
```csharp
var orangePen = new Pen(Brushes.Orange, 4d);
	
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

// an input stream: over time, we 'snapshot' the arrow key state
Stream<Tuple<int, int>> StreamInput()
{
	return Time.Ticks.Snapshot(
		Keyboard.Arrows, (dt, arrows) => arrows);  
}

// update the current point with current input state
Point NewPoint(Tuple<int, int> input, Point pointOld)
{
	return new Point(pointOld.X + input.Item1, pointOld.Y + input.Item2);
}

var main = 
	StreamInput().Accum(new Point(0, 0), NewPoint).Map(View);
```
- Set focus in the result area and hold arrow keys to move face

#######
Over time, more elaborated examples might follow.