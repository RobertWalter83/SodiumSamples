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
You can get this packages installed via NuGet:
- [CSharp.Scripting (used version: 1.2.1)](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting).
- [AvalonEdit (used version: 5.0.3.0)](https://www.nuget.org/packages/AvalonEdit)

This is a work-in-progress live scripting application, heavily inspired by Bret Victors talk on [Inventing on Principle](https://www.youtube.com/watch?v=EGqwXt90ZqA), and the Elm language API.
Yet, this project isn't close to the capabilities of its inspirations, it might still be fun for some people. 

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
-> Change the string and see the effect :)


### 2. Show an image (make sure there is the file "problem1.png" in the root directory of the application)
```csharp
Element Image() 
{
    return e.Image(300, 200, @"problem1.png");
}

var main = Image();
```
-> Fiddle around with the numbers to see their pupose


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
-> again, you can change the scripts and see the effects immediately


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
-> Move your mouse over the result view (right half)


### 6. Keyboard state
```csharp
var main =
	k.Arrows.Map(e.Show);
```
-> Set focus into the result area and press the arrow keys
-> Replace the "Arrows" by "Wasd"
There is a known issue where the text editor sometimes doesn't show a caret anymore once you switched focus. If that happens, you have to restart.

 

Over time, more elaborated examples might follow.