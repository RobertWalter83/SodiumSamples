using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Sodium;
using WPFMedia = System.Windows.Media; 

namespace LiveScripting
{
    public static class Text
    {
        public static FormattedText FromString(string st)
        {
            return new FormattedText(st, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                12d, Brushes.Black);
        }
    }

    public static class Setup
    {
        public static void RunVoid(Action setupAction)
        {
            Transaction.RunVoid(setupAction);
        }

        public static T Run<T>(Func<T> setupFunc)
        {
            return Transaction.Run<T>(setupFunc);
        }
    }

    public static class Graphics
    {
        public static WPFMedia.Drawing Collage(int width, int height, IEnumerable<dynamic> elements)
        {
            DrawingGroup dg = new DrawingGroup();
            foreach (var element in elements)
            {
                var drawing = Drawing.Show(element);
                dg.Children.Add(drawing);
            }

            return dg;
        }

        public static Rect Rect(double w, double h)
        {
            return new Rect {Height = h, Width = w};
        }

        public static Oval Oval(double w, double h)
        {
            return new Oval { Height = h, Width = w };
        }

        public static Image Image(int w, int h, string src)
        {
            return new Image { Height = h, Width = w, Src = src };
        }

        public static class Drawing
        {
            private static readonly Point pointZero = new Point(0,0);

            public static WPFMedia.Drawing Show(string st)
            {
                return Show(Text.FromString(st));
            }

            public static WPFMedia.Drawing Show(object obj)
            {
                return Show("Don't know how to render this: " + obj);
            }

            public static WPFMedia.Drawing Show(WPFMedia.Drawing drawing)
            {
                return drawing;
            }

            public static WPFMedia.Drawing Show(FormattedText text)
            {
                return new GeometryDrawing(Brushes.Black, null, text.BuildGeometry(pointZero));
            }

            public static WPFMedia.Drawing Show(Rect rect)
            {
                return DrawingFromGeometry(new RectangleGeometry(RectFromElem(rect)));
            }

            public static WPFMedia.Drawing Show(Oval oval)
            {
                return DrawingFromGeometry(new EllipseGeometry(RectFromElem(oval)));
            }

            public static WPFMedia.Drawing Show(Image image)
            {
                return new ImageDrawing(new BitmapImage(new Uri(image.Src, UriKind.RelativeOrAbsolute)),
                    RectFromElem(image));
            }

            private static System.Windows.Rect RectFromElem(Element elem)
            {
                return new System.Windows.Rect(pointZero, new Size(elem.Height, elem.Width));
            }

            private static WPFMedia.Drawing DrawingFromGeometry(Geometry geometry)
            {
                return new GeometryDrawing(null, new Pen(Brushes.Black, 1d), geometry);
            }
        }
    }

    public abstract class Element
    {
        internal double Width { get; set; }
        internal double Height { get; set; }
    }

    public class Rect : Element { }

    public class Oval : Element { }

    public class Image : Element
    {
        internal string Src { get; set; }
    }
}
