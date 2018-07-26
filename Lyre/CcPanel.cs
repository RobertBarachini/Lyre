using System.Drawing;
using System.Windows.Forms;

public class CcPanel : Panel
{
    // Fixing broken Panel scroll when losing and regaining focus ...

    // https://stackoverflow.com/questions/9306975/c-sharp-panel-with-autoscroll-srollbar-position-reset-on-a-control-focus
    protected override Point ScrollToControl(Control activeControl)
    {
        // Returning the current location prevents the panel from
        // scrolling to the active control when the panel loses and regains focus
        return this.DisplayRectangle.Location;
    }

    //// If it has padding
    //protected override Point ScrollToControl(Control activeControl)
    //{
    //    Point retPt = DisplayRectangle.Location;
    //    retPt.Offset(new Point(-1 * Padding.Left, -1 * Padding.Bottom));

    //    return retPt;
    //}
}