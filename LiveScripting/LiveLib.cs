using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiveScripting
{
    public static class Graphics
    {
        internal static readonly Point PointZero = new Point(0, 0);

        public static LiveScripting.Element Collage(int w, int h, params Drawing[] drawings)
        {
            DrawingGroup dg = new DrawingGroup {ClipGeometry = new RectangleGeometry(RectFromDim(w, h))};
            foreach (var drawing in drawings)
            {
                dg.Children.Add(drawing);
            }

            return new LiveScripting.Element(dg);
        }

        public static class Element
        {
            public static LiveScripting.Element Show(object a)
            {
                return Text(ToString(a));
            }

            private static string ToString(object a)
            {
                if (!a.GetType().IsArray)
                    return a.ToString();

                StringBuilder sb = new StringBuilder();
                sb.Append("[ ");
                var arr = a as object[];
                if (arr != null)
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        sb.Append(ToString(arr[i]));
                        
                        if (i+1 != arr.Length)
                            sb.Append(", ");
                    }
                }
                sb.Append(" ]");
                return sb.ToString();
            }

            public static LiveScripting.Element Image(int w, int h, string src)
            {
                return new Image(w, h, src);
            }

            public static LiveScripting.Element Text(string st)
            {
                return new Text(st);
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

        public static Drawing ToDrawing(Shape shape, Brush b, Pen p)
        {
            return shape.ToDrawing(b, p);
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

        internal DrawingGroup ToDrawing(Brush b, Pen p)
        {
            return GroupFromDrawing(DrawingFromGeometry(Geometry(), b, p));
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
            : base(AsDrawingGroup(new ImageDrawing(new BitmapImage(new Uri(src, UriKind.RelativeOrAbsolute)),
                Graphics.RectFromDim(w, h))))
        {
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
