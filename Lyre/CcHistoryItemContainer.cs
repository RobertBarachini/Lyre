﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class CcHistoryItemContainer : CcPanel
{
    private Label ccTitle;
    private PictureBox ccThumbnail;

    private HistoryItem historyItem;

    public CcHistoryItemContainer(HistoryItem historyItem)
    {
        this.historyItem = historyItem;

        InitComponents();
    }

    public HistoryItem getHistoryItem()
    {
        return historyItem;
    }

    private void InitComponents()
    {
        BackColor = Shared.preferences.colorBackground;
        DoubleBuffered = true;

        ccThumbnail = new PictureBox()
        {
            Parent = this,
            BackColor = Shared.preferences.colorBackground,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Cursor = Cursors.Hand
        };
        ccThumbnail.Click += CcThumbnail_Click; ;
        Controls.Add(ccThumbnail);
        setThumbnail();

        ccTitle = new Label()
        {
            Parent = this,
            Text = historyItem.title,
            ForeColor = Shared.preferences.colorFontDefault,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
            Cursor = Cursors.Hand
        };
        ccTitle.Click += CcTitle_Click; ;
        Controls.Add(ccTitle);
    }

    private void setThumbnail()
    {
        try
        {
            string pathSmall = Path.GetFileNameWithoutExtension(historyItem.path_thumbnail) + "_144" + ".jpg"/*Path.GetExtension(historyItem.path_thumbnail)*/;
            pathSmall = Path.Combine(Path.GetDirectoryName(historyItem.path_thumbnail), pathSmall);
            Image img;
            if(File.Exists(pathSmall) == false)
            {
                img = SharedFunctions.resizeImage(SharedFunctions.getImage(historyItem.path_thumbnail));
                img.Save(pathSmall);
            }
            else
            {
                img = SharedFunctions.getImage(pathSmall);
            }

            ccThumbnail.Image = img;
        }
        catch (Exception ex)
        {
            //ccThumbnail.BackColor = Color.Lime;
        }
    }

    private void CcThumbnail_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(historyItem.path_thumbnail);
        }
        catch (Exception ex) { }
    }

    private void CcTitle_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(historyItem.path_output);
        }
        catch (Exception ex) { }
    }

    public void ResizeComponents()
    {
        SuspendLayout();

        ccThumbnail.Height = Height;
        ccThumbnail.Width = (ccThumbnail.Height / 9) * 16;
        ccThumbnail.Left = Width - ccThumbnail.Width;
        ccThumbnail.Top = 0;

        ccTitle.Left = 30;
        ccTitle.Top = 15;
        ccTitle.Width = Width - ccThumbnail.Width - Left - ccTitle.Left;
        ccTitle.Height = 50;

        ResumeLayout();
    }
}