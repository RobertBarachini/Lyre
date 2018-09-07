//42 ; 
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
        private Label ccAudioVideoQuality;
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
        private Label ccExpandInstructions;

        private object resourcesMissingCountLock = new object();
        private int resourcesMissingCount;
        private Queue<DownloadContext> downloadsPreQueue = new Queue<DownloadContext>();
        private System.Windows.Forms.Timer timerStatusUpdater;
        private System.Windows.Forms.Timer timerPreQueueHandler;
        private bool noPreferencesFound;
        private bool instructionsExpanded = false;

        private void Form1_Load(object sender, EventArgs e)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = null;

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

            noPreferencesFound = ! File.Exists(Shared.filePreferences);

            resourcesMissingCount = SharedFunctions.getResourcesMissingCount(OnlineResource.resourcesListDownloader);

            if (resourcesMissingCount > 0)
            {
                //getResources();
                try // if a dll is missing
                {
                    loadSources();
                }
                catch (Exception ex)
                {
                    //preferences = new Preferences();
                }

                InitComponentsMissing();
            }
            else
            {
                try // if a dll is missing
                {
                    loadSources();
                }
                catch (Exception ex)
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

                loadDlQueue();
                ResizeComponents();
                this.Show();

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
            }

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
                DownloadContext newVideoContext = downloadsPreQueue.Dequeue();
                newDownload(newVideoContext);
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

            if(downloadsUnfinished > 0)
            {
                ccPanelInstructions.Visible = false;
            }

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
                    ccResourceDownloaderLog.SelectionColor = Color.Lime;
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
            string fileContents = "";
            if(File.Exists(Shared.filenameDlQueue))
            {
                fileContents = File.ReadAllText(Shared.filenameDlQueue);
            }
            else
            {
                LinkedList<DownloadContext> dNew = new LinkedList<DownloadContext>();
                SharedFunctions.saveJSON(Shared.filenameDlQueue, dNew);
                return;
            }

            {
                LinkedList<DownloadContext> urls = new LinkedList<DownloadContext>();
                if (SharedFunctions.loadJSON(ref urls, fileContents))
                {
                    if (resourcesMissingCount == 0) // first download resources and then populate and resume downloads
                    {
                        foreach (object s in urls)
                        {
                            try
                            {
                                downloadsPreQueue.Enqueue((DownloadContext)s);
                                continue;
                            }
                            catch (Exception ex) { }
                        }
                    }
                    return;
                }
            }

            {   // Backward compatibility
                LinkedList<string> urls = new LinkedList<string>();
                if (SharedFunctions.loadJSON(ref urls, fileContents))
                {
                    if (resourcesMissingCount == 0) // first download resources and then populate and resume downloads
                    {
                        foreach (object s in urls)
                        {
                            try
                            {
                                downloadsPreQueue.Enqueue(new DownloadContext((string)s, Shared.preferences.canConvert, Shared.preferences.maxAudioQualitySelector, Shared.preferences.maxVideoQualitySelector, Shared.preferences.maxVideoFrameRateSelector));
                                continue;
                            }
                            catch (Exception ex) { }
                        }
                    }
                    return;
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
                LinkedList<DownloadContext> downloadsToSave = new LinkedList<DownloadContext>();
                foreach (DownloadContainer dc in DownloadContainer.getDownloadsAccess())
                {
                    if (dc.isFinished() == false)
                    {
                        downloadsToSave.AddLast(new DownloadContext(dc.getURL(), Shared.preferences.canConvert, Shared.preferences.maxAudioQualitySelector, Shared.preferences.maxVideoQualitySelector, Shared.preferences.maxVideoFrameRateSelector));
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
                foreach (DownloadContext s in downloadsPreQueue)
                {
                    downloadsToSave.AddLast(s);
                }
                SharedFunctions.saveJSON(Shared.filenameDlQueue, downloadsToSave);
            }
        }

        private void InitComponents()
        {
            InitBasicComponents();

            FormClosing += Form1_FormClosing;
            SizeChanged += Form1_SizeChanged;
            KeyDown += Paste_KeyDown;
            KeyUp += Paste_KeyUp;
            MouseMove += Form1_MouseMove;
            MouseDown += Form1_MouseDown;

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
            ccDownloadsContainer.SizeChanged += CcDownloadsContainer_SizeChanged;
            ccContainer.Controls.Add(ccDownloadsContainer);

            ccSettingsContainer = new CcPanel()
            {
                Parent = ccContainer,
                BackColor = Shared.preferences.colorForeground,
                AutoScroll = true
            };
            ccSettingsContainer.SizeChanged += CcSettingsContainer_SizeChanged;
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

            ccTextInstructions = new RichTextBox()
            {
                Parent = ccPanelInstructions,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel),
                Text = Shared.instructionsBasic
                //ScrollBars = RichTextBoxScrollBars.None
            };
            ccTextInstructions.LinkClicked += CcTextInstructions_LinkClicked;
            ccPanelInstructions.Controls.Add(ccTextInstructions);

            ccExpandInstructions = new Label()
            {
                Text = "More",
                ForeColor = Shared.preferences.colorAccent2,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            ccExpandInstructions.Paint += CcExpandInstructions_Paint;
            ccExpandInstructions.Click += CcExpandInstructions_Click;
            ccPanelInstructions.Controls.Add(ccExpandInstructions);

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
            ccCanConvert.DoubleClick += CcCanConvert_DoubleClick;
            ccStatusBar.Controls.Add(ccCanConvert);

            ccAudioVideoQuality = new Label()
            {
                Parent = ccTopBar,
                BackColor = ccTopBar.BackColor,
                ForeColor = Shared.preferences.colorAccent2, // colorAccent4/7
                Cursor = Cursors.Hand,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel),
                AutoSize = true
            };
            ccAudioVideoQuality.MouseClick += CcVideoQuality_MouseClick;
            ccAudioVideoQuality.MouseDoubleClick += CcVideoQuality_MouseDoubleClick;
            ccTopBar.Controls.Add(ccAudioVideoQuality);
            updateCcAudioVideoQualityText();

            ccHistoryButton = new Label()
            {
                Parent = ccTopBar,
                BackColor = ccTopBar.BackColor,
                ForeColor = Shared.preferences.colorFontDefault,
                Cursor = Cursors.Hand,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 24, GraphicsUnit.Pixel),
                AutoSize = true,
                Text = "📥"//"💢"//"🎲"//"🔰"//"Hi"
            };
            ccHistoryButton.Click += CcHistoryButton_Click;
            ccTopBar.Controls.Add(ccHistoryButton);

            // Show the desired container - downloadsContainer is default
            turnOnContainerInvisibility();
            ccDownloadsContainer.Visible = true;
        }

        private void CcExpandInstructions_Click(object sender, EventArgs e)
        {
            instructionsExpanded = ! instructionsExpanded;
            if(instructionsExpanded)
            {
                ccExpandInstructions.Text = "Less";
                ccTextInstructions.Text = Shared.instructions;
            }
            else
            {
                ccExpandInstructions.Text = "More";
                ccTextInstructions.Text = Shared.instructionsBasic;
            }
            // ResizeComponents() doesn't work here
            FormWindowState fs = WindowState;
            WindowState = FormWindowState.Normal;
            Height = Height + 1;
            Height = Height - 1;
            WindowState = fs;
        }

        private void CcExpandInstructions_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            Pen b1 = new Pen(Color.FromArgb(155, Shared.preferences.colorAccent2), 2);
            e.Graphics.DrawRectangle(b1, 0, 0, ccExpandInstructions.Width - 1, ccExpandInstructions.Height - 1);

            b1.Dispose();
        }

        private void InitComponentsMissing()
        {
            InitBasicComponents();

            // Resource Downloader (debug oriented)
            ccResourceDownloaderLog = new RichTextBox()
            {
                Parent = ccContainer,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = new Font(Shared.preferences.fontDefault.FontFamily, 18, GraphicsUnit.Pixel),
                ReadOnly = true
            };
            ccResourceDownloaderLog.KeyDown += CcResourceDownloaderLog_KeyDown;
            ccResourceDownloaderLog.LinkClicked += CcResourceDownloaderLog_LinkClicked;
            ccContainer.Controls.Add(ccResourceDownloaderLog);

            string singular = "components";
            if(resourcesMissingCount == 1)
            {
                singular = "component";
            }

            ccResourceDownloaderLog.Text = "The program will download and install ";
            ccResourceDownloaderLog.SelectionColor = Color.OrangeRed;
            ccResourceDownloaderLog.AppendText(resourcesMissingCount.ToString());
            ccResourceDownloaderLog.SelectionColor = Color.White;
            ccResourceDownloaderLog.AppendText(" missing " + singular + "."
                + Environment.NewLine + "Once the process is complete press '");
            ccResourceDownloaderLog.SelectionColor = Shared.preferences.colorAccent2;
            ccResourceDownloaderLog.AppendText("esc");
            ccResourceDownloaderLog.SelectionColor = Color.White;
            ccResourceDownloaderLog.AppendText("' button to restart the program." + Environment.NewLine + Environment.NewLine
                + "To start the download press the '");
            ccResourceDownloaderLog.SelectionColor = Shared.preferences.colorAccent2;
            ccResourceDownloaderLog.AppendText("Enter");
            ccResourceDownloaderLog.SelectionColor = Color.White;
            ccResourceDownloaderLog.AppendText("' key ..." + Environment.NewLine + Environment.NewLine);
        }

        private void InitBasicComponents()
        {

            Text = "Lyre - A music app by Robert Barachini";
            DoubleBuffered = true;
            Width = Shared.preferences.formWidth;
            Height = Shared.preferences.formHeight;
            Top = Shared.preferences.formTop;
            Height = Shared.preferences.formHeight;
            BackColor = Color.Lime;//Shared.preferences.colorForeground;
            //FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;

            ccContainer = new CcPanel()
            {
                Parent = this,
                BackColor = Shared.preferences.colorForeground,
                Dock = DockStyle.Fill
            };
            Controls.Add(ccContainer);
        }

        private void CcDownloadsContainer_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                // This solves AutoScroll issue
                CcPanel whichControl = ccDownloadsContainer;
                Point scrollAuto = whichControl.AutoScrollPosition;
                whichControl.AutoScrollPosition = new Point(0, 0);
                whichControl.SuspendLayout();

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


                ccPanelInstructions.Left = ccStatusBar.Left;
                ccPanelInstructions.Width = ccStatusBar.Width;
                ccPanelInstructions.Top = ccStatusBar.Top + ccStatusBar.Height + 30;

                ccExpandInstructions.Width = 90;
                ccExpandInstructions.Height = 40;

                ccTextInstructions.Top = 30 + 3;

                if (instructionsExpanded)
                {
                    ccTextInstructions.Top = 30;
                    ccPanelInstructions.Height = 1080;
                    ccExpandInstructions.Left = (ccPanelInstructions.Width / 2) - (ccExpandInstructions.Width / 2);
                    ccExpandInstructions.Top = ccPanelInstructions.Height - ccExpandInstructions.Height - 30;
                    ccTextInstructions.Width = ccStatusBar.Width;
                    ccTextInstructions.Width = ccTextInstructions.Parent.Width - (2 * ccTextInstructions.Left);
                    ccTextInstructions.Height = ccTextInstructions.Parent.Height - (3 * 30) - ccExpandInstructions.Height;
                }
                else
                {
                    ccPanelInstructions.Height = 90;
                    ccExpandInstructions.Top = (ccPanelInstructions.Height / 2) - (ccExpandInstructions.Height / 2);
                    ccExpandInstructions.Left = ccPanelInstructions.Width - ccExpandInstructions.Width - 30;
                    ccTextInstructions.Width = ccTextInstructions.Parent.Width - (2 * ccTextInstructions.Left) - ccExpandInstructions.Width - 30;
                    ccTextInstructions.Height = ccTextInstructions.Parent.Height - (2 * ccTextInstructions.Top - 3);
                }

                ccTextInstructions.Left = 30;
                

                // download containers
                if (DownloadContainer.getDownloadsAccess().Count > 0)
                {
                    resizeDcMain();
                }

                whichControl.ResumeLayout();
                whichControl.AutoScrollPosition = new Point(Math.Abs(scrollAuto.X), Math.Abs(scrollAuto.Y));
            }
            catch(Exception ex) { }
        }

        private void CcSettingsContainer_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                // This solves AutoScroll issue
                CcPanel whichControl = ccSettingsContainer;
                Point scrollAuto = whichControl.AutoScrollPosition;
                whichControl.AutoScrollPosition = new Point(0, 0);
                whichControl.SuspendLayout();

                ccSettingsPanel.Top = 50;
                //ccSettingsPanel.MaximumSize = new Size(800, ccSettingsPanel.Height); // resizing issues...
                ccSettingsPanel.Width = 800; //Math.Min(800, ccSettingsContainer.Width - 30); //800;
                ccSettingsPanel.Left = Math.Max(0, (ccSettingsContainer.Width - ccSettingsPanel.Width) / 2);
                //this.Text = ccSettingsContainer.Width + "/" + ccSettingsPanel.Width;

                ccSettingsBottomMargin.Top = ccSettingsPanel.Top + ccSettingsPanel.Height + 50;
                ccSettingsBottomMargin.Height = 1;

                whichControl.ResumeLayout();
                whichControl.AutoScrollPosition = new Point(Math.Abs(scrollAuto.X), Math.Abs(scrollAuto.Y));
            }
            catch(Exception ex) { }
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
            updateCcAudioVideoQualityText();
        }

        private void CcCanConvert_DoubleClick(object sender, EventArgs e)
        {
            Shared.preferences.canConvert = ccCanConvert.isON;
            updateCcAudioVideoQualityText();
        }

        private void CcVideoQuality_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            changeAudioVideoQuality(e);
        }

        private void CcVideoQuality_MouseClick(object sender, MouseEventArgs e)
        {
            changeAudioVideoQuality(e);
        }

        private void changeAudioVideoQuality(MouseEventArgs e)
        {
            if (Shared.preferences.canConvert)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Shared.increaseAudioQuality();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    Shared.decreaseAudioQuality();
                }
            }
            else
            {
                if (e.Button == MouseButtons.Left)
                {
                    Shared.increaseVideoQuality();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    Shared.decreaseVideoQuality();
                }
            }

            updateCcAudioVideoQualityText();
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

        private void updateCcAudioVideoQualityText()
        {
            if (Shared.preferences.canConvert)
            {
                ccAudioVideoQuality.Text = Shared.getAudioQualityString(Shared.preferences.maxAudioQualitySelector);
            }
            else
            {
                ccAudioVideoQuality.Text = Shared.getVideoQualityString(Shared.preferences.maxVideoQualitySelector) + " " + Shared.getVideoFrameRatePure(Shared.preferences.maxVideoFrameRateSelector);
            }
        }

        private void CcResourceDownloaderLog_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private bool obtainingMissingResources = false;
        private void CcResourceDownloaderLog_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

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
            else if(e.KeyCode == Keys.Enter)
            {
                if(obtainingMissingResources == false)
                {
                    obtainingMissingResources = true;
                    getResources();
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

            ccAudioVideoQuality.Top = barMargin;
            ccAudioVideoQuality.Left = ccSettingsButton.Left + ccSettingsButton.Width + barMargin;
            ccAudioVideoQuality.Height = ccFormClose.Height;

            ccHistoryButton.Top = barMargin - 3;
            ccHistoryButton.Left = ccTopBar.Width - ccHistoryButton.Width - barMargin + 3;
            ccHistoryButton.Height = ccFormClose.Height;

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

            if (Shared.updatePressed)
            {
                try
                {
                    string updaterLoc = OnlineResource.LyreUpdaterLocation;
                    string downloaderLoc = OnlineResource.LyreDownloaderLocation;

                    // Problems with paths / descriptors?
                    Directory.SetCurrentDirectory(OnlineResource.LyreUpdaterLocation);

                    Process p = new Process();
                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.FileName = Path.Combine(OnlineResource.LyreDownloaderLocation, "updater\\LyreUpdater.exe");
                    p.StartInfo = ps;
                    p.Start();

                    // Problems with paths / descriptors?
                    //Directory.SetCurrentDirectory(downloaderLoc);
                    //Shared.mainForm.Close();
                    Application.Exit();
                }
                catch (Exception ex) { }
            }
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
            if(e.Control && e.KeyCode == Keys.V && copyPasteDown == false)
            {
                ccPanelInstructions.Visible = false;
                detectLinks();
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

        private void detectLinks()
        {
            // queue download url-s
            string clipboardString = Clipboard.GetText().Replace("http://", "https://");
            LinkedList<string> hits = new LinkedList<string>();
            string pattern = "https://www.youtube.com/watch?v=";
            // youtube video_IDs are currently 11 chars long
            int idLen = 11;
            string[] patterns = new string[]
            {
                    "https://www.youtube.com/watch?v=",
                    "https://www.youtube.com/embed/",
                    "https://youtu.be/",
                    "https://www.youtube.com/playlist?list="
            };
            int counter = 0;
            int cLen = -1;
            while (true)
            {
                cLen = clipboardString.Length;
                try
                {
                    int index = -1; //clipboardString.IndexOf(pattern);
                    for (int i = 0; i < patterns.Length; i++)
                    {
                        int index2 = clipboardString.IndexOf(patterns[i]);
                        if (index2 != -1 && (index2 < index || index == -1))
                        {
                            index = index2;
                            pattern = patterns[i];
                        }
                    }

                    if (index == -1)
                    {
                        break;
                    }

                    if (pattern.Equals(patterns[3]) == false)
                    {
                        if (clipboardString.Length < pattern.Length + idLen)
                        {
                            break;
                        }

                        string id = clipboardString.Substring(index + pattern.Length, idLen);

                        if (clipboardString.Contains("&list="))
                        {
                            expandPlaylist(clipboardString, false);
                        }

                        if (SharedFunctions.isLegitID(id) == false)
                        {
                            break;
                        }

                        // fix link recognition for lists 

                        string hit = patterns[0] + id;
                        clipboardString = clipboardString.Substring(index + pattern.Length + idLen);

                        hits.AddLast(hit);
                        counter++;
                    }
                    else
                    {
                        expandPlaylist(clipboardString, true);
                    }
                }
                catch (Exception ex)
                {
                    clipboardString = clipboardString.Substring(1);
                }
                if(cLen == clipboardString.Length)
                {
                    break;
                }
            }
            foreach (string l in hits)
            {
                downloadsPreQueue.Enqueue(new DownloadContext(l, Shared.preferences.canConvert, Shared.preferences.maxAudioQualitySelector, Shared.preferences.maxVideoQualitySelector, Shared.preferences.maxVideoFrameRateSelector));
                Application.DoEvents();
            }
            copyPasteDown = true;
        }

        private void expandPlaylist(string clipboardString, bool onlyPlaylist)
        {
            try
            {
                // 34 current list_ID length || 18 is possible too???
                //hit += clipboardString.Substring(0, "&list=".Length + 34);
                //"https://www.youtube.com/watch?v=",
                //"https://www.youtube.com/embed/",
                //"https://youtu.be/",
                //"https://www.youtube.com/playlist?list=" onlyPlaylist

                string listHit = "";

                if(onlyPlaylist)
                {
                    listHit = clipboardString.Substring("https://www.youtube.com/playlist?list=".Length);
                    listHit = listHit.Substring(0, Math.Min(listHit.Length, 34));
                }
                else
                {
                    listHit = clipboardString.Substring(clipboardString.IndexOf("&list=") + "&list=".Length);
                    listHit = listHit.Substring(0, Math.Min(listHit.Length, 34));
                }

                if (listHit.Contains("&")) // in case the playlist is 18 chars long and url contains &index= / &t= / ...
                {
                    int index = listHit.IndexOf("&");
                    listHit = listHit.Substring(0, index);
                }

                // youtube-dl --flat-playlist -j "https://www.youtube.com/playlist?list=PLSdoVPM5Wnne47ib65gVG206M7qp43us-"
                // start youtube-dl process - get individual video IDs on process exit
                Process singleDownload = new Process();
                string arguments = "--flat-playlist -j \"" + "https://www.youtube.com/playlist?list=" + listHit + "\"";
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
            catch(Exception ex)
            {

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
                        downloadsPreQueue.Enqueue(new DownloadContext(url, Shared.preferences.canConvert, Shared.preferences.maxAudioQualitySelector, Shared.preferences.maxVideoQualitySelector, Shared.preferences.maxVideoFrameRateSelector));
                    });
                }
            }
        }

        private void newDownload(DownloadContext downloadContext)
        {
            DownloadContainer newDc = new DownloadContainer();
            if (DownloadContainer.getDownloadsAccess().Count == 1)
            {
                dcMain = newDc;
                dcMain.Parent = ccDownloadsContainer;
                resizeDcMain();
            }
            newDc.download(downloadContext, Shared.preferences.downloadsDirectory);
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
