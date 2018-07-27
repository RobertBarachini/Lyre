﻿//42 ; 
// by Robert Barachini

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace Lyre
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        //public static Preferences preferences; // preferences object

        //
        ////  Controls
        //
        private CcPanel ccContainer;
        private CcPanel ccTopBar;
        private CcPanel ccDownloadsContainer;
        private DownloadContainer dcMain;
        private CcPanel ccFormMinimize;
        private CcPanel ccFormMaximize;
        private CcPanel ccFormClose;
        private CcPanel ccDownloadsDirectory;
        private Label ccHint;
        private Label ccVideoQuality;
        private Label ccHistoryButton;
        private CcHistoryViewer ccHistoryViewer;
        private CcPanel ccSettingsButton;
        private CcPanel ccSettingsContainer;
        private CcSettings ccSettingsPanel;
        private CcPanel ccSettingsBottomMargin;
        private RichTextBox ccResourceDownloaderLog;

        // Status Bar
        private CcPanel ccStatusBar;
        private Label ccDownloadsText;
        private Label ccDownloadsValue;
        private Label ccActiveDownloadsText;
        private Label ccActiveDownloadsValue;
        private Label ccCanConvertText;
        private CcToggle ccCanConvert;

        // Instructions
        private CcPanel ccPanelInstructions;
        private RichTextBox ccTextInstructions;

        private object resourcesMissingCountLock = new object();
        private int resourcesMissingCount;
        private Queue<string> downloadsPreQueue = new Queue<string>();
        private System.Windows.Forms.Timer timerStatusUpdater;
        private System.Windows.Forms.Timer timerPreQueueHandler;
        private bool noPreferencesFound;

        private void Form1_Load(object sender, EventArgs e)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (File.Exists("LyreLibrary.dll") == false)
            {
                WebClient wc1 = new WebClient();
                wc1.Proxy = null;
                wc1.DownloadFile("https://robertbarachini.github.io/projects/Lyre/resources/LyreLibrary.dll", "LyreLibrary.dll");
            }

            Form1_Load_Call();
        }

        public int getDownloadsPreQueueCount()
        {
            return downloadsPreQueue.Count;
        }

        private void Form1_Load_Call()
        {
            Shared.mainForm = this;

            noPreferencesFound = File.Exists(Shared.filePreferences);

            resourcesMissingCount = SharedFunctions.getResourcesMissingCount(OnlineResource.resourcesListDownloader);
            try // if a dll is missing
            {
                loadSources();
            }
            catch(Exception ex)
            {
                //preferences = new Preferences();
            }
            InitComponents();
            //try
            //{
            //    InitComponents();
            //}
            //catch(Exception ex)
            //{
            //    File.Delete(Shared.filePreferences);
            //    //saveJSON(Shared.filePreferences, Shared.preferences);
            //    //loadJSON(Shared.filePreferences, ref Shared.preferences);
            //    loadSources();
            //    InitComponents();
            //}
            if (resourcesMissingCount == 0)
            {
                loadDlQueue();
            }
            ResizeComponents();
            //this.Show();
            if (resourcesMissingCount > 0)
            {
                getResources();
            }

            timerStatusUpdater = new System.Windows.Forms.Timer()
            {
                Interval = 1000
            };
            timerStatusUpdater.Tick += TimerStatusUpdater_Tick;
            timerStatusUpdater.Start();

            timerPreQueueHandler = new System.Windows.Forms.Timer()
            {
                Interval = 1000
            };
            timerPreQueueHandler.Tick += TimerPreQueueHandler_Tick;
            timerPreQueueHandler.Start();

            this.BringToFront();
        }

        private void TimerPreQueueHandler_Tick(object sender, EventArgs e)
        {
            timerPreQueueHandler.Stop();
            handlePreQueue();
            timerPreQueueHandler.Start();
        }

        private void handlePreQueue()
        {
            int downloadsActive = DownloadContainer.getActiveProcessesCount();
            int downloadsInQueue = DownloadContainer.getDownloadsQueueCount();

            int downloadsOngoing = downloadsActive + downloadsInQueue;
            while(downloadsOngoing < Shared.preferences.maxDownloadContainerControls)
            {
                if(downloadsPreQueue.Count == 0)
                {
                    break;
                }
                string newVideoID = downloadsPreQueue.Dequeue();
                newDownload(newVideoID);
                //Application.DoEvents();
                downloadsOngoing++;
            }
        }

        private void TimerStatusUpdater_Tick(object sender, EventArgs e)
        {
            updateStatusBar();
        }

        private void updateStatusBar()
        {
            int downloadsActive = DownloadContainer.getActiveProcessesCount();

            int downloadsUnfinished = Shared.getUnfinishedDownloadsCount();
            int maximumControls = Shared.preferences.maxDownloadContainerControls;
            int maximumActive = Shared.preferences.maxActiveProcesses;

            ccDownloadsValue.Text = downloadsUnfinished.ToString() + " / " + maximumControls.ToString();
            ccActiveDownloadsValue.Text = downloadsActive.ToString() + " / " + maximumActive.ToString();

            // Bug - changing a cursor for a specific control changes it globally
            //if(downloadsUnfinished == 0)
            //{
            //    if (ccDownloadsDirectory.Cursor != Cursors.Hand)
            //    {
            //        ccDownloadsDirectory.Cursor = Cursors.Hand;
            //    }
            //}
            //else
            //{
            //    if (ccDownloadsDirectory.Cursor != Cursors.No)
            //    {
            //        ccDownloadsContainer.Cursor = Cursors.No;
            //    }
            //}
        }

        private void getResources()
        {
            foreach(OnlineResource onR in OnlineResource.resourcesListDownloader)
            {
                List<string> missingFiles = new List<string>();
                foreach (string path in onR.paths)
                {
                    if (File.Exists(path) == false)
                    {
                        missingFiles.Add(path);
                    }
                }
                if(missingFiles.Count > 0)
                {
                    ccResourceDownloaderLog.AppendText("Downloading missing resource" + Environment.NewLine);
                    ccResourceDownloaderLog.AppendText("Credit: " + onR.credit + Environment.NewLine);
                    ccResourceDownloaderLog.AppendText("URL: " + onR.url + Environment.NewLine + Environment.NewLine);
                    // fetch the missing file from the web
                    DownloaderAsync newDA = new DownloaderAsync(missingFiles);
                    setDownloaderEvents(newDA);
                    newDA.download(onR.url);
                }
            }
        }

        private void setDownloaderEvents(DownloaderAsync whichToSet)
        {
            whichToSet.MyDownloadCompleted += DA_MyDownloadCompleted;
            //whichToSet.MyDownloadChanged += DA_MyDownloadChanged; // not needed right now
        }

        private void DA_MyDownloadChanged(DownloaderAsync sender, DownloadProgressChangedEventArgs e)
        {
            double progress = (double)e.BytesReceived / (double)e.TotalBytesToReceive;
            try
            {
                ccResourceDownloaderLog.AppendText(sender.filename + " : [PROGRESS] " + (progress * 100).ToString("0.000") + " % " + Environment.NewLine);
                ccResourceDownloaderLog.ScrollToCaret();
            }
            catch (Exception ex) { }
        }

        private void DA_MyDownloadCompleted(DownloaderAsync sender, DownloadDataCompletedEventArgs e)
        {
            ccResourceDownloaderLog.AppendText(sender.filename + " : [PROGRESS] Done" + Environment.NewLine);
            ccResourceDownloaderLog.AppendText(sender.filename + " : " + sender.url.ToString() + ":" + Environment.NewLine);
            ccResourceDownloaderLog.AppendText(sender.filename + " : [BYTES SIZE] " + sender.totalBytesToReceive.ToString() + Environment.NewLine);
            ccResourceDownloaderLog.AppendText(sender.filename + " : [TIME NEEDED] (ms): " + Math.Round(sender.timeNeeded.TotalMilliseconds) + Environment.NewLine + Environment.NewLine);
            ccResourceDownloaderLog.ScrollToCaret();
            try
            {
                lock (resourcesMissingCountLock)
                {
                    resourcesMissingCount--;
                }

                foreach (string outputPath in sender.outputPaths)
                {
                    string path = "";
                    string senderPath = outputPath;

                    if (senderPath.Contains("\\"))
                    {
                        path = senderPath.Substring(0, senderPath.LastIndexOf("\\"));
                    }

                    if (path.Length > 0)
                    {
                        Directory.CreateDirectory(path);
                    }

                    // write the file
                    File.Create(outputPath).Close();
                    File.WriteAllBytes(outputPath, sender.data);
                }
            }
            catch (Exception ex)
            {
                ccResourceDownloaderLog.AppendText(sender.filename + Environment.NewLine + ex.ToString() + Environment.NewLine + Environment.NewLine);
            }

            lock(resourcesMissingCountLock)
            {
                if(resourcesMissingCount == 0)
                {
                    ccResourceDownloaderLog.AppendText("Press 'ESCAPE' button to restart the app ..." + Environment.NewLine);
                    ccResourceDownloaderLog.ScrollToCaret();
                }
            }
        }

        private void loadSources()
        {
            SharedFunctions.loadJSON(Shared.filePreferences, ref Shared.preferences);
            SharedFunctions.loadJSON(Shared.filenameHistory, ref Shared.history);
        }

        private void loadDlQueue()
        {
            LinkedList<string> urls = new LinkedList<string>();
            SharedFunctions.loadJSON(Shared.filenameDlQueue, ref urls);
            if (resourcesMissingCount == 0) // first download resources and then populate and resume downloads
            {
                foreach (string s in urls)
                {
                    downloadsPreQueue.Enqueue(s);
                }
            }
        }

        private void saveSources()
        {
            SharedFunctions.saveJSON(Shared.filePreferences, Shared.preferences);
            SharedFunctions.saveJSON(Shared.filenameHistory, Shared.history);

            // if files ar missing and are being rebuilt downloads queue is not activated
            // therefore no objects are created and without the condition we overwrite
            // valid downloads in the queue
            if (resourcesMissingCount != -1)
            {
                LinkedList<string> urls = new LinkedList<string>();
                foreach (DownloadContainer dc in DownloadContainer.getDownloadsAccess())
                {
                    if (dc.isFinished() == false)
                    {
                        urls.AddLast(dc.getURL());
                    }
                    //// Kill the process
                    //try
                    //{
                    //    dc.getSingleDownload().Kill();
                    //}
                    //catch(Exception ex)
                    //{

                    //}
                }
                foreach (string s in downloadsPreQueue)
                {
                    urls.AddLast(s);
                }
                SharedFunctions.saveJSON(Shared.filenameDlQueue, urls);
            }
        }

        private void InitComponents()
        {
            Text = "Lyre - A music app by Robert Barachini";
            FormClosing += Form1_FormClosing;
            DoubleBuffered = true;
            Width = Shared.preferences.formWidth;
            Height = Shared.preferences.formHeight;
            Top = Shared.preferences.formTop;
            Height = Shared.preferences.formHeight;
            SizeChanged += Form1_SizeChanged;
            KeyDown += Paste_KeyDown;
            KeyUp += Paste_KeyUp;
            BackColor = Color.Lime;//Shared.preferences.colorForeground;
            //FormBorderStyle = FormBorderStyle.None;
            MouseMove += Form1_MouseMove;
            MouseDown += Form1_MouseDown;
            KeyPreview = true;

            ccContainer = new CcPanel()
            {
                Parent = this,
                BackColor = Shared.preferences.colorForeground,
                Dock = DockStyle.Fill
            };
            Controls.Add(ccContainer);

            ccTopBar = new CcPanel()
            {
                Parent = ccContainer,
                BackColor = Shared.preferences.colorBackground
            };
            ccTopBar.MouseDown += Form1_MouseDown;
            ccContainer.Controls.Add(ccTopBar);

            ccDownloadsContainer = new CcPanel()
            {
                Parent = ccContainer,
                BackColor = Shared.preferences.colorForeground,
                AutoScroll = true,
                AutoSize = false
            };
            ccDownloadsContainer.KeyDown += Paste_KeyDown;
            ccDownloadsContainer.KeyUp += Paste_KeyUp;
            ccContainer.Controls.Add(ccDownloadsContainer);

            ccSettingsContainer = new CcPanel()
            {
                Parent = ccContainer,
                BackColor = Shared.preferences.colorForeground,
                AutoScroll = true
            };
            ccContainer.Controls.Add(ccSettingsContainer);

            ccSettingsPanel = new CcSettings()
            {
                Parent = ccSettingsContainer
            };
            //ccSettingsPanel.AutoScroll = true;
            ccSettingsContainer.Controls.Add(ccSettingsPanel);

            ccSettingsBottomMargin = new CcPanel()
            {
                Parent = ccSettingsContainer,
                BackColor = ccSettingsContainer.BackColor
            };
            ccSettingsContainer.Controls.Add(ccSettingsBottomMargin);

            ccFormMinimize = new CcPanel()
            {
                Parent = ccTopBar,
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Zoom,
                BackgroundImage = SharedFunctions.getImage(Path.Combine(OnlineResource.resourcesDirectory, OnlineResource.FormControls_Minimize)),
                BackColor = ccTopBar.BackColor,
                Visible = false
            };
            ccFormMinimize.Click += CcFormMinimize_Click;
            ccTopBar.Controls.Add(ccFormMinimize);

            ccFormMaximize = new CcPanel()
            {
                Parent = ccTopBar,
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Zoom,
                BackgroundImage = SharedFunctions.getImage(Path.Combine(OnlineResource.resourcesDirectory, OnlineResource.FormControls_Maximize)),
                BackColor = ccTopBar.BackColor,
                Visible = false
            };
            ccFormMaximize.Click += CcFormMaximize_Click;
            ccTopBar.Controls.Add(ccFormMaximize);

            ccFormClose = new CcPanel()
            {
                Parent = ccTopBar,
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Zoom,
                BackgroundImage = SharedFunctions.getImage(Path.Combine(OnlineResource.resourcesDirectory, OnlineResource.FormControls_CloseBig)),
                BackColor = ccTopBar.BackColor,
                Visible = false
            };
            ccFormClose.Click += CcFormClose_Click;
            ccTopBar.Controls.Add(ccFormClose);

            ccDownloadsDirectory = new CcPanel()
            {
                Parent = ccTopBar,
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Zoom,
                BackgroundImage = SharedFunctions.getImage(Path.Combine(OnlineResource.resourcesDirectory, OnlineResource.FormControls_IMG_Directory)),
                BackColor = ccTopBar.BackColor,
                Visible = false
            };
            ccDownloadsDirectory.Click += CcDownloadsDirectory_Click;
            ccTopBar.Controls.Add(ccDownloadsDirectory);

            ccHint = new Label()
            {
                Parent = ccTopBar,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel),
                Text = "ALPHA preview : Paste Youtube links anywhere, really ...",
                ForeColor = Shared.preferences.colorFontDefault,
                BackColor = Shared.preferences.colorBackground,
                Visible = false
            };
            ccTopBar.Controls.Add(ccHint);

            ccSettingsButton = new CcPanel()
            {
                Parent = ccTopBar,
                Cursor = Cursors.Hand,
                BackgroundImageLayout = ImageLayout.Zoom,
                BackgroundImage = SharedFunctions.getImage(Path.Combine(OnlineResource.resourcesDirectory, OnlineResource.FormControls_IMG_Settings)),
                BackColor = ccTopBar.BackColor,
                Visible = true
            };
            ccSettingsButton.Click += CcSettings_Click;
            ccTopBar.Controls.Add(ccSettingsButton);

            // Status Bar
            ccStatusBar = new CcPanel()
            {
                Parent = ccDownloadsContainer,
                BackColor = Shared.preferences.colorBackground
            };
            ccStatusBar.BringToFront();
            ccDownloadsContainer.Controls.Add(ccStatusBar);

            // Instructions
            ccPanelInstructions = new CcPanel()
            {
                Parent = ccDownloadsContainer,
                BackColor = Shared.preferences.colorBackground
            };
            ccPanelInstructions.BringToFront();
            ccDownloadsContainer.Controls.Add(ccPanelInstructions);
            if (noPreferencesFound)
            {
                ccPanelInstructions.Visible = false;
            }

            ccTextInstructions = new RichTextBox()
            {
                Parent = ccPanelInstructions,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel),
                Text = Shared.instructions
            };
            ccTextInstructions.LinkClicked += CcTextInstructions_LinkClicked;
            ccPanelInstructions.Controls.Add(ccTextInstructions);


            ccDownloadsText = new Label()
            {
                Parent = ccStatusBar,
                BackColor = ccStatusBar.BackColor,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
                Text = "Downloads:",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            ccStatusBar.Controls.Add(ccDownloadsText);

            ccDownloadsValue = new Label()
            {
                Parent = ccStatusBar,
                BackColor = ccStatusBar.BackColor,
                ForeColor = Shared.preferences.colorAccent2,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
                Text = "0 / 0",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            ccDownloadsValue.MouseClick += CcDownloadsValue_MouseClick;
            ccDownloadsValue.MouseDoubleClick += CcDownloadsValue_MouseDoubleClick;
            ccStatusBar.Controls.Add(ccDownloadsValue);

            ccActiveDownloadsText = new Label()
            {
                Parent = ccStatusBar,
                BackColor = ccStatusBar.BackColor,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
                Text = "Active downloads:",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            ccStatusBar.Controls.Add(ccActiveDownloadsText);

            ccActiveDownloadsValue = new Label()
            {
                Parent = ccStatusBar,
                BackColor = ccStatusBar.BackColor,
                ForeColor = Shared.preferences.colorAccent2,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
                Text = "0 / 0",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            ccActiveDownloadsValue.MouseClick += ccActiveDownloadsValue_MouseClick;
            ccActiveDownloadsValue.MouseDoubleClick += ccActiveDownloadsValue_MouseDoubleClick;
            ccStatusBar.Controls.Add(ccActiveDownloadsValue);

            ccCanConvertText = new Label()
            {
                Parent = ccStatusBar,
                BackColor = ccStatusBar.BackColor,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
                Text = "Convert to .mp3",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            ccStatusBar.Controls.Add(ccCanConvertText);

            ccCanConvert = new CcToggle()
            {
                Parent = ccStatusBar,
                isON = Shared.preferences.canConvert,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                colorON = Shared.preferences.colorAccent2,
                colorOFF = Shared.preferences.colorAccent3
            };
            ccCanConvert.Click += CcCanConvert_Click;
            ccStatusBar.Controls.Add(ccCanConvert);

            ccVideoQuality = new Label()
            {
                Parent = ccTopBar,
                BackColor = ccTopBar.BackColor,
                ForeColor = Shared.preferences.colorAccent2, // colorAccent4/7
                Cursor = Cursors.Hand,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel),
                AutoSize = true
            };
            ccVideoQuality.MouseClick += CcVideoQuality_MouseClick;
            ccVideoQuality.MouseDoubleClick += CcVideoQuality_MouseDoubleClick;
            ccTopBar.Controls.Add(ccVideoQuality);
            updateCcVideoQualityText();

            ccHistoryButton = new Label()
            {
                Parent = ccTopBar,
                BackColor = ccTopBar.BackColor,
                ForeColor = Shared.preferences.colorFontDefault,
                Cursor = Cursors.Hand,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 24, GraphicsUnit.Pixel),
                AutoSize = true,
                Text = "Hi"
            };
            ccHistoryButton.Click += CcHistoryButton_Click;
            ccTopBar.Controls.Add(ccHistoryButton);

            // Resource Downloader (debug oriented)
            ccResourceDownloaderLog = new RichTextBox()
            {
                Parent = ccDownloadsContainer,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 16, GraphicsUnit.Pixel),
                ReadOnly = true
            };
            ccResourceDownloaderLog.KeyDown += CcResourceDownloaderLog_KeyDown;
            ccResourceDownloaderLog.LinkClicked += CcResourceDownloaderLog_LinkClicked;
            ccDownloadsContainer.Controls.Add(ccResourceDownloaderLog);
            if (resourcesMissingCount == 0)
            {
                ccResourceDownloaderLog.Visible = false;
            }
            else
            {
                ccStatusBar.Visible = false;
                ccPanelInstructions.Visible = false;
            }

            // Show the desired container - downloadsContainer is default
            turnOnContainerInvisibility();
            ccDownloadsContainer.Visible = true;
        }

        private void CcHistoryButton_Click(object sender, EventArgs e)
        {
            // Init the viewer - not initialized at start to conserve resources
            if(ccHistoryViewer == null)
            {
                ccHistoryViewer = new CcHistoryViewer()
                {
                    Parent = ccContainer,
                    BackColor = Shared.preferences.colorForeground,
                    AutoScroll = true,
                    AutoSize = false
                };
                ccContainer.Controls.Add(ccHistoryViewer);
                ccHistoryViewer.Visible = false;
            }

            if(ccHistoryViewer.Visible)
            {
                turnOnContainerInvisibility();
                ccDownloadsContainer.Visible = true;
                ccHistoryButton.ForeColor = Shared.preferences.colorFontDefault;
            }
            else
            {
                turnOnContainerInvisibility();
                ccHistoryViewer.Visible = true;
                ccHistoryButton.ForeColor = Shared.preferences.colorAccent2;
                ResizeComponents();
            }
        }

        private void CcTextInstructions_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void ccActiveDownloadsValue_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Shared.preferences.maxActiveProcesses = Math.Min(Math.Min(Shared.preferences.maxDownloadContainerControls, Shared.preferences.maxActiveProcesses + 4), int.MaxValue);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Shared.preferences.maxActiveProcesses = Math.Max(1, Shared.preferences.maxActiveProcesses - 4);
            }

            updateStatusBar();
        }

        private void ccActiveDownloadsValue_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Shared.preferences.maxActiveProcesses = Math.Min(Math.Min(Shared.preferences.maxDownloadContainerControls, Shared.preferences.maxActiveProcesses + 1), int.MaxValue);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Shared.preferences.maxActiveProcesses = Math.Max(1, Shared.preferences.maxActiveProcesses - 1);
            }

            updateStatusBar();
        }

        private void CcDownloadsValue_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Shared.preferences.maxDownloadContainerControls = Math.Min(Shared.preferences.maxDownloadContainerControls + 4, int.MaxValue);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Shared.preferences.maxDownloadContainerControls = Math.Max(1, Shared.preferences.maxDownloadContainerControls - 4);
                Shared.preferences.maxActiveProcesses = Math.Min(Shared.preferences.maxDownloadContainerControls, Shared.preferences.maxActiveProcesses);
            }

            updateStatusBar();
        }

        private void CcDownloadsValue_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                Shared.preferences.maxDownloadContainerControls = Math.Min(Shared.preferences.maxDownloadContainerControls + 1, int.MaxValue);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Shared.preferences.maxDownloadContainerControls = Math.Max(1, Shared.preferences.maxDownloadContainerControls - 1);
                Shared.preferences.maxActiveProcesses = Math.Min(Shared.preferences.maxDownloadContainerControls, Shared.preferences.maxActiveProcesses);
            }

            updateStatusBar();
        }

        private void CcCanConvert_Click(object sender, EventArgs e)
        {
            Shared.preferences.canConvert = ccCanConvert.isON;
        }

        private void CcVideoQuality_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //Shared.increaseVideoQuality();
                Shared.increaseVideoQuality();
            }
            else if (e.Button == MouseButtons.Right)
            {
                //Shared.decreaseVideoQuality();
                Shared.decreaseVideoQuality();
            }

            updateCcVideoQualityText();
        }

        private void CcVideoQuality_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                Shared.increaseVideoQuality();
            }
            else if(e.Button == MouseButtons.Right)
            {
                Shared.decreaseVideoQuality();
            }

            updateCcVideoQualityText();
        }

        // https://stackoverflow.com/a/1592899
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void updateCcVideoQualityText()
        {
            ccVideoQuality.Text = Shared.getVideoQualityString(Shared.preferences.maxVideoQualitySelector) + " " + Shared.getVideoFrameRatePure(Shared.preferences.maxVideoFrameRateSelector);
        }

        private void CcResourceDownloaderLog_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void CcResourceDownloaderLog_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
            {
                lock(resourcesMissingCountLock)
                {
                    if(resourcesMissingCount == 0)
                    {
                        ccResourceDownloaderLog.Visible = false;

                        // Restart the app for the full functionality
                        resourcesMissingCount = -1;
                        Application.Restart();
                    }
                }
            }
        }

        private void CcSettings_Click(object sender, EventArgs e)
        {
            if(ccSettingsContainer.Visible)
            {
                turnOnContainerInvisibility();
                ccDownloadsContainer.Visible = true;
            }
            else
            {
                ccHistoryButton.ForeColor = Shared.preferences.colorFontDefault;
                turnOnContainerInvisibility();
                ccSettingsContainer.Visible = true;
            }
        }

        private void turnOnContainerInvisibility()
        {
            // All of the main container control panels
            ccDownloadsContainer.Visible = false;
            ccSettingsContainer.Visible = false;
            if(ccHistoryViewer != null)
            {
                ccHistoryViewer.Visible = false;
            }
        }

        private void CcDownloadsDirectory_Click(object sender, EventArgs e)
        {
            if (Shared.getUnfinishedDownloadsCount() == 0)
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

        private void ResizeComponents()
        {
            this.SuspendLayout();

            //Shared.preferences.formWidth = this.Width;
            //Shared.preferences.formHeight = this.Height;

            //ccContainer.Top = 0;
            //ccContainer.Left = 0;
            //ccContainer.Width = this.Width;
            //ccContainer.Height = this.Height;

            ccTopBar.Top = 0;
            ccTopBar.Left = 0;
            ccTopBar.Width = ccContainer.Width;
            ccTopBar.Height = 50;

            ccDownloadsContainer.Top = ccTopBar.Top + ccTopBar.Height;
            ccDownloadsContainer.Left = 0;
            ccDownloadsContainer.Width = ccContainer.Width;
            ccDownloadsContainer.Height = ccContainer.Height - ccTopBar.Height;

            ccSettingsContainer.Top = ccTopBar.Top + ccTopBar.Height;
            ccSettingsContainer.Left = 0;
            ccSettingsContainer.Width = ccContainer.Width;
            ccSettingsContainer.Height = ccContainer.Height - ccTopBar.Height;

            if (ccHistoryViewer != null && ccHistoryViewer.Visible)
            {
                ccHistoryViewer.Top = ccTopBar.Top + ccTopBar.Height;
                ccHistoryViewer.Left = 0;
                ccHistoryViewer.Width = ccContainer.Width;
                ccHistoryViewer.Height = ccContainer.Height - ccTopBar.Height;
            }

            ccSettingsPanel.Top = 50;
            //ccSettingsPanel.MaximumSize = new Size(800, ccSettingsPanel.Height); // resizing issues...
            ccSettingsPanel.Width = 800; //Math.Min(800, ccSettingsContainer.Width - 30); //800;
            ccSettingsPanel.Left = Math.Max(0, (ccSettingsContainer.Width - ccSettingsPanel.Width) / 2);
            //this.Text = ccSettingsContainer.Width + "/" + ccSettingsPanel.Width;

            ccSettingsBottomMargin.Top = ccSettingsPanel.Top + ccSettingsPanel.Height + 50;
            ccSettingsBottomMargin.Height = 1;

            int barMargin = 10;
            ccFormClose.Top = barMargin;
            ccFormClose.Height = ccTopBar.Height - (ccFormClose.Top * 2);
            ccFormClose.Width = ccFormClose.Height;
            ccFormClose.Left = ccTopBar.Width - ccFormClose.Width - barMargin;

            ccFormMaximize.Top = ccFormClose.Top;
            ccFormMaximize.Height = ccFormClose.Height;
            ccFormMaximize.Width = ccFormClose.Width;
            ccFormMaximize.Left = ccFormClose.Left - ccFormMaximize.Width - barMargin;

            ccFormMinimize.Top = ccFormClose.Top;
            ccFormMinimize.Height = ccFormClose.Height;
            ccFormMinimize.Width = ccFormClose.Width;
            ccFormMinimize.Left = ccFormMaximize.Left - ccFormMinimize.Width - barMargin;

            //ccHint.Top = barMargin;
            //ccHint.Left = ccDownloadsDirectory.Left + ccDownloadsDirectory.Width + barMargin;
            //ccHint.Width = 500;
            //ccHint.Height = ccFormClose.Height;

            ccSettingsButton.Top = barMargin;
            ccSettingsButton.Left = barMargin;
            ccSettingsButton.Width = ccFormClose.Width;
            ccSettingsButton.Height = ccFormClose.Height;

            //ccDownloadsDirectory.Top = barMargin;
            //ccDownloadsDirectory.Left = ccSettingsButton.Left + ccSettingsButton.Width + barMargin;
            //ccDownloadsDirectory.Height = ccFormClose.Height;
            //ccDownloadsDirectory.Width = ccFormClose.Width;

            ccVideoQuality.Top = barMargin;
            ccVideoQuality.Left = ccSettingsButton.Left + ccSettingsButton.Width + barMargin;
            ccVideoQuality.Height = ccFormClose.Height;

            ccHistoryButton.Top = barMargin - 3;
            ccHistoryButton.Left = ccTopBar.Width - ccHistoryButton.Width - barMargin + 3;
            ccHistoryButton.Height = ccFormClose.Height;

            // status bar
            ccStatusBar.Top = 50; // ccSettingsButton.Top + ccSettingsButton.Height + barMargin;
            ccStatusBar.Height = 70;
            ccStatusBar.Left = 50;
            ccStatusBar.Width = ccContainer.Width - (2 * ccStatusBar.Left);

            ccDownloadsText.Top = 18;
            ccDownloadsValue.Top = ccDownloadsText.Top;
            ccActiveDownloadsText.Top = ccDownloadsText.Top;
            ccActiveDownloadsValue.Top = ccDownloadsText.Top;
            ccDownloadsText.Left = 30;
            ccDownloadsValue.Left = ccDownloadsText.Left + ccDownloadsText.Width + 5;
            ccActiveDownloadsText.Left = ccDownloadsValue.Left + ccDownloadsValue.Width + 30;
            ccActiveDownloadsValue.Left = ccActiveDownloadsText.Left + ccActiveDownloadsText.Width + 5;
            ccDownloadsText.Height = ccStatusBar.Height;
            ccDownloadsValue.Height = ccDownloadsText.Height;
            ccActiveDownloadsText.Height = ccDownloadsText.Height;
            ccActiveDownloadsValue.Height = ccDownloadsText.Height;

            ccCanConvert.Top = ccDownloadsText.Top;
            ccCanConvert.Width = 70;
            ccCanConvert.Left = ccStatusBar.Width - ccCanConvert.Width - 30;
            ccCanConvert.Height = ccStatusBar.Height - (2 * ccCanConvert.Top);

            ccCanConvertText.Top = ccDownloadsText.Top;
            ccCanConvertText.Left = ccCanConvert.Left - 180;


            ccPanelInstructions.Top = ccStatusBar.Top + ccStatusBar.Height + 50;
            ccPanelInstructions.Left = ccStatusBar.Left;
            ccPanelInstructions.Width = ccStatusBar.Width;
            ccPanelInstructions.Height = 500;

            ccTextInstructions.Top = 30;
            ccTextInstructions.Left = 30;
            ccTextInstructions.Width = ccTextInstructions.Parent.Width - (2 * ccTextInstructions.Left);
            ccTextInstructions.Height = ccTextInstructions.Parent.Height - (2 * ccTextInstructions.Top);

            // download containers
            if (DownloadContainer.getDownloadsAccess().Count > 0)
            {
                resizeDcMain();
            }

            this.ResumeLayout();
        }

        public void resizeDcMain()
        {
            try
            {
                dcMain = DownloadContainer.getDownloadsAccess().First.Value;
                dcMain.Top = ccStatusBar.Top + ccStatusBar.Height + 30;
                dcMain.Left = 50;
                dcMain.Width = ccDownloadsContainer.Width - (dcMain.Left * 2);
                dcMain.Height = 100;
            }
            catch(Exception ex)
            {

            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void CcFormClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CcFormMaximize_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void CcFormMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (resourcesMissingCount != -1)
            {
                saveSources();
            }

            // this kills all DownloadContainer processes as well
            DownloadContainer.removeAllControls();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                ResizeComponents();
            }
            catch (Exception ex)
            {

            }
        }

        private bool copyPasteDown = false;
        private void Paste_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Control && e.KeyCode == Keys.V && copyPasteDown == false && ccResourceDownloaderLog.Visible == false)
            {
                ccPanelInstructions.Visible = false;

                // queue download url-s
                string clipboardString = Clipboard.GetText();
                LinkedList<string> hits = new LinkedList<string>();
                string pattern = "https://www.youtube.com/watch?v=";
                int counter = 0;
                while (true)
                {
                    int index = clipboardString.IndexOf(pattern);
                    if (index == -1)
                    {
                        break;
                    }

                    // youtube video_IDs are currently 11 chars long
                    string hit = clipboardString.Substring(index, pattern.Length + 11);
                    clipboardString = clipboardString.Substring(index + pattern.Length + 11);
                    if (clipboardString.Length >= "&list=".Length + 34 && clipboardString.Substring(0, "&list=".Length).Equals("&list=")) // 34 current list_ID length
                    {
                        //hit += clipboardString.Substring(0, "&list=".Length + 34);
                        string listHit = clipboardString.Substring(0, "&list=".Length + 34);
                        // youtube-dl --flat-playlist -j "https://www.youtube.com/list=PLSdoVPM5Wnne47ib65gVG206M7qp43us-"
                        // start youtube-dl process - get individual video IDs on process exit
                        Process singleDownload = new Process();
                        string arguments = "--flat-playlist -j \"" + hit + listHit + "\"";
                        singleDownload.StartInfo.FileName = @"youtube-dl.exe";
                        singleDownload.StartInfo.Arguments = arguments;
                        singleDownload.StartInfo.CreateNoWindow = true;
                        singleDownload.StartInfo.UseShellExecute = false;
                        singleDownload.StartInfo.RedirectStandardOutput = true;
                        singleDownload.OutputDataReceived += SingleDownload_OutputDataReceived;
                        singleDownload.EnableRaisingEvents = true;
                        singleDownload.Exited += SingleDownload_Exited; ;
                        singleDownload.Start();
                        singleDownload.BeginOutputReadLine();
                    }
                    hits.AddLast(hit);
                    counter++;
                }
                foreach (string l in hits)
                {
                    downloadsPreQueue.Enqueue(l);
                    Application.DoEvents();
                    //newDownload(l);
                }
                copyPasteDown = true;
            }
            else if(e.KeyCode == Keys.F11)
            {
                if(this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                }
            }
        }

        private void SingleDownload_Exited(object sender, EventArgs e)
        {
            // DO NOTHING ?
        }

        private void SingleDownload_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }
           
            string dataString = e.Data.ToString();
            if (dataString.Length > 0)
            {
                if(dataString.Substring(0, 1).Equals("{"))
                {
                    JObject jsonO = JObject.Parse(dataString);
                    string url = "https://www.youtube.com/watch?v=";
                    url += jsonO.GetValue("id");
                    this.Invoke((MethodInvoker)delegate
                    {
                        downloadsPreQueue.Enqueue(url);
                    });
                }
            }
        }

        private void newDownload(string url)
        {
            DownloadContainer newDc = new DownloadContainer();
            if (DownloadContainer.getDownloadsAccess().Count == 1)
            {
                dcMain = newDc;
                dcMain.Parent = ccDownloadsContainer;
                resizeDcMain();
            }
            newDc.download(url, Shared.preferences.downloadsDirectory, Shared.preferences.canConvert);
        }

        private void Paste_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V)
            {
                copyPasteDown = false;
            }
        }
    }
}
