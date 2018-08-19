using System;
using System.Drawing;
using System.Windows.Forms;

class CcToggle : Control
{
    private Timer tAnimate;
    private double tLocation;
    private double tVelocity;

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
            tAnimate.Stop();
            tVelocity = 15;
            if (_isON == false)
            {
                tVelocity *= -1;
            }
            tAnimate.Start();

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
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        tLocation = 0;
        tAnimate = new Timer();
        tAnimate.Interval = 20;
        tAnimate.Tick += TAnimate_Tick;
        isON = false;
        this.Click += CcToggle_Click;
        DoubleClick += CcToggle_DoubleClick;
    }

    private void CcToggle_DoubleClick(object sender, EventArgs e)
    {
        isON = !isON;
    }

    private void CcToggle_Click(object sender, EventArgs e)
    {
        isON = !isON;
    }

    private void TAnimate_Tick(object sender, EventArgs e)
    {
        double cPos = tLocation;
        cPos += tVelocity;
        tVelocity = tVelocity * 0.6;
        double breakVal = 1.8;
        if(Math.Abs(tVelocity) < breakVal)
        {
            tVelocity = tVelocity < 0 ? - breakVal : breakVal;
        }
        if((cPos <= 0 /*&& _isON == false*/) || (cPos >= Width - Height/* && isON == true*/))
        {
            if(cPos < 0)
            {
                cPos = 0;
            }
            if(cPos > Width - Height)
            {
                cPos = Width - Height;
            }
            tAnimate.Stop();
        }
        tLocation = cPos;

        Invalidate();
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
        e.Graphics.FillEllipse(b1, (int)tLocation, 0, Height - 1, Height - 1);

        // Text
        b1 = new SolidBrush(isON ? _colorON : _colorOFF);
        string text = isON ? "ON" : "OFF";
        StringFormat sf = new StringFormat();
        sf.LineAlignment = StringAlignment.Center;
        sf.Alignment = StringAlignment.Center;
        e.Graphics.DrawString(text, Shared.preferences.fontDefault, b1, new Rectangle((int)tLocation, 0, Height - 1, Height), sf);

        b1.Dispose();
    }
}