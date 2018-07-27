using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class CcHistoryViewer : CcPanel
{
    private static LinkedList<CcHistoryItemContainer> hiControls = new LinkedList<CcHistoryItemContainer>();
    private static object historyListLock;
    private static Timer timerRefreshMissingHi;
    private static string searchPlaceholder = "Search";
    private static int minimumRequiredLength = 2;

    private static CcPanel ccSearchContainer;
    private static TextBox ccSearch;

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

        ccSearchContainer = new CcPanel()
        {
            Parent = this,
            BackColor = Shared.preferences.colorBackground
        };
        Controls.Add(ccSearchContainer);

        ccSearch = new TextBox()
        {
            Parent = ccSearchContainer,
            BorderStyle = BorderStyle.None,
            BackColor = Shared.preferences.colorBackground,
            ForeColor = Shared.preferences.colorFontDefault,
            Font = new Font(Shared.preferences.fontDefault.FontFamily, 26, GraphicsUnit.Pixel),
            Text = searchPlaceholder
        };
        ccSearch.TextChanged += CcSearch_TextChanged;
        ccSearch.GotFocus += CcSearch_GotFocus;
        ccSearch.LostFocus += CcSearch_LostFocus;
        ccSearchContainer.Controls.Add(ccSearch);
    }

    private void CcSearch_LostFocus(object sender, EventArgs e)
    {
        if(ccSearch.Text.Length < minimumRequiredLength)
        {
            ccSearch.Text = searchPlaceholder;
        }
    }

    private void CcSearch_GotFocus(object sender, EventArgs e)
    {
        if(ccSearch.Text.Equals(searchPlaceholder))
        {
            ccSearch.Text = "";
        }
    }

    private void CcSearch_TextChanged(object sender, EventArgs e)
    {
        if(ccSearch.Text.Length < minimumRequiredLength)
        {
            filterReset();
        }
        else
        {
            if (ccSearch.ContainsFocus)
            {
                filterVisible(ccSearch.Text);
            }
        }
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
        LinkedList<HistoryItem> intermediateList = new LinkedList<HistoryItem>();
        int added = 0;

        lock (historyListLock)
        {
            added = Shared.history.Count - hiControls.Count;
            int counter = 0;
            foreach (HistoryItem hi in Shared.history)
            {
                if(counter >= added)
                {
                    break;
                }

                counter++;
                intermediateList.AddFirst(hi);
            }
        }

        foreach(HistoryItem hi in intermediateList)
        {
            CcHistoryItemContainer newHi = new CcHistoryItemContainer(hi)
            {
                Parent = this
            };

            Controls.Add(newHi);
            hiControls.AddFirst(newHi);
            Application.DoEvents();
        }

        if (added > 0)
        {
            //filterVisible("mix");
            ResizeComponents();
        }

        //Shared.mainForm.Text = hiControls.Count.ToString();
    }

    // Search - Filter by video title, url, ...
    private void filterVisible(string match)
    {
        string matchS = SharedFunctions.getSearchString(match);

        // Not yet implemented
        if (match.Length < minimumRequiredLength) // 3
        {
            return;
        }

        foreach(CcHistoryItemContainer hiC in hiControls)
        {
            HistoryItem hi = hiC.getHistoryItem();
            if(SharedFunctions.getSearchString(hi.title).Contains(matchS))
            {
                hiC.Visible = true;
            }
            else
            {
                hiC.Visible = false;
            }
        }

        ResizeComponents();
    }

    private void filterReset()
    {
        foreach (CcHistoryItemContainer hiC in hiControls)
        {
            hiC.Visible = true;
        }
        ResizeComponents();
    }

    private void ResizeComponents()
    {
        if(Visible == false)
        {
            return;
        }

        SuspendLayout();

        int counter = 0;
        int hiHeight = 80;
        int hiWidth = Width - 200;
        int hiLeft = (Width - hiWidth) / 2;
        int bottomMargin = 30;

        ccSearchContainer.Top = 50;
        ccSearchContainer.Left = hiLeft;
        ccSearchContainer.Height = 60;
        ccSearchContainer.Width = hiWidth;

        ccSearch.Left = 32;
        ccSearch.Width = ccSearchContainer.Width - (ccSearch.Left * 2);
        ccSearch.Height = ccSearchContainer.Height;
        ccSearch.Top = (ccSearchContainer.Height - ccSearch.Height) / 2;

        int topPoint = ccSearchContainer.Top + ccSearchContainer.Height + bottomMargin; //50;
        

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

                Application.DoEvents();
            }
        }

        ResumeLayout();
    }
}
