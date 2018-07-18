using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;

// This bit of code looks like a complete overkill but the end result looks pretty.

namespace Lyre
{
    class CcSettings : Panel
    {
        private ColorDialog ccPicker;
        private System.Windows.Forms.Timer timerStatusUpdater;

        private Label ccLabelSettins;
        private Panel ccBottomMargin;

        // Colors
        private Panel ccContainerColors;
        private Label ccTitleColors;

        private Label ccLabelColorFont;
        private Panel ccPanelColorFont;

        private Label ccLabelColorForeground;
        private Panel ccPanelColorForeground;

        private Label ccLabelColorBackground;
        private Panel ccPanelColorBackground;

        private Label ccLabelColorAccent1;
        private Panel ccPanelColorAccent1;

        private Label ccLabelColorAccent2;
        private Panel ccPanelColorAccent2;

        private Label ccLabelColorAccent3;
        private Panel ccPanelColorAccent3;

        private Label ccLabelColorAccent4;
        private Panel ccPanelColorAccent4;

        private Label ccLabelColorAccent5;
        private Panel ccPanelColorAccent5;

        private Label ccLabelColorAccent6;
        private Panel ccPanelColorAccent6;

        private Label ccLabelColorAccent7;
        private Panel ccPanelColorAccent7;

        // File folder
        private Panel ccContainerFiles;
        private Label ccTitleFiles;

        private Label ccLabelFolderDownloads;
        private RichTextBox ccTextFolderDownloads;
        private Panel ccButtonFolderDownloads;

        private Label ccLabelFolderTemp;
        private RichTextBox ccTextFolderTemp;
        private Panel ccButtonFolderTemp;

        public CcSettings()
        {
            this.DoubleBuffered = true;
            this.BackColor = Shared.preferences.colorBackground;
            this.SizeChanged += CcSettings_SizeChanged;
            this.AutoSize = true;

            ccPicker = new ColorDialog();
            ccPicker.AllowFullOpen = true;
            ccPicker.AnyColor = true;
            ccPicker.FullOpen = true;

            InitComponents();
            resizeComponents();

            timerStatusUpdater = new System.Windows.Forms.Timer();
            timerStatusUpdater.Interval = 1000;
            timerStatusUpdater.Tick += TimerStatusUpdater_Tick;
            timerStatusUpdater.Start();
        }

        private void TimerStatusUpdater_Tick(object sender, EventArgs e)
        {
            // Some settings should not be changed during the download process
            // Bug - changing a cursor for a specific control changes it globally
            if (getUnfinishedDownloadsCount() == 0)
            {
                //if (ccButtonFolderDownloads.Cursor != Cursors.Hand)
                //{
                //    ccButtonFolderDownloads.Cursor = Cursors.Hand;
                //}
                ccButtonFolderDownloads.BackColor = ccButtonFolderDownloads.Parent.BackColor;

                //if (ccButtonFolderTemp.Cursor != Cursors.Hand)
                //{
                //    ccButtonFolderTemp.Cursor = Cursors.Hand;
                //}
                ccButtonFolderTemp.BackColor = ccButtonFolderTemp.Parent.BackColor;
            }
            else
            {
                //if (ccButtonFolderDownloads.Cursor != Cursors.No)
                //{
                //    ccButtonFolderDownloads.Cursor = Cursors.No;
                //}
                ccButtonFolderDownloads.BackColor = Shared.preferences.colorAccent3;

                //if (ccButtonFolderTemp.Cursor != Cursors.No)
                //{
                //    ccButtonFolderTemp.Cursor = Cursors.No;
                //}
                ccButtonFolderTemp.BackColor = Shared.preferences.colorAccent3;
            }
        }

        private void CcSettings_SizeChanged(object sender, EventArgs e)
        {
            resizeComponents();
        }

        private void InitComponents()
        {
            ccLabelSettins = new Label();
            ccLabelSettins.Parent = this;
            this.Controls.Add(ccLabelSettins);
            ccLabelSettins.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelSettins.Font = new Font(Shared.preferences.fontDefault.FontFamily, 36, GraphicsUnit.Pixel);
            ccLabelSettins.Text = "Settings";
            ccLabelSettins.AutoSize = true;

            // Colors
            ccContainerColors = new Panel();
            ccContainerColors.Parent = this;
            this.Controls.Add(ccContainerColors);
            ccContainerColors.BackColor = Shared.preferences.colorBackground;//Shared.preferences.colorAccent2;
            ccContainerColors.AutoSize = true;

            ccTitleColors = new Label();
            ccTitleColors.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccTitleColors);
            ccTitleColors.ForeColor = Shared.preferences.colorFontDefault;
            ccTitleColors.Font = new Font(Shared.preferences.fontDefault.FontFamily, 28, GraphicsUnit.Pixel);
            ccTitleColors.Text = "Colors";
            ccTitleColors.AutoSize = true;


