using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Sodium;

namespace LiveScripting
{
    public static class Basic
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
                return min;

            if (val.CompareTo(max) > 0)
                return max;

            return val;
        }
    }

    public static class Time
    {
        public static Stream<TimeSpan> Ticks { get; internal set; }
    }
    public static class Mouse
    {
        public static Cell<Point> MousePos { get; internal set; }

        public static Cell<Tuple<MouseButtonState, MouseButtonState, MouseButtonState>> MouseButtons
        { get; internal set; }
    }

    public static class Keyboard
    {
        public static Cell<Tuple<int, int>> Arrows { get; internal set; }
        public static Cell<Tuple<int, int>> Wasd { get; internal set; }
    }

    public static class Screen
    {
        public static Cell<Size> Size { get; internal set; }
    }

    public static class Graphics
    {
        internal static readonly Point PointZero = new Point(0, 0);
        
        public static LiveScripting.Element Collage(int w, int h, params Drawing[] drawings)
        {
            DrawingGroup dg = new DrawingGroup {ClipGeometry = new RectangleGeometry(RectFromDim(w, h))};
            foreach (var drawing in drawings)
            {
                if (drawing == null)
                    continue;
                dg.Children.Add(drawing);
            }

            return new LiveScripting.Element(dg);
        }

        public static class Element
        {
            public static LiveScripting.Element Show<T>(T t)
            {
                return Text(ToString(t));
            }

            public static LiveScripting.Element Show(object a)
            {
                return Show<object>(a);
            }

            private static string ToString(object a)
            {
                if (!a.GetType().IsArray)
                    return a.ToString();
                
                var en = a as IEnumerable;
                return $"[ {string.Join(", ", (from object element in en select ToString(element)).ToArray())} ]";
            }

            public static LiveScripting.Element Image(int w, int h, string src)
            {
                return new Image(w, h, src);
            }

            public static LiveScripting.Element Text(string st)
            {
                return new Text(st);
            }

            public static LiveScripting.Element Container(int w, int h, Position p, LiveScripting.Element e)
            {
                var shapeContainer = Rect(w, h);
                var drawingContainer = ToDrawing(shapeContainer, Brushes.Transparent, null);
                var drawingContent = AsDrawing(e);
                return Collage(w, h, drawingContainer, TransformTo(w, h, p, drawingContent));
            }

            private static Drawing TransformTo(int w, int h, Position p, Drawing content)
            {
                double offsetX = 0;
                double offsetY = 0;
                double dx = w - content.Bounds.Width;
                double dy = h - content.Bounds.Height;
                switch (p)
                {
                    case Position.topLeft:
                        break;
                    case Position.topRight:
                        offsetX = dx;
                        break;
                    case Position.middle:
                        offsetX = dx/2;
                        offsetY = dy/2;
                        break;
                    case Position.midTop:
                        offsetX = dx/2;
                        break;
                    case Position.midBottom:
                        offsetX = dx/2;
                        offsetY = dy;
                        break;
                    case Position.midLeft:
                        offsetY = dy/2;
                        break;
                    case Position.midRight:
                        offsetX = dx;
                        offsetY = dy/2;
                        break;
                    case Position.bottomLeft:
                        offsetY = dy;
                        break;
                    case Position.bottomRight:
                        offsetX = dx;
                        offsetY = dy;
                        break;

                }
                return Transform.Move(content, offsetX, offsetY);
            }

            public enum Position
            {
                middle,
                midTop,
                midBottom,
                midLeft,
                midRight,
                topLeft,
                topRight,
                bottomLeft,
                bottomRight
            }
        }

        public static Shape Rect(double w, double h)
        {
            return new Rect(w, h);
        }

        public static Shape Oval(double w, double h)
        {
            return new Oval(w, h);
        }

        public static Shape Square(double l)
        {
            return Rect(l, l);
        }

        public static Shape Circle(double r)
        {
            return Oval(r, r);
        }

        public static Shape Path(IEnumerable<Point> points)
        {
            var path = new Path(0, 0);
            foreach (var p in points)
                path.Add(p);
            return path;
        }

        public static Drawing ToDrawing(Shape shape, Brush b, Pen p)
        {
            return shape.DrawWith(b, p);
        }

        public static Drawing AsDrawing(LiveScripting.Element ele)
        {
            return ele.drawing.Clone();
        }

        internal static System.Windows.Rect RectFromDim(double w, double h)
        {
            return new System.Windows.Rect(PointZero, new Size(w, h));
        }
    }

    public abstract class Shape
    {
        internal readonly double width;
        internal readonly double height;

        internal Shape(double w, double h)
        {
            width = w; 
            height = h;
        }

        public DrawingGroup DrawWith(Brush b, Pen p)
        {
            return GroupFromDrawing(DrawingFromGeometry(GeometryForDrawing(), b, p));
        }

        internal virtual Geometry GeometryForDrawing()
        {
            return Geometry();
        }

        internal abstract Geometry Geometry();

        protected System.Windows.Rect Rect()
        {
            return new System.Windows.Rect(new Size(width, height));
        }

        private static DrawingGroup GroupFromDrawing(Drawing drawing)
        {
            DrawingGroup dg = new DrawingGroup();
            dg.Children.Add(drawing);
            return dg;
        }

        private static Drawing DrawingFromGeometry(Geometry g, Brush b = null, Pen p = null)
        {
            return new GeometryDrawing(b, p, g);
        }
    }

    internal class Rect : Shape
    {
        internal Rect(double w, double h) : base(w, h)
        {
        }

        internal override Geometry Geometry()
        {
            return new RectangleGeometry(Rect());
        }
    }

    internal class Oval : Shape
    {
        internal Oval(double w, double h) : base(w, h)
        {
        }

        internal override Geometry Geometry()
        {
            return new EllipseGeometry(Rect());
        }
    }

    internal class Path : Shape
    {
        internal readonly PathFigure pathFigure;
        private static readonly Point pointNaN = new Point(double.NaN, double.NaN);
        public Path(double w, double h) : base(w, h)
        {
            pathFigure = new PathFigure(pointNaN, new List<PathSegment>(), false);
        }

        internal void Add(Point p)
        {
            if (pathFigure.StartPoint.Equals(pointNaN))
                pathFigure.StartPoint = p;
            else
                pathFigure.Segments.Add(new LineSegment(p, false));
        }

        internal override Geometry GeometryForDrawing()
        {
            foreach (var segment in pathFigure.Segments)
                segment.IsStroked = true;
            return base.GeometryForDrawing();
        }

        internal override Geometry Geometry()
        {
            return new PathGeometry(new [] {pathFigure});
        }
    }

    public class Element
    {
        internal Drawing drawing;

        protected Element()
        {}

        internal Element(Drawing drawing)
        {
            this.drawing = drawing;
        }

        internal virtual void Draw(DrawingContext dc)
        {
            dc.DrawDrawing(drawing);
        }

        internal static Drawing AsDrawingGroup(Drawing drawing)
        {
            DrawingGroup dg = new DrawingGroup();
            dg.Children.Add(drawing);
            return dg;
        }
    }

    internal class Image : Element
    {
        internal readonly string src;

        internal Image(int w, int h, string src)
        {
            if(System.IO.File.Exists(src))
                drawing = AsDrawingGroup(new ImageDrawing(new BitmapImage(new Uri(src, UriKind.RelativeOrAbsolute)),
                    Graphics.RectFromDim(w, h)));
            else
                drawing = AsDrawingGroup(Graphics.AsDrawing(Graphics.Element.Text($"File '{src}' not found.")));

            this.src = src;
        }
    }

    internal class Text : Element
    {
        internal FormattedText formattedText;
        
        internal Text(string st)
        {
            formattedText = new FormattedText(st, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                14d, Brushes.Black);

            drawing = AsDrawingGroup(new GeometryDrawing(Brushes.Black, null,
                        formattedText.BuildGeometry(Graphics.PointZero)));
        }
    }

    public static class Transform
    {
        public static Drawing Scale(Drawing drawing, double sf)
        {
            return Scale(drawing, sf, sf);
        }

        public static Drawing Scale(Drawing drawing, double sfX, double sfY)
        {
            return ApplyTransform(drawing, new ScaleTransform(sfX, sfY));
        }

        public static Drawing Rotate(Drawing drawing, double rd)
        {
            return ApplyTransform(drawing, new RotateTransform(rd));
        }

        public static Drawing Move(Drawing drawing, double x, double y)
        {
            return ApplyTransform(drawing, new TranslateTransform(x, y));
        }

        private static Drawing ApplyTransform(Drawing drawing, System.Windows.Media.Transform t)
        {
            if (drawing == null)
                return null;

            if (!(drawing is DrawingGroup))
                drawing = Element.AsDrawingGroup(drawing.Clone());

            DrawingGroup dg = drawing as DrawingGroup;

            if (dg.Transform == null)
                dg.Transform = new TransformGroup();

            (dg.Transform as TransformGroup).Children.Add(t);

            return dg;
        }
    }
}
