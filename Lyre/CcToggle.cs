using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

class CcToggle : Control
{
    private bool _isON;
    public bool isON
    {
        get
        {
            return _isON;
        }
        set
        {
            _isON = value;
            Invalidate();
        }
    }

    private Color _colorON;
    public Color colorON
    {
        get
        {
            return _colorON;
        }
        set
        {
            _colorON = value;
            Invalidate();
        }
    }

    private Color _colorOFF;
    public Color colorOFF
    {
        get
        {
            return _colorOFF;
        }
        set
        {
            _colorOFF = value;
            Invalidate();
        }
    }

    public CcToggle()
    {
        Cursor = Cursors.Hand;
        isON = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background
        Brush b1 = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(b1, ClientRectangle);

        // Bar
        b1 = new SolidBrush(isON ? _colorON : _colorOFF);
        e.Graphics.FillRectangle(b1, Height / 2, Height / 4, Width - Height, Height / 2);

        // Bar circle left
        e.Graphics.FillEllipse(b1, Height / 4, Height / 4, Height / 2, Height / 2);

        // Bar circle left
        e.Graphics.FillEllipse(b1, Width - (int)(3.0/4.0 * Height), Height / 4, Height / 2, Height / 2);

        // Circle ON/OFF
        b1 = new SolidBrush(ForeColor);
        e.Graphics.FillEllipse(b1, isON ? Width - Height : 0, 0, Height - 1, Height - 1);

        b1.Dispose();
    }
}