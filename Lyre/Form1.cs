//42 ; 
// by Robert Barachini

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Reflection;
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
        private Panel ccContainer;
        private Panel ccTopBar;
        private Panel ccDownloadsContainer;
        private DownloadContainer dcMain;
        private Panel ccFormMinimize;
        private Panel ccFormMaximize;
        private Panel ccFormClose;
        private Panel ccDownloadsDirectory;
        private Label ccHint;
        private Panel ccSettings;
        private RichTextBox ccResourceDownloaderLog;

        // Status Bar
        private Panel ccStatusBar;
        private Label ccDownloadsText;
        private Label ccDownloadsValue;
        private Label ccActiveDownloadsText;
        private Label ccActiveDownloadsValue;


        private object resourcesMissingCountLock = new object();
        private int resourcesMissingCount;
        private Queue<string> downloadsPreQueue = new Queue<string>();
        private System.Windows.Forms.Timer timerStatusUpdater;
        private System.Windows.Forms.Timer timerPreQueueHandler;

        private void Form1_Load(object sender, EventArgs e)
        {
            Form1_Load_Call();
        }

        private void Form1_Load_Call()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            resourcesMissingCount = getResourcesMissingCount();
            try // if a dll is missing
            {
                loadSources();
            }
            catch(Exception ex)
            {
                //preferences = new Preferences();
            }
            InitComponents();
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

            timerStatusUpdater = new System.Windows.Forms.Timer();
            timerStatusUpdater.Interval = 1000;
            timerStatusUpdater.Tick += TimerStatusUpdater_Tick;
            timerStatusUpdater.Start();

            timerPreQueueHandler = new System.Windows.Forms.Timer();
            timerPreQueueHandler.Interval = 1000;
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
            int downloadsInQueue = DownloadContainer.getDownloadsQueueCount();
            int downloadsInPreQueue = downloadsPreQueue.Count;

            int downloadsUnfinished = downloadsActive + downloadsInQueue + downloadsInPreQueue;
            int maximumControls = Shared.preferences.maxDownloadContainerControls;
            int maximumActive = Shared.preferences.maxActiveProcesses;

            ccDownloadsValue.Text = downloadsUnfinished.ToString() + " / " + maximumControls.ToString();
            ccActiveDownloadsValue.Text = downloadsActive.ToString() + " / " + maximumActive.ToString();
        }

        private int getResourcesMissingCount()
        {
            int count = 0;
            foreach(OnlineResource onR in OnlineResource.resourcesList)
            {
                if(File.Exists(onR.path) == false)
                {
                    count++;
                }
            }
            return count;
        }

        private void getResources()
        {
            foreach(OnlineResource onR in OnlineResource.resourcesList)
            {
                if(File.Exists(onR.path) == false)
                {
                    ccResourceDownloaderLog.AppendText("Downloading missing resource" + Environment.NewLine);
                    ccResourceDownloaderLog.AppendText("Credit: " + onR.credit + Environment.NewLine);
                    ccResourceDownloaderLog.AppendText("URL: " + onR.url + Environment.NewLine + Environment.NewLine);
                    // fetch the missing file from the web
                    DownloaderAsync newDA = new DownloaderAsync(onR.path);
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
                resourcesMissingCount--;
                string path = "";
                string senderPath = sender.outputPath;
                if (senderPath.Contains("\\"))
                {
                    path = senderPath.Substring(0, senderPath.LastIndexOf("\\"));
                }
                if (path.Length > 0)
                {
                    Directory.CreateDirectory(path);
                }
                File.Create(sender.outputPath).Close();
                File.WriteAllBytes(sender.outputPath, sender.data);
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
            //preferences = new Preferences();
            loadJSON(Shared.filePreferences, ref Shared.preferences);
            loadJSON(Shared.filenameHistory, ref Shared.history);
            
        }

        private void loadDlQueue()
        {
            LinkedList<string> urls = new LinkedList<string>();
            loadJSON(Shared.filenameDlQueue, ref urls);
            if (resourcesMissingCount == 0) // first download resources and then populate and resume downloads
            {
                foreach (string s in urls)
                {
                    //newDownload(s);
                    downloadsPreQueue.Enqueue(s);
                }
            }
        }

        private void saveSources()
        {
            saveJSON(Shared.filePreferences, Shared.preferences);
            saveJSON(Shared.filenameHistory, Shared.history);

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
                saveJSON(Shared.filenameDlQueue, urls);
            }
        }

        private void InitComponents()
        {
            this.Text = "Lyre - A music app by Robert Barachini";
            this.FormClosing += Form1_FormClosing;
            this.DoubleBuffered = true;
            this.Width = Shared.preferences.formWidth;
            this.Height = Shared.preferences.formHeight;
            this.Top = Shared.preferences.formTop;
            this.Height = Shared.preferences.formHeight;
            this.SizeChanged += Form1_SizeChanged;
            this.KeyDown += Paste_KeyDown;
            this.KeyUp += Paste_KeyUp;
            this.BackColor = Shared.preferences.colorForeground;
            //this.FormBorderStyle = FormBorderStyle.None;
            this.MouseMove += Form1_MouseMove;
            this.MouseDown += Form1_MouseDown;
            //this.BackColor = Color.Lime;

            ccContainer = new Panel();
            ccContainer.Parent = this;
            this.Controls.Add(ccContainer);
            ccContainer.BackColor = Shared.preferences.colorForeground;
            ccContainer.Dock = DockStyle.Fill;

            ccTopBar = new Panel();
            ccTopBar.Parent = ccContainer;
            ccContainer.Controls.Add(ccTopBar);
            ccTopBar.BackColor = Shared.preferences.colorBackground;
            ccTopBar.MouseDown += Form1_MouseDown;

            ccDownloadsContainer = new Panel();
            ccDownloadsContainer.Parent = ccContainer;
            ccContainer.Controls.Add(ccDownloadsContainer);
            ccDownloadsContainer.BackColor = Shared.preferences.colorForeground;
            ccDownloadsContainer.AutoScroll = true;
            ccDownloadsContainer.KeyDown += Paste_KeyDown;
            ccDownloadsContainer.KeyUp += Paste_KeyUp;

            ccFormMinimize = new Panel();
            ccFormMinimize.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormMinimize);
            ccFormMinimize.Cursor = Cursors.Hand;
            ccFormMinimize.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormMinimize.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_Minimize));
            ccFormMinimize.BackColor = ccTopBar.BackColor;
            ccFormMinimize.Click += CcFormMinimize_Click;
            ccFormMinimize.Visible = false;

            ccFormMaximize = new Panel();
            ccFormMaximize.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormMaximize);
            ccFormMaximize.Cursor = Cursors.Hand;
            ccFormMaximize.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormMaximize.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_Maximize));
            ccFormMaximize.BackColor = ccTopBar.BackColor;
            ccFormMaximize.Click += CcFormMaximize_Click;
            ccFormMaximize.Visible = false;

            ccFormClose = new Panel();
            ccFormClose.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormClose);
            ccFormClose.Cursor = Cursors.Hand;
            ccFormClose.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormClose.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_CloseBig));
            ccFormClose.BackColor = ccTopBar.BackColor;
            ccFormClose.Click += CcFormClose_Click;
            ccFormClose.Visible = false;

            ccDownloadsDirectory = new Panel();
            ccDownloadsDirectory.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccDownloadsDirectory);
            ccDownloadsDirectory.Cursor = Cursors.Hand;
            ccDownloadsDirectory.BackgroundImageLayout = ImageLayout.Zoom;
            ccDownloadsDirectory.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_IMG_Directory));
            ccDownloadsDirectory.BackColor = ccTopBar.BackColor;
            ccDownloadsDirectory.Click += CcDownloadsDirectory_Click;

            ccHint = new Label();
            ccHint.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccHint);
            ccHint.Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel);
            ccHint.Text = "ALPHA preview : Paste Youtube links anywhere, really ...";
            ccHint.ForeColor = Shared.preferences.colorFontDefault;
            ccHint.BackColor = Shared.preferences.colorBackground;

            ccSettings = new Panel();
            ccSettings.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccSettings);
            ccSettings.Cursor = Cursors.Hand;
            ccSettings.BackgroundImageLayout = ImageLayout.Zoom;
            ccSettings.BackgroundImage = getImage(Path.Combine(Shared.resourcesDirectory, Shared.FormControls_IMG_Settings));
            ccSettings.BackColor = ccTopBar.BackColor;
            ccSettings.Click += CcSettings_Click;
            ccSettings.Visible = false;

            // Status Bar
            ccStatusBar = new Panel();
            ccStatusBar.Parent = ccContainer;
            ccContainer.Controls.Add(ccStatusBar);
            ccStatusBar.BackColor = Shared.preferences.colorBackground;
            ccStatusBar.BringToFront();

            ccDownloadsText = new Label();
            ccDownloadsText.Parent = ccStatusBar;
            ccStatusBar.Controls.Add(ccDownloadsText);
            ccDownloadsText.BackColor = ccStatusBar.BackColor;
            ccDownloadsText.ForeColor = Color.White;
            ccDownloadsText.Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel);
            ccDownloadsText.Text = "Downloads:";
            ccDownloadsText.TextAlign = ContentAlignment.MiddleLeft;
            ccDownloadsText.AutoSize = true;

            ccDownloadsValue = new Label();
            ccDownloadsValue.Parent = ccStatusBar;
            ccStatusBar.Controls.Add(ccDownloadsValue);
            ccDownloadsValue.BackColor = ccStatusBar.BackColor;
            ccDownloadsValue.ForeColor = Shared.preferences.colorAccent2;
            ccDownloadsValue.Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel);
            ccDownloadsValue.Text = "0 / 0";
            ccDownloadsValue.TextAlign = ContentAlignment.MiddleLeft;
            ccDownloadsValue.AutoSize = true;

            ccActiveDownloadsText = new Label();
            ccActiveDownloadsText.Parent = ccStatusBar;
            ccStatusBar.Controls.Add(ccActiveDownloadsText);
            ccActiveDownloadsText.BackColor = ccStatusBar.BackColor;
            ccActiveDownloadsText.ForeColor = Color.White;
            ccActiveDownloadsText.Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel);
            ccActiveDownloadsText.Text = "Active downloads:";
            ccActiveDownloadsText.TextAlign = ContentAlignment.MiddleLeft;
            ccActiveDownloadsText.AutoSize = true;

            ccActiveDownloadsValue = new Label();
            ccActiveDownloadsValue.Parent = ccStatusBar;
            ccStatusBar.Controls.Add(ccActiveDownloadsValue);
            ccActiveDownloadsValue.BackColor = ccStatusBar.BackColor;
            ccActiveDownloadsValue.ForeColor = Shared.preferences.colorAccent2;
            ccActiveDownloadsValue.Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel);
            ccActiveDownloadsValue.Text = "0 / 0";
            ccActiveDownloadsValue.TextAlign = ContentAlignment.MiddleLeft;
            ccActiveDownloadsValue.AutoSize = true;

            // Resource Downloader (debug oriented)
            ccResourceDownloaderLog = new RichTextBox();
            ccResourceDownloaderLog.Parent = ccDownloadsContainer;
            ccDownloadsContainer.Controls.Add(ccResourceDownloaderLog);
            ccResourceDownloaderLog.BorderStyle = BorderStyle.None;
            ccResourceDownloaderLog.Dock = DockStyle.Fill;
            ccResourceDownloaderLog.BackColor = Shared.preferences.colorBackground;
            ccResourceDownloaderLog.ForeColor = Shared.preferences.colorFontDefault;
            ccResourceDownloaderLog.Font = new Font(Shared.preferences.fontDefault.FontFamily, 16, GraphicsUnit.Pixel);
            ccResourceDownloaderLog.ReadOnly = true;
            ccResourceDownloaderLog.KeyDown += CcResourceDownloaderLog_KeyDown;
            ccResourceDownloaderLog.LinkClicked += CcResourceDownloaderLog_LinkClicked;
            if(resourcesMissingCount == 0)
            {
                ccResourceDownloaderLog.Visible = false;
            }
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
            // Implement settings
        }

        private void CcDownloadsDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Choose future downloads destination directory.";
                if(Shared.preferences.downloadsDirectory.Equals("downloads"))
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

            ccDownloadsDirectory.Top = barMargin;
            ccDownloadsDirectory.Left = barMargin;
            ccDownloadsDirectory.Height = ccFormClose.Height;
            ccDownloadsDirectory.Width = ccFormClose.Width;

            ccHint.Top = barMargin;
            ccHint.Left = ccDownloadsDirectory.Left + ccDownloadsDirectory.Width + barMargin;
            ccHint.Width = 500;
            ccHint.Height = ccFormClose.Height;

            ccSettings.Top = barMargin;
            ccSettings.Left = ccHint.Left + ccHint.Width + barMargin;
            ccSettings.Width = ccDownloadsDirectory.Width;
            ccSettings.Height = ccDownloadsDirectory.Height;

            // status bar
            ccStatusBar.Top = ccSettings.Top + ccSettings.Height + 50;
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


            // download containers
            if (DownloadContainer.getDownloadsAccess().Count > 0)
            {
                resizeDcMain();
            }

            this.ResumeLayout();
        }

        private void resizeDcMain()
        {
            try
            {
                dcMain = DownloadContainer.getDownloadsAccess().First.Value;
                dcMain.Top = ccStatusBar.Top + ccStatusBar.Height - ccDownloadsContainer.Top + 30; //30;
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
            this.Close();
        }

        private void CcFormMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void CcFormMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (resourcesMissingCount != -1)
            {
                saveSources();
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
                    int stop = 0;
                    //newDownload(url);
                    this.Invoke((MethodInvoker)delegate
                    {
                        //newDownload(url);
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
            newDc.download(url, Shared.preferences.downloadsDirectory, true);
        }

        private void Paste_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V)
            {
                copyPasteDown = false;
            }
        }

        private void loadJSON<T>(string path, ref T obj)
        {
            if (File.Exists(path))
            {
                try
                {
                    string fileString = File.ReadAllText(path);
                    obj = JsonConvert.DeserializeObject<T>(fileString);
                }
                catch (Exception ex)
                {
                    saveJSON(path, obj);
                }
            }
            else
            {
                saveJSON(path, obj);
            }
        }

        public void saveJSON<T>(string path, T obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(path, jsonString);
        }
    }
}
