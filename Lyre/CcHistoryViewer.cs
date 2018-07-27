using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class CcHistoryViewer : CcPanel
{
    private static LinkedList<CcHistoryItemContainer> hiControls = new LinkedList<CcHistoryItemContainer>();
    private static object historyListLock;
    private static Timer timerRefreshMissingHi;

    public CcHistoryViewer()
    {
        historyListLock = new object();

        InitComponents();

        timerRefreshMissingHi = new Timer();
        timerRefreshMissingHi.Interval = 1000;
        timerRefreshMissingHi.Tick += TimerRefreshMissingHi_Tick;
        timerRefreshMissingHi.Start();
    }

    private void InitComponents()
    {
        BackColor = Shared.preferences.colorForeground;
        SizeChanged += CcHistoryViewer_SizeChanged;
        VisibleChanged += CcHistoryViewer_VisibleChanged;
        DoubleBuffered = true;
        AutoScroll = true;
    }

    private void CcHistoryViewer_VisibleChanged(object sender, EventArgs e)
    {
        if(Visible)
        {
            resumeRefreshing();
        }
        else
        {
            stopRefreshing();
        }
    }

    private void CcHistoryViewer_SizeChanged(object sender, EventArgs e)
    {
        try
        {
            ResizeComponents();
        }
        catch(Exception ex) { }
    }

    private void TimerRefreshMissingHi_Tick(object sender, EventArgs e)
    {
        timerRefreshMissingHi.Stop();

        addMissingHiControls();

        timerRefreshMissingHi.Start();
    }

    // When reentering the control
    public void resumeRefreshing()
    {
        timerRefreshMissingHi.Start();
    }

    // When leaving the control aka. minimizing, changing to invisible, ...
    public void stopRefreshing()
    {
        timerRefreshMissingHi.Stop();
    }

    // Add missing controls
    private void addMissingHiControls()
    {
        int added = 0;
        lock (historyListLock)
        {
            if(Shared.history.Count == 0)
            {
                return;
            }

            HistoryItem currentFirst;
            if (hiControls.Count == 0)
            {
                currentFirst = null;
            }
            else
            {
                currentFirst = hiControls.First.Value.getHistoryItem();
            }

            foreach (HistoryItem hi in Shared.history)
            {
                // Only add new components
                // On init loads all history items
                // On refresh adds only new downloaded items
                if(/*currentFirst != null &&*/ hi == currentFirst /*|| hiControls.Count >= 50*/)
                {
                    break;
                }

                CcHistoryItemContainer newHi = new CcHistoryItemContainer(hi)
                {
                    Parent = this
                };
                Controls.Add(newHi);

                hiControls.AddLast(newHi);

                added++;
                Application.DoEvents();
            }
        }

        if (added > 0)
        {
            ResizeComponents();
        }
        Shared.mainForm.Text = hiControls.Count.ToString();
    }

    // Search - Filter by video title, url, ...
    private void filterVisible(string match)
    {
        // Not yet implemented
    }

    private void ResizeComponents()
    {
        if(Visible == false)
        {
            return;
        }

        SuspendLayout();

        int topPoint = 50;
        int counter = 0;
        int hiHeight = 80;
        int hiWidth = Width - 200;
        int hiLeft = (Width - hiWidth) / 2;
        int bottomMargin = 30;

        foreach(CcHistoryItemContainer hi in hiControls)
        {
            if (hi.Visible)
            {
                hi.Top = topPoint + ((hiHeight + bottomMargin) * counter);
                hi.Left = hiLeft;
                hi.Width = hiWidth;
                hi.Height = hiHeight;

                hi.ResizeComponents();

                counter++;
            }
        }

        ResumeLayout();
    }
}