            ccLabelColorFont = new Label();
            ccLabelColorFont.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorFont);
            ccLabelColorFont.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorFont.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorFont.Text = "Font color:";
            ccLabelColorFont.AutoSize = true;

            ccPanelColorFont = new Panel();
            ccPanelColorFont.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorFont);
            ccPanelColorFont.BackColor = Shared.preferences.colorFontDefault;
            ccPanelColorFont.Cursor = Cursors.Hand;
            ccPanelColorFont.Click += CcPanelColorFont_Click;


            ccLabelColorForeground = new Label();
            ccLabelColorForeground.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorForeground);
            ccLabelColorForeground.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorForeground.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorForeground.Text = "Background light:";
            ccLabelColorForeground.AutoSize = true;

            ccPanelColorForeground = new Panel();
            ccPanelColorForeground.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorForeground);
            ccPanelColorForeground.BackColor = Shared.preferences.colorForeground;
            ccPanelColorForeground.Cursor = Cursors.Hand;
            ccPanelColorForeground.Click += CcPanelColorForeground_Click;


            ccLabelColorBackground = new Label();
            ccLabelColorBackground.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorBackground);
            ccLabelColorBackground.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorBackground.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorBackground.Text = "Background dark:";
            ccLabelColorBackground.AutoSize = true;

            ccPanelColorBackground = new Panel();
            ccPanelColorBackground.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorBackground);
            ccPanelColorBackground.BackColor = Shared.preferences.colorBackground;
            ccPanelColorBackground.Cursor = Cursors.Hand;
            ccPanelColorBackground.Click += CcPanelColorBackground_Click;
            ccPanelColorBackground.BorderStyle = BorderStyle.FixedSingle;


            ccLabelColorAccent1 = new Label();
            ccLabelColorAccent1.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent1);
            ccLabelColorAccent1.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent1.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent1.Text = "Accent 1:";
            ccLabelColorAccent1.AutoSize = true;

            ccPanelColorAccent1 = new Panel();
            ccPanelColorAccent1.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent1);
            ccPanelColorAccent1.BackColor = Shared.preferences.colorAccent1;
            ccPanelColorAccent1.Cursor = Cursors.Hand;
            ccPanelColorAccent1.Click += CcPanelColorAccent1_Click;

            ccLabelColorAccent2 = new Label();
            ccLabelColorAccent2.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent2);
            ccLabelColorAccent2.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent2.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent2.Text = "Accent 2:";
            ccLabelColorAccent2.AutoSize = true;

            ccPanelColorAccent2 = new Panel();
            ccPanelColorAccent2.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent2);
            ccPanelColorAccent2.BackColor = Shared.preferences.colorAccent2;
            ccPanelColorAccent2.Cursor = Cursors.Hand;
            ccPanelColorAccent2.Click += CcPanelColorAccent2_Click;


            ccLabelColorAccent3 = new Label();
            ccLabelColorAccent3.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent3);
            ccLabelColorAccent3.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent3.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent3.Text = "Accent 3:";
            ccLabelColorAccent3.AutoSize = true;

            ccPanelColorAccent3 = new Panel();
            ccPanelColorAccent3.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent3);
            ccPanelColorAccent3.BackColor = Shared.preferences.colorAccent3;
            ccPanelColorAccent3.Cursor = Cursors.Hand;
            ccPanelColorAccent3.Click += CcPanelColorAccent3_Click;


            ccLabelColorAccent4 = new Label();
            ccLabelColorAccent4.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent4);
            ccLabelColorAccent4.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent4.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent4.Text = "Accent 4:";
            ccLabelColorAccent4.AutoSize = true;

            ccPanelColorAccent4 = new Panel();
            ccPanelColorAccent4.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent4);
            ccPanelColorAccent4.BackColor = Shared.preferences.colorAccent4;
            ccPanelColorAccent4.Cursor = Cursors.Hand;
            ccPanelColorAccent4.Click += CcPanelColorAccent4_Click;


            ccLabelColorAccent5 = new Label();
            ccLabelColorAccent5.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent5);
            ccLabelColorAccent5.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent5.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent5.Text = "Accent 5:";
            ccLabelColorAccent5.AutoSize = true;

            ccPanelColorAccent5 = new Panel();
            ccPanelColorAccent5.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent5);
            ccPanelColorAccent5.BackColor = Shared.preferences.colorAccent5;
            ccPanelColorAccent5.Cursor = Cursors.Hand;
            ccPanelColorAccent5.Click += CcPanelColorAccent5_Click;

            ccLabelColorAccent6 = new Label();
            ccLabelColorAccent6.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent6);
            ccLabelColorAccent6.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent6.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent6.Text = "Accent 6:";
            ccLabelColorAccent6.AutoSize = true;

            ccPanelColorAccent6 = new Panel();
            ccPanelColorAccent6.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent6);
            ccPanelColorAccent6.BackColor = Shared.preferences.colorAccent6;
            ccPanelColorAccent6.Cursor = Cursors.Hand;
            ccPanelColorAccent6.Click += CcPanelColorAccent6_Click;


            ccLabelColorAccent7 = new Label();
            ccLabelColorAccent7.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccLabelColorAccent7);
            ccLabelColorAccent7.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelColorAccent7.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelColorAccent7.Text = "Accent 7:";
            ccLabelColorAccent7.AutoSize = true;

            ccPanelColorAccent7 = new Panel();
            ccPanelColorAccent7.Parent = ccContainerColors;
            ccContainerColors.Controls.Add(ccPanelColorAccent7);
            ccPanelColorAccent7.BackColor = Shared.preferences.colorAccent7;
            ccPanelColorAccent7.Cursor = Cursors.Hand;
            ccPanelColorAccent7.Click += CcPanelColorAccent7_Click;

            // File folder
            ccContainerFiles = new Panel();
            ccContainerFiles.Parent = this;
            this.Controls.Add(ccContainerFiles);
            ccContainerFiles.BackColor = Shared.preferences.colorBackground;
            ccContainerFiles.AutoSize = true;

            ccTitleFiles = new Label();
            ccTitleFiles.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccTitleFiles);
            ccTitleFiles.ForeColor = Shared.preferences.colorFontDefault;
            ccTitleFiles.Font = new Font(Shared.preferences.fontDefault.FontFamily, 28, GraphicsUnit.Pixel);
            ccTitleFiles.Text = "File folders";
            ccTitleFiles.AutoSize = true;


            ccLabelFolderDownloads = new Label();
            ccLabelFolderDownloads.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccLabelFolderDownloads);
            ccLabelFolderDownloads.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelFolderDownloads.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelFolderDownloads.Text = "Downloads folder: ";
            ccLabelFolderDownloads.AutoSize = true;

            ccTextFolderDownloads = new RichTextBox();
            ccTextFolderDownloads.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccTextFolderDownloads);
            ccTextFolderDownloads.ForeColor = Shared.preferences.colorFontDefault;
            ccTextFolderDownloads.BackColor = ccContainerFiles.BackColor;
            ccTextFolderDownloads.BorderStyle = BorderStyle.None;
            ccTextFolderDownloads.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccTextFolderDownloads.ReadOnly = true;
            ccTextFolderDownloads.WordWrap = true;
            ccTextFolderDownloads.Text = Path.GetFullPath(Shared.preferences.downloadsDirectory);

            ccButtonFolderDownloads = new Panel();
            ccButtonFolderDownloads.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccButtonFolderDownloads);
            ccButtonFolderDownloads.Cursor = Cursors.Hand;
            ccButtonFolderDownloads.BackgroundImageLayout = ImageLayout.Zoom;
            ccButtonFolderDownloads.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_IMG_Directory));
            ccButtonFolderDownloads.BackColor = ccContainerFiles.BackColor;
            ccButtonFolderDownloads.Click += ccButtonFolderDownloads_Click;


            ccLabelFolderTemp = new Label();
            ccLabelFolderTemp.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccLabelFolderTemp);
            ccLabelFolderTemp.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelFolderTemp.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccLabelFolderTemp.Text = "Temporary files: ";
            ccLabelFolderTemp.AutoSize = true;

            ccTextFolderTemp = new RichTextBox();
            ccTextFolderTemp.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccTextFolderTemp);
            ccTextFolderTemp.ForeColor = Shared.preferences.colorFontDefault;
            ccTextFolderTemp.BackColor = ccContainerFiles.BackColor;
            ccTextFolderTemp.BorderStyle = BorderStyle.None;
            ccTextFolderTemp.Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel);
            ccTextFolderTemp.ReadOnly = true;
            ccTextFolderTemp.WordWrap = true;
            ccTextFolderTemp.Text = Path.GetFullPath(Shared.preferences.tempDirectoy);

            ccButtonFolderTemp = new Panel();
            ccButtonFolderTemp.Parent = ccContainerFiles;
            ccContainerFiles.Controls.Add(ccButtonFolderTemp);
            ccButtonFolderTemp.Cursor = Cursors.Hand;
            ccButtonFolderTemp.BackgroundImageLayout = ImageLayout.Zoom;
            ccButtonFolderTemp.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_IMG_Directory));
            ccButtonFolderTemp.BackColor = ccContainerFiles.BackColor;
            ccButtonFolderTemp.Click += CcButtonFolderTemp_Click;

            // Bottom margin
            ccBottomMargin = new Panel();
            ccBottomMargin.Parent = this;
            this.Controls.Add(ccBottomMargin);
            ccBottomMargin.BackColor = this.BackColor;
        }

        private int getUnfinishedDownloadsCount()
        {
            int downloadsActive = DownloadContainer.getActiveProcessesCount();
            int downloadsInQueue = DownloadContainer.getDownloadsQueueCount();
            int downloadsInPreQueue = Shared.mainForm.getDownloadsPreQueueCount();

            int downloadsUnfinished = downloadsActive + downloadsInQueue + downloadsInPreQueue;

            return downloadsUnfinished;
        }

        private void CcButtonFolderTemp_Click(object sender, EventArgs e)
        {
            if (getUnfinishedDownloadsCount() == 0)
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Choose future temporary files destination directory.";
                    if (Shared.preferences.tempDirectoy.Equals("temp"))
                    {
                        folderDialog.SelectedPath = Path.Combine(Directory.GetCurrentDirectory(), Shared.preferences.tempDirectoy);
                    }
                    else
                    {
                        folderDialog.SelectedPath = Shared.preferences.tempDirectoy;
                    }
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        Shared.preferences.tempDirectoy = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void ccButtonFolderDownloads_Click(object sender, EventArgs e)
        {
            if (getUnfinishedDownloadsCount() == 0)
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Choose future downloads destination directory.";
                    if (Shared.preferences.downloadsDirectory.Equals("downloads"))
                    {
                        folderDialog.SelectedPath = Path.Combine(Directory.GetCurrentDirectory(), Shared.preferences.downloadsDirectory);
                    }
                    else
                    {
                        folderDialog.SelectedPath = Shared.preferences.downloadsDirectory;
                    }
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        Shared.preferences.downloadsDirectory = folderDialog.SelectedPath;
                    }
                }
            }
        }

        public static Image getImage(string path)
        {
            int counter = 0;
            while (System.IO.File.Exists(path) == false)
            {
                Application.DoEvents();
                Thread.Sleep(50);
                counter += 50;
                if (counter > 1000)
                {
                    break;
                }
            }
            try
            {
                Image img = Image.FromStream(new MemoryStream(System.IO.File.ReadAllBytes(path)));
                return img;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void CcPanelColorAccent7_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent7 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent6_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent6 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent5_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent5 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent4_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent4 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent3_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent3 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent2_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent2 = ccPicker.Color;
            }
        }

        private void CcPanelColorAccent1_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorAccent1 = ccPicker.Color;
            }
        }

        private void CcPanelColorBackground_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorBackground = ccPicker.Color;
            }
        }

        private void CcPanelColorForeground_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorForeground = ccPicker.Color;
            }
        }

        private void CcPanelColorFont_Click(object sender, EventArgs e)
        {
            Control senderControl = (Control)sender;
            ccPicker.Color = senderControl.BackColor;
            DialogResult result = ccPicker.ShowDialog();
            if (result == DialogResult.OK)
            {
                senderControl.BackColor = ccPicker.Color;
                // Specific setting to be edited
                Shared.preferences.colorFontDefault = ccPicker.Color;
            }
        }

        private void resizeComponents()
        {
            int bottomMargin = 30;
            int bottomMarginM = 20;
            int bottomMarginS = 10;

            ccLabelSettins.Left = 30;
            ccLabelSettins.Top = 70;

            // Colors
            ccContainerColors.Top = ccLabelSettins.Top + ccLabelSettins.Height + bottomMargin + 20;
            ccContainerColors.Left = ccLabelSettins.Left + bottomMarginM;
            ccContainerColors.Width = this.Width - (ccContainerColors.Left * 2);

            ccTitleColors.Top = 0;
            ccTitleColors.Left = 0; //bottomMarginS;


            ccLabelColorFont.Top = ccTitleColors.Top + ccTitleColors.Height + bottomMargin;
            ccLabelColorFont.Left = 0; // ccTitleColors.Left + bottomMarginS;

            ccPanelColorFont.Top = ccLabelColorFont.Top;
            ccPanelColorFont.Width = 50;
            ccPanelColorFont.Height = ccPanelColorFont.Width;
            ccPanelColorFont.Left = ccContainerColors.Width - ccPanelColorFont.Width;


            ccLabelColorForeground.Top = ccPanelColorFont.Top + ccPanelColorFont.Height + bottomMarginM;
            ccLabelColorForeground.Left = ccLabelColorFont.Left;

            ccPanelColorForeground.Top = ccLabelColorForeground.Top;
            ccPanelColorForeground.Width = ccPanelColorFont.Width;
            ccPanelColorForeground.Height = ccPanelColorForeground.Width;
            ccPanelColorForeground.Left = ccPanelColorFont.Left;


            ccLabelColorBackground.Top = ccPanelColorForeground.Top + ccPanelColorForeground.Height + bottomMarginM;
            ccLabelColorBackground.Left = ccLabelColorFont.Left;

            ccPanelColorBackground.Top = ccLabelColorBackground.Top;
            ccPanelColorBackground.Width = ccPanelColorFont.Width;
            ccPanelColorBackground.Height = ccPanelColorBackground.Width;
            ccPanelColorBackground.Left = ccPanelColorFont.Left;


            ccLabelColorAccent1.Top = ccPanelColorBackground.Top + ccPanelColorBackground.Height + bottomMarginM;
            ccLabelColorAccent1.Left = ccLabelColorFont.Left;

            ccPanelColorAccent1.Top = ccLabelColorAccent1.Top;
            ccPanelColorAccent1.Width = ccPanelColorFont.Width;
            ccPanelColorAccent1.Height = ccPanelColorFont.Width;
            ccPanelColorAccent1.Left = ccPanelColorFont.Left;


            ccLabelColorAccent2.Top = ccPanelColorAccent1.Top + ccPanelColorAccent1.Height + bottomMarginM;
            ccLabelColorAccent2.Left = ccLabelColorFont.Left;

            ccPanelColorAccent2.Top = ccLabelColorAccent2.Top;
            ccPanelColorAccent2.Width = ccPanelColorFont.Width;
            ccPanelColorAccent2.Height = ccPanelColorFont.Width;
            ccPanelColorAccent2.Left = ccPanelColorFont.Left;


            ccLabelColorAccent3.Top = ccPanelColorAccent2.Top + ccPanelColorAccent2.Height + bottomMarginM;
            ccLabelColorAccent3.Left = ccLabelColorFont.Left;

            ccPanelColorAccent3.Top = ccLabelColorAccent3.Top;
            ccPanelColorAccent3.Width = ccPanelColorFont.Width;
            ccPanelColorAccent3.Height = ccPanelColorFont.Width;
            ccPanelColorAccent3.Left = ccPanelColorFont.Left;


            ccLabelColorAccent4.Top = ccPanelColorAccent3.Top + ccPanelColorAccent3.Height + bottomMarginM;
            ccLabelColorAccent4.Left = ccLabelColorFont.Left;

            ccPanelColorAccent4.Top = ccLabelColorAccent4.Top;
            ccPanelColorAccent4.Width = ccPanelColorFont.Width;
            ccPanelColorAccent4.Height = ccPanelColorFont.Width;
            ccPanelColorAccent4.Left = ccPanelColorFont.Left;


            ccLabelColorAccent5.Top = ccPanelColorAccent4.Top + ccPanelColorAccent4.Height + bottomMarginM;
            ccLabelColorAccent5.Left = ccLabelColorFont.Left;

            ccPanelColorAccent5.Top = ccLabelColorAccent5.Top;
            ccPanelColorAccent5.Width = ccPanelColorFont.Width;
            ccPanelColorAccent5.Height = ccPanelColorFont.Width;
            ccPanelColorAccent5.Left = ccPanelColorFont.Left;

            ccLabelColorAccent6.Top = ccPanelColorAccent5.Top + ccPanelColorAccent5.Height + bottomMarginM;
            ccLabelColorAccent6.Left = ccLabelColorFont.Left;

            ccPanelColorAccent6.Top = ccLabelColorAccent6.Top;
            ccPanelColorAccent6.Width = ccPanelColorFont.Width;
            ccPanelColorAccent6.Height = ccPanelColorFont.Width;
            ccPanelColorAccent6.Left = ccPanelColorFont.Left;


            ccLabelColorAccent7.Top = ccPanelColorAccent6.Top + ccPanelColorAccent6.Height + bottomMarginM;
            ccLabelColorAccent7.Left = ccLabelColorFont.Left;

            ccPanelColorAccent7.Top = ccLabelColorAccent7.Top;
            ccPanelColorAccent7.Width = ccPanelColorFont.Width;
            ccPanelColorAccent7.Height = ccPanelColorFont.Width;
            ccPanelColorAccent7.Left = ccPanelColorFont.Left;

            // File folder
            ccContainerFiles.Top = ccContainerColors.Top + ccContainerColors.Height + bottomMargin + 20;
            ccContainerFiles.Left = ccLabelSettins.Left + bottomMarginM;
            ccContainerFiles.Width = this.Width - (ccContainerFiles.Left * 2);

            ccTitleFiles.Top = 0;
            ccTitleFiles.Left = 0; //bottomMarginS;


            ccLabelFolderDownloads.Top = ccTitleFiles.Top + ccTitleFiles.Height + bottomMargin;
            ccLabelFolderDownloads.Left = 0;

            ccButtonFolderDownloads.Top = ccLabelFolderDownloads.Top;
            ccButtonFolderDownloads.Width = 50;
            ccButtonFolderDownloads.Height = ccButtonFolderDownloads.Width;
            ccButtonFolderDownloads.Left = ccContainerFiles.Width - ccButtonFolderDownloads.Width;

            ccTextFolderDownloads.Top = ccLabelFolderDownloads.Top;
            ccTextFolderDownloads.Left = ccLabelFolderDownloads.Left + ccLabelFolderDownloads.Width + bottomMargin;
            ccTextFolderDownloads.Width = ccButtonFolderDownloads.Left - (bottomMargin * 2) - ccLabelFolderDownloads.Width - ccLabelFolderDownloads.Left;
            ccTextFolderDownloads.Height = (int)(ccTextFolderDownloads.Font.Size * 3 * 1.5);


            ccLabelFolderTemp.Top = ccTextFolderDownloads.Top + ccTextFolderDownloads.Height + bottomMargin;
            ccLabelFolderTemp.Left = 0;

            ccButtonFolderTemp.Top = ccLabelFolderTemp.Top;
            ccButtonFolderTemp.Width = 50;
            ccButtonFolderTemp.Height = ccButtonFolderTemp.Width;
            ccButtonFolderTemp.Left = ccButtonFolderDownloads.Left;

            ccTextFolderTemp.Top = ccLabelFolderTemp.Top;
            ccTextFolderTemp.Left = ccLabelFolderDownloads.Left + ccLabelFolderDownloads.Width + bottomMargin;
            ccTextFolderTemp.Width = ccButtonFolderDownloads.Left - (bottomMargin * 2) - ccLabelFolderTemp.Width - ccLabelFolderTemp.Left;
            ccTextFolderTemp.Height = (int)(ccTextFolderTemp.Font.Size * 3 * 1.5);


            // Bottom margin
            ccBottomMargin.Height = 1;
            ccBottomMargin.Width = 1;
            ccBottomMargin.Left = ccContainerFiles.Left;
            ccBottomMargin.Top = ccContainerFiles.Top + ccContainerFiles.Height + 100;
        }
    }
}
