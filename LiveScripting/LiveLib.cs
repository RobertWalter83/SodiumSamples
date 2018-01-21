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
        internal static class Util
    {
        internal static Tuple<T, T> AsTuple2<T>(this T item)
        {
            return Tuple.Create(item, item);
        }

        internal static Tuple<T, T, T> AsTuple3<T>(this T item)
        {
            return Tuple.Create(item, item, item);
        }
    }
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
        public static Cell<Point> PosCell { get { return PosStream.Calm().Hold(Graphics.PointZero); } }

        public static Cell<Tuple<MouseButtonState, MouseButtonState, MouseButtonState>> ButtonsCell
        {
            get { return ButtonsStream.Calm().Hold(MouseButtonState.Released.AsTuple3()); }
        }

        public static Stream<Point> PosStream { get; internal set; }

        public static Stream<Tuple<MouseButtonState, MouseButtonState, MouseButtonState>> ButtonsStream
        { get; internal set; }
    }

    public static class Keyboard
    {
        public static Cell<Tuple<int, int>> ArrowsCell
        {
            get { return ArrowsStream.Calm().Hold(0.AsTuple2()); }
        }

        public static Cell<Tuple<int, int>> WasdCell
        {
            get { return WasdStream.Calm().Hold(0.AsTuple2()); }
        }

        public static Cell<bool> SpaceCell
        {
            get { return SpaceStream.Calm().Hold(false); }
        }

        public static Stream<Tuple<int, int>> ArrowsStream { get; internal set; }
        public static Stream<Tuple<int, int>> WasdStream { get; internal set; }
        public static Stream<bool> SpaceStream { get; internal set; }
    }

    public static class Screen
    {
        public static Cell<Size> Size { get; internal set; }
    }

    public static class Graphics
    {
        internal static readonly Point PointZero = new Point(0, 0);
        
        public static Element Collage(int w, int h, params Drawing[] drawings)
        {
            DrawingGroup dg = new DrawingGroup {ClipGeometry = new RectangleGeometry(RectFromDim(w, h))};
            foreach (var drawing in drawings)
            {
                if (drawing == null)
                    continue;
                dg.Children.Add(drawing);
            }

            return new Element(dg);
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

        public static Shape Ngon(int n, double r)
        {
            return Path(Enumerable.Range(0, n).Select(i => NGonPoint(n, r, i)));
        }

        private static Point NGonPoint(int n, double r, int i)
        {
            return new Point(NGonCoord(n, r, i, Math.Cos), NGonCoord(n, r, i, Math.Sin));
        }

        private static double NGonCoord(int n, double r, int i, Func<double, double> funcTrigonometry)
        {
            var angle = 2 * i * Math.PI / n;
            return r * funcTrigonometry.Invoke(angle);
        }

        public static Drawing ToDrawing(Shape shape, Brush b, Pen p)
        {
            return shape.DrawWith(b, p);
        }

        public static Drawing AsDrawing(Element ele)
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
            if (!pathFigure.StartPoint.Equals(((LineSegment)pathFigure.Segments.Last()).Point))
            {
                pathFigure.Segments.Add(new LineSegment(pathFigure.StartPoint, false));
            }
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
        { }

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
        public static Element Show<T>(T t)
        {
            return Text(ToString(t));
        }

        public static Element Show(object a)
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

        public static Element Image(int w, int h, string src)
        {
            return new Image(w, h, src);
        }

        public static Element Text(string st)
        {
            return new Text(st);
        }

        public static Element Container(int w, int h, Position p, Element e)
        {
            var shapeContainer = Graphics.Rect(w, h);
            var drawingContainer = Graphics.ToDrawing(shapeContainer, Brushes.Transparent, null);
            var drawingContent = Graphics.AsDrawing(e);
            return Graphics.Collage(w, h, drawingContainer, TransformTo(w, h, p, drawingContent));
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
                    offsetX = dx / 2;
                    offsetY = dy / 2;
                    break;
                case Position.midTop:
                    offsetX = dx / 2;
                    break;
                case Position.midBottom:
                    offsetX = dx / 2;
                    offsetY = dy;
                    break;
                case Position.midLeft:
                    offsetY = dy / 2;
                    break;
                case Position.midRight:
                    offsetX = dx;
                    offsetY = dy / 2;
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

    internal class Image : Element
    {
        internal readonly string src;

        internal Image(int w, int h, string src)
        {
            if(System.IO.File.Exists(src))
                drawing = AsDrawingGroup(new ImageDrawing(new BitmapImage(new Uri(src, UriKind.RelativeOrAbsolute)),
                    Graphics.RectFromDim(w, h)));
            else
                drawing = AsDrawingGroup(Graphics.AsDrawing(Element.Text($"File '{src}' not found.")));

            this.src = src;
        }
    }

    public class Text : Element
    {
        internal readonly FormattedText formattedText;
        internal readonly Brush foreground;
        internal readonly CultureInfo culture;
        internal readonly FlowDirection flow;
        internal readonly FontFamily fontFamily;
        internal readonly FontStyle fontStyle;
        internal readonly FontWeight fontWeight;
        internal readonly FontStretch fontStretch;
        internal readonly double size;

        private static readonly Brush foregroundDefault = Brushes.Black;

        private FormattedText FormattedText(string st)
        {
            return new FormattedText(st, culture, flow,
                new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                size, foregroundDefault);
        }

        public static Text FromString(string st)
        {
            return new Text(st);
        }

        public static Text Foreground(Text text, Brush foreground)
        {
            return new Text(text, foreground);
        }

        public static Text Bold(Text text)
        {
            return new Text(text, FontWeights.Bold);
        }

        public static Text Size(Text text, double size)
        {
            return new Text(text, size);
        }

        public static Text Italic(Text text)
        {
            return new Text(text, FontStyles.Italic);
        }

        public static Text Decoration(Text text, TextDecoration decoration, params TextDecoration[] moreDecorations)
        {
            var textNew = new Text(text);
            TextDecorationCollection tdc = new TextDecorationCollection {decoration};

            foreach (var more in moreDecorations)
                tdc.Add(more);

            textNew.formattedText.SetTextDecorations(tdc);
            textNew.SetDrawing();
            return textNew;
        }

        public static Text Flow(FlowDirection flow, Text text)
        {
            return new Text(text, flow);
        }

        public static Text Family(string stFamily, Text text)
        {
            return new Text(text, new FontFamily(stFamily));
        }

        internal Text(string st)
            : this(
                st, 14d, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new FontFamily("Consolas"),
                FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, Brushes.Black)
        {

        }

        private Text(Text text)
            : this(
                text.formattedText.Text, text.size, text.culture, text.flow, text.fontFamily, text.fontStyle,
                text.fontWeight, text.fontStretch, text.foreground)
        {

        }

        private Text(Text text, Brush foreground)
            : this(
                text.formattedText.Text, text.size, text.culture, text.flow, text.fontFamily, text.fontStyle,
                text.fontWeight, text.fontStretch, foreground)
        {

        }

        private Text(Text text, FontFamily family)
            : this(
                text.formattedText.Text, text.size, text.culture, text.flow, family, text.fontStyle, text.fontWeight,
                text.fontStretch, text.foreground)
        {

        }

        private Text(Text text, FlowDirection flow)
            : this(
                text.formattedText.Text, text.size, text.culture, flow, text.fontFamily, text.fontStyle, text.fontWeight,
                text.fontStretch, text.foreground)
        {
        }

        private Text(Text text, FontStyle style)
            : this(
                text.formattedText.Text, text.size, text.culture, text.flow, text.fontFamily, style, text.fontWeight,
                text.fontStretch, text.foreground)
        {
        }

        private Text(Text text, FontWeight weight)
            : this(
                text.formattedText.Text, text.size, text.culture, text.flow, text.fontFamily, text.fontStyle, weight,
                text.fontStretch, text.foreground)
        {
        }

        private Text(Text text, double size)
            : this(
                text.formattedText.Text, size, text.culture, text.flow, text.fontFamily, text.fontStyle, text.fontWeight,
                text.fontStretch, text.foreground)
        {
        }

        private Text(string st, double size, CultureInfo culture, FlowDirection flow, FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch, Brush foreground)
        {
            this.size = size;
            this.flow = flow;
            this.culture = culture;
            this.fontFamily = fontFamily;
            this.fontStyle = fontStyle;
            this.fontWeight = fontWeight;
            this.fontStretch = fontStretch;
            formattedText = FormattedText(st);
            this.foreground = foreground;
            SetDrawing();
        }

        private void SetDrawing()
        {
            drawing = AsDrawingGroup(new GeometryDrawing(foreground, null,
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
