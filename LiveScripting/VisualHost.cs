using System.Windows;
using System.Windows.Media;

namespace LiveScripting
{
    public class VisualHost : FrameworkElement
    {
        public DrawingVisual Dv { get; }

        public VisualHost()
        {
            Dv = new DrawingVisual();
            this.AddVisualChild(Dv);
            this.AddLogicalChild(Dv);
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            return Dv;
        }
    }
}
