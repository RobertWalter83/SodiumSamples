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
        internal static readonly Point PointZero = new Point(0, 0);

        public static Collage Collage(int w, int h, IEnumerable<dynamic> elements)
        {
            Collage collage = new Collage {Width = w, Height = h};
            foreach (var element in elements)
            {
                var eleNew = Drawing.Show(element);
                collage.Add(eleNew);
            }

            return collage;
        }

        public static Rect Rect(double w, double h)
        {
            return new Rect { Height = h, Width = w };
        }

        public static Oval Oval(double w, double h)
        {
            return new Oval { Height = h, Width = w };
        }

        public static Image Image(int w, int h, string src)
        {
            return new Image { Height = h, Width = w, Src = src };
        }

        public static Text Text(string st)
        {
            return LiveScripting.Text.FromString(st);
        }

        public static class Drawing
        {
            public static Element Show(object obj)
            {
                return Show("Don't know how to render this: " + obj);
            }

            public static Element Show(Element ele)
            {
                return ele;
            }

            public static Element Show(string st)
            {
                return Show(Text(st));
            }

            public static Element Show(Text text)
            {
                return text;
            }

            public static Element Show(Rect rect)
            {
                var clone = rect.Clone<Rect>();
                clone.Drawing = DrawingFromGeometry(new RectangleGeometry(RectFromElem(rect)));
                return clone;
            }

            public static Element Show(Oval oval)
            { 
                var clone = oval.Clone<Oval>();
                clone.Drawing = DrawingFromGeometry(new EllipseGeometry(RectFromElem(oval)));
                return clone;
            }

            public static Element Show(Image image)
            {
                var clone = image.Clone<Image>();
                clone.Drawing = new ImageDrawing(new BitmapImage(new Uri(image.Src, UriKind.RelativeOrAbsolute)),
                    RectFromElem(image));
                return clone;
            }

            private static System.Windows.Rect RectFromElem(Element elem)
            {
                return new System.Windows.Rect(PointZero, new Size(elem.Height, elem.Width));
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

        internal Drawing Drawing { get; set; }

        internal FormattedText Text { get; set; }

        internal abstract void Draw(DrawingContext dc);
        
        protected virtual void CloneAction(Element clone)
        {
            clone.Width = Width;
            clone.Height = Height;
        }

        internal T Clone<T>() where T : Element, new()
        {
            var t = new T();
            CloneAction(t);
            return t;
        }
    }

    public class Rect : Element
    {
        internal new FormattedText Text
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set Text for Rect."); }
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.DrawDrawing(Drawing);
        }

        protected override void CloneAction(Element clone)
        {
            base.CloneAction(clone);
            clone.Drawing = this.Drawing;
        }
    }

    public class Oval : Element
    {
        internal new FormattedText Text
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set Text for Oval."); }
        }

        protected override void CloneAction(Element clone)
        {
            base.CloneAction(clone);
            clone.Drawing = this.Drawing;
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.DrawDrawing(Drawing);
        }
    }

    public class Image : Element
    {
        internal string Src { get; set; }

        internal new FormattedText Text
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set Text for Image."); }
        }

        protected override void CloneAction(Element clone)
        {
            base.CloneAction(clone);
            clone.Drawing = Drawing;
            (clone as Image).Src = Src;
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.DrawDrawing(Drawing);
        }
    }

    public class Text : Element
    {
        public static Text FromString(string st)
        {
            return new Text {Text = new FormattedText(st, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                12d, Brushes.Black)};
        }

        internal new Drawing Drawing
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set 'Drawing' for Text."); }
        }

        protected override void CloneAction(Element clone)
        {
            base.CloneAction(clone);
            clone.Text = Text;
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.DrawText(Text, Graphics.PointZero);
        }
    }

    public class Collage : Element
    {
        internal readonly IList<Element> rgElement = new List<Element>();  

        public void Add(Element ele)
        {
            rgElement.Add(ele);
        }

        internal new Drawing Drawing
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set 'Drawing' for Collage."); }
        }

        internal new FormattedText Text
        {
            get { return null; }
            set { throw new InvalidOperationException("Cannot set 'Text' for Collage."); }
        }

        protected override void CloneAction(Element clone)
        {
            base.CloneAction(clone);
            foreach (var element in rgElement)
                (clone as Collage).Add(element);
        }

        internal override void Draw(DrawingContext dc)
        {
            dc.PushClip(new RectangleGeometry(new System.Windows.Rect(new Size(Width, Height))));
            
            foreach (var ele in rgElement)
                ele.Draw(dc);
        }
    }
}
