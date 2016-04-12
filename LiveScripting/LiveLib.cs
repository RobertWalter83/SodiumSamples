using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiveScripting
{
    public static class Graphics
    {
        internal static readonly Point PointZero = new Point(0, 0);

        public static Collage Collage(int w, int h, params Element[] elements)
        {
            Collage collage = new Collage(w, h);
            foreach (var element in elements)
            {
                collage.Add(element.drawing);
            }

            return collage;
        }

        public static Rect Rect(double w, double h)
        {
            return new Rect (h, w);
        }

        public static Oval Oval(double w, double h)
        {
            return new Oval(h, w);
        }

        public static Image Image(int w, int h, string src)
        {
            return new Image(h, w, src);
        }

        public static Text Text(string st)
        {
            return new Text(st);
        }
    }

    public abstract class Element
    {
        internal readonly double width;
        internal readonly double height;
        internal DrawingGroup drawing;

        protected Element(double w, double h) 
        {
            width = w;
            height = h;
        }

        internal virtual void Draw(DrawingContext dc)
        {
            dc.DrawDrawing(drawing);
        }

        protected static DrawingGroup GroupFromDrawing(Drawing drawing)
        {
            DrawingGroup dg = new DrawingGroup();
            dg.Children.Add(drawing);
            return dg;
        }

        protected static System.Windows.Rect RectFromDim(double w, double h)
        {
            return new System.Windows.Rect(Graphics.PointZero, new Size(h, w));
        }

        protected static Drawing DrawingFromGeometry(Geometry geometry)
        {
            return new GeometryDrawing(null, new Pen(Brushes.Black, 1d), geometry);
        }
    }

    public class Rect : Element
    {
        internal Rect(double w, double h) : base(w, h)
        {
            drawing = GroupFromDrawing(DrawingFromGeometry(new RectangleGeometry(RectFromDim(w,h))));
        }
    }

    public class Oval : Element
    {
        internal Oval(double w, double h) : base(w, h)
        {
            drawing = GroupFromDrawing(DrawingFromGeometry(new EllipseGeometry(RectFromDim(w,h))));
        }
    }

    public class Image : Element
    {
        internal readonly string src;

        internal Image(double w, double h, string src) : base(w, h)
        {
            this.src = src;
            drawing = GroupFromDrawing(new ImageDrawing(new BitmapImage(new Uri(src, UriKind.RelativeOrAbsolute)),
                    RectFromDim(w, h)));
        }
    }

    public class Text : Element
    {
        internal FormattedText formattedText;

        internal Text(string st) : this(0, 0, st) { }

        internal Text(double w, double h, string st) : base(w, h)
        {
            formattedText = new FormattedText(st, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                14d, Brushes.Black);

            this.drawing = GroupFromDrawing(new GeometryDrawing(Brushes.Black, null,
                        formattedText.BuildGeometry(Graphics.PointZero)));
        }
    }

    public class Collage : Element
    {
        
        internal Collage(double w, double h) : base(w,h) 
        {
            drawing = new DrawingGroup();
        }

        internal void Add(Drawing dr)
        {
            this.drawing.Children.Add(dr);
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.PushClip(new RectangleGeometry(new System.Windows.Rect(new Size(width, height))));
            base.Draw(dc);
        }
    }

    public static class Transform
    {
        public static Element Scale(Element element, double sf)
        {
            return Scale(element, sf, sf);
        }

        public static Element Scale(Element element, double sfX, double sfY)
        {
            if(element.drawing.Transform == null)
                element.drawing.Transform = new TransformGroup();

            (element.drawing.Transform as TransformGroup).Children.Add(new ScaleTransform(sfX, sfY));
            
            return element;
        }

        public static Element Rotate(Element element, double rd)
        {
            if (element.drawing.Transform == null)
                element.drawing.Transform = new TransformGroup();

            (element.drawing.Transform as TransformGroup).Children.Add(new RotateTransform(rd));

            return element;
        }

        public static Element Move(Element element, double x, double y)
        {
            if (element.drawing.Transform == null)
                element.drawing.Transform = new TransformGroup();

            (element.drawing.Transform as TransformGroup).Children.Add(new TranslateTransform(x, y));

            return element;
        }
    }
}
