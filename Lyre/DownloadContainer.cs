using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TagLib;

//
//// TODO
//
// - check if the download and encoding has been a success - check at event "Process.Exit" = mp3/video exists

// kako bi naredil da ko se klice download ne caka na svoj vrstni red s queue..
// static timer ki gleda kateri je prvi DownloadContainer v downloadsQueue in šele nato pokliče download()

class DownloadContainer : Panel
{
    private static object downloadsLock = new object();
    private static LinkedList<DownloadContainer> downloads = new LinkedList<DownloadContainer>();
    private static object downloadsQueueLock = new object();
    private static LinkedList<DownloadContainer> downloadsQueue = new LinkedList<DownloadContainer>();
    private static object activeProcessesLock = new object();
    private static int activeProcesses = 0;
    private static int maxActiveProcesses = 3; // 3
    private LinkedListNode<DownloadContainer> downloadNode;
    private static System.Windows.Forms.Timer downloadsHandler = new System.Windows.Forms.Timer();

    public string getURL()
    {
        return url.ToString();
    }

    public int getActiveProcessesAccess()
    {
        lock(activeProcessesLock)
        {
            return activeProcesses;
        }
    }

    public static LinkedList<DownloadContainer> getDownloadsAccess()
    {
        lock(downloadsLock)
        {
            return downloads;
        }
    }

    public bool isFinished()
    {
        return finished;
    }

    private Uri url;
    private PictureBox thumbnail;
    private Panel progressBar;
    private Label progressLabel;
    private Label title;
    private Panel cancelButton;
    private double progress;
    private  StringBuilder processOutput;
    private Process singleDownload;
    private JObject infoJSON;
    private string videoID;
    private string destinationDirectory;
    private string imageExtension;
    private bool instanceRemoved;
    private bool downloadStarted;
    private bool canConvert;
    private System.Windows.Forms.Timer animateProgress;
    private System.Windows.Forms.Timer animateGeneral;
    private double previousProgress;
    private RichTextBox outputLog;
    private bool finished = false;

    public DownloadContainer()
    {
        downloads.AddLast(this);
        downloadNode = downloads.Last;
        if (downloads.Count > 0)
        {
            this.Parent = downloads.First.Value.Parent;
        }
        initComp();
        this.SizeChanged += DownloadContainer_SizeChanged;

        if (downloadsHandler.Interval == 100)
        {
            downloadsHandler.Interval = 1000;
            downloadsHandler.Tick += DownloadsHandler_Tick;
            downloadsHandler.Start();
        }
    }

    private void DownloadsHandler_Tick(object sender, EventArgs e)
    {
        lock (activeProcessesLock)
        {
            if (activeProcesses < maxActiveProcesses)
            {
                downloadsHandler.Stop();

                lock (downloadsQueueLock)
                {
                    while (downloadsQueue.Count > 0)
                    {
                        DownloadContainer dc = downloadsQueue.First.Value;
                        downloadsQueue.RemoveFirst();

                        if (dc != null && dc.instanceRemoved == false)
                        {
                            new Thread(() =>
                            {
                                Thread.CurrentThread.IsBackground = true;
                                dc.title.Invoke((MethodInvoker)delegate
                                {
                                    dc.title.Text = "Waiting for download ...";
                                });
                                dc.download_p(dc.url.ToString(), dc.destinationDirectory, dc.canConvert);
                            }).Start();

                            activeProcesses++;
                            dc.downloadStarted = true;

                            if (activeProcesses == maxActiveProcesses)
                            {
                                break;
                            }
                        }
                    }
                }

                downloadsHandler.Start();
            }
        }
    }

    private void updateProgress(double newProgress)
    {
        try
        {
            int widthSetup = this.Width - thumbnail.Width - this.Left - 75;
            this.progressBar.Invoke((MethodInvoker)delegate
            {
                progressBar.Width = (int)((widthSetup) * newProgress);
            });
            this.progressLabel.Invoke((MethodInvoker)delegate
            {
                if (newProgress == 1)
                {
                    progressLabel.BackColor = Preferences.colorAccent3;
                    progressLabel.Text = "Processing ...";
                }
                else
                {
                    progressLabel.Text = (newProgress * 100).ToString("0.0") + " %";
                }
            });
        }
        catch (Exception ex)
        {

        }
    }

    private void DownloadContainer_SizeChanged(object sender, EventArgs e)
    {
        try
        {
            resize();
        }
        catch (Exception ex)
        {

        }
    }

    private string getVideoID(string url)
    {
        int index = url.IndexOf("watch?v=");
        if (index < 0)
        {
            return null;
        }
        else
        {
            url = url.Substring(index + "watch?v=".Length);
            return url;
        }
    }

    private void initComp()
    {
        instanceRemoved = false;
        downloadStarted = false;
        processOutput = new StringBuilder();
        progress = 0;

        this.DoubleBuffered = true;
        this.BackColor = Preferences.colorBackground;
        this.Font = Preferences.fontDefault;

        thumbnail = new PictureBox();
        thumbnail.Parent = this;
        this.Controls.Add(thumbnail);
        thumbnail.BackColor = Preferences.colorBackground;
        thumbnail.SizeMode = PictureBoxSizeMode.StretchImage; // Zoom should be used - change thumbnail.Left
        thumbnail.Image = getImageFrame();//getImage("squareAnimation256.gif");
        thumbnail.Cursor = Cursors.Hand;
        thumbnail.Click += Thumbnail_Click;

        title = new Label();
        title.Parent = this;
        this.Controls.Add(title);
        title.Font = Preferences.fontDefault;
        title.Text = "Queued for download ...";
        title.ForeColor = Color.White;
        title.TextAlign = ContentAlignment.TopLeft;
        title.Font = new Font(Preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel);

        progressBar = new Panel();
        progressBar.Parent = this;
        this.Controls.Add(progressBar);
        progressBar.BackColor = Preferences.colorAccent1;

        progressLabel = new Label();
        progressLabel.Parent = progressBar;
        progressBar.Controls.Add(progressLabel);
        progressLabel.Dock = DockStyle.Fill;
        progressLabel.ForeColor = Color.White;
        progressLabel.BackColor = Preferences.colorAccent1;
        progressLabel.TextAlign = ContentAlignment.MiddleCenter;
        progressLabel.Text = "";
        progressLabel.Font = new Font(Preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel);

        outputLog = new RichTextBox();
        outputLog.Parent = this;
        this.Controls.Add(outputLog);
        outputLog.BorderStyle = BorderStyle.None;
        outputLog.BackColor = Preferences.colorForeground;
        outputLog.ForeColor = Color.White;
        outputLog.Font = new Font(Preferences.fontDefault.FontFamily, 14, GraphicsUnit.Pixel);

        cancelButton = new Panel();
        cancelButton.Parent = this;
        this.Controls.Add(cancelButton);
        cancelButton.BackColor = Preferences.colorAccent2;
        cancelButton.Cursor = Cursors.Hand;
        cancelButton.Click += CancelButton_Click;
        //cancelButton.BackgroundImage = getImage("squareAnimation64.gif");

        updateProgress(0);
        resize();

        previousProgress = progress;
        animateProgress = new System.Windows.Forms.Timer();
        animateProgress.Interval = 50;
        animateProgress.Tick += AnimateProgress_Tick;
        animateProgress.Start();

        animateGeneral = new System.Windows.Forms.Timer();
        animateGeneral.Interval = 50;
        animateGeneral.Tick += AnimateGeneral_Tick;
        animateGeneral.Start();
    }

    private void AnimateGeneral_Tick(object sender, EventArgs e)
    {
        animateGeneral.Stop();

        if (imageExtension == null && canAnimateThumbnail)
        {
            double change = 0.07;
            if (growing)
            {
                sizePercent += change;
                if (sizePercent > 2) // 1
                {
                    sizePercent = 1;
                    growing = false;
                }
            }
            else
            {
                sizePercent -= change;
                if (sizePercent < -2) // 0
                {
                    int chosen = r1.Next(0, 4);
                    if (chosen == 0) { c1 = Preferences.colorAccent1; }
                    if (chosen == 1) { c1 = Preferences.colorAccent2; }
                    if (chosen == 2) { c1 = Preferences.colorAccent3; }
                    if (chosen == 3) { c1 = Preferences.colorAccent4; }
                    //c1 = Preferences.colorAccent1;
                    perSide = r1.Next(1, 9);
                    sizePercent = 0;
                    growing = true;
                }
            }
            try
            {
                this.thumbnail.Invoke((MethodInvoker)delegate
                {
                    thumbnail.Image.Dispose();
                    thumbnail.Image = getImageFrame();
                });
            }
            catch (Exception ex)
            {

            }
        }

        animateGeneral.Start();
    }

    private void AnimateProgress_Tick(object sender, EventArgs e)
    {
        double progressIncrement = /*0.01d;*/ Math.Max((Math.Abs(progress - previousProgress) / 10d), 0.001);
        previousProgress += progressIncrement;
        if (previousProgress > progress)
        {
            previousProgress = progress;
        }
        updateProgress(previousProgress);
    }

    public void download(string url, string destinationDirectory, bool canConvert)
    {
        this.videoID = getVideoID(url);
        string path = Path.Combine(Preferences.tempDirectoy, videoID + ".info.json");
        if (System.IO.File.Exists(path) && 1 == 2)
        {
            infoJSON = JObject.Parse(System.IO.File.ReadAllText(path));
            this.title.Invoke((MethodInvoker)delegate
            {
                this.title.Text = infoJSON.GetValue("fulltitle").ToString();
            });
            string fileURL = infoJSON["thumbnails"].First["url"].ToString();
            imageExtension = getExtension(fileURL);
            this.thumbnail.Invoke((MethodInvoker)delegate
            {
                thumbnail.Image = getImage(Path.Combine(Preferences.tempDirectoy, infoJSON.GetValue("display_id").ToString() + imageExtension));
            });
            updateProgress(1);
        }
        else
        {
            try
            {
                this.url = new Uri(url);
            }
            catch (Exception ex)
            {
                return;
            }
            this.canConvert = canConvert;
            this.destinationDirectory = destinationDirectory;

            lock (downloadsQueueLock)
            {
                downloadsQueue.AddLast(this);
            }
        }
    }

    private void download_p(string url, string destinationDirectory, bool canConvert)
    {
        //animateProgress.Start();

        if (Directory.Exists(Preferences.tempDirectoy) == false)
        {
            Directory.CreateDirectory(Preferences.tempDirectoy);
        }
        if (Directory.Exists(destinationDirectory) == false)
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        this.videoID = getVideoID(url);
        string arguments = "";

        singleDownload = new Process();
        {
            singleDownload.StartInfo.FileName = @"youtube-dl.exe";
            if (canConvert)
            {
                arguments = "--extract-audio --audio-format mp3 " + "-o \"" + Path.Combine(Preferences.tempDirectoy, videoID + ".%(ext)s"/*, "%(title)s - %(id)s.%(ext)s"*/) + "\" " + url + " --write-thumbnail --write-info-json --audio-quality 0";
            }
            else
            {
                arguments = "-o \"" + Path.Combine(Preferences.tempDirectoy, videoID + ".%(ext)s"/*, "%(title)s - %(id)s.%(ext)s"*/) + "\" " + url + " --write-thumbnail --write-info-json";
            }
            singleDownload.StartInfo.Arguments = arguments;
            singleDownload.StartInfo.CreateNoWindow = true;
            singleDownload.StartInfo.UseShellExecute = false;
            singleDownload.StartInfo.RedirectStandardOutput = true;
            singleDownload.OutputDataReceived += SingleDownload_OutputDataReceived;
            singleDownload.EnableRaisingEvents = true;
            singleDownload.Exited += SingleDownload_Exited;
            singleDownload.Start();
            singleDownload.BeginOutputReadLine();
        }
    }

    private void SingleDownload_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        processOutput.Append(e.Data.ToString() + Environment.NewLine);

        this.outputLog.Invoke((MethodInvoker)delegate
        {
            outputLog.AppendText(e.Data.ToLower() + Environment.NewLine);
        });

        Application.DoEvents();
        try
        {
            string data = e.Data.ToString();
            if (data.Contains("[download]") && data.Contains("%"))
            {
                data = data.Substring("[download]".Length, 7);
                data = data.Substring(0, data.IndexOf("%")).Replace(" ", "");
                double perc = Convert.ToDouble(data, CultureInfo.InvariantCulture);
                progress = perc / 100d;
                //updateProgress(perc / 100d);
            }
            else if (data.Contains("[info]"))
            {
                string path = Path.Combine(Preferences.tempDirectoy, videoID + ".info.json");
                while (System.IO.File.Exists(path) == false)
                {
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
                infoJSON = JObject.Parse(System.IO.File.ReadAllText(path));
                this.title.Invoke((MethodInvoker)delegate
                {
                    this.title.Text = infoJSON.GetValue("fulltitle").ToString();
                });
            }
            else if (data.Contains("Writing thumbnail to: "))
            {
                string fileURL = infoJSON["thumbnails"].First["url"].ToString();
                imageExtension = getExtension(fileURL);
                this.thumbnail.Invoke((MethodInvoker)delegate
                {
                    thumbnail.Image = getImage(Path.Combine(Preferences.tempDirectoy, infoJSON.GetValue("display_id").ToString() + imageExtension));
                });
            }
            else
            {
                // TO BE IMPLEMENTED
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void SingleDownload_Exited(object sender, EventArgs e)
    {
        finished = true;
        animateProgress.Stop();
        updateProgress(1);

        if (instanceRemoved == false)
        {
            lock (activeProcessesLock)
            {
                activeProcesses--;
            }
            instanceRemoved = true;
        }

        try
        {
            TagLib.File soundFile = TagLib.File.Create(Path.Combine(Preferences.tempDirectoy, videoID + ".mp3"));
            IPicture albumArt = new Picture(Path.Combine(Preferences.tempDirectoy, videoID + imageExtension));
            soundFile.Tag.Pictures = new IPicture[1] { albumArt };
            soundFile.Tag.Comment = videoID;
            soundFile.Save();
        }
        catch (Exception ex) { }

        try
        {
            System.IO.File.Move(Path.Combine(Preferences.tempDirectoy, videoID) + ".mp3", Path.Combine(destinationDirectory, infoJSON.GetValue("fulltitle").ToString() + ".mp3"));
        }
        catch (Exception ex) { }

        HistoryItem hi = new HistoryItem();
        //EXAMPLE : Path.GetFullPath((new Uri(absolute_path)).LocalPath);
        hi.path_output = Path.GetFullPath(Path.Combine(destinationDirectory, infoJSON.GetValue("fulltitle").ToString() + ".mp3"));
        hi.path_thumbnail = Path.GetFullPath(Path.Combine(Preferences.tempDirectoy, videoID + imageExtension));
        hi.title = infoJSON.GetValue("fulltitle").ToString();
        hi.url = infoJSON.GetValue("webpage_url").ToString();
        hi.time_created_UTC = DateTime.UtcNow;
        lock (Shared.lockHistory)
        {
            Shared.history.AddFirst(hi);
        }

        this.progressLabel.Invoke((MethodInvoker)delegate
        {
            this.progressLabel.Text = "✔";
            progressLabel.BackColor = Preferences.colorAccent4;
        });

        System.IO.File.WriteAllText(Path.Combine(Preferences.tempDirectoy, videoID) + ".txt", processOutput.ToString());
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        if (instanceRemoved == false)
        {
            if (downloadStarted)
            {
                lock (activeProcessesLock)
                {
                    activeProcesses--;
                }
            }
            instanceRemoved = true;
        }

        try
        {
            if (singleDownload != null && singleDownload.HasExited == false)
            {
                singleDownload.Exited -= SingleDownload_Exited;
                singleDownload.Kill();
            }
        }
        catch (Exception ex)
        {
        }

        LinkedListNode<DownloadContainer> nextNode = downloadNode.Next;
        lock (downloadsLock)
        {
            downloads.Remove(this);
        }

        if (downloadNode.Previous == null)
        {
            if (nextNode != null)
            {
                nextNode.Value.Top = this.Top;
            }
        }
        if (nextNode != null)
        {
            nextNode.Value.resize();
        }
        this.Dispose();
    }

    private string getExtension(string input)
    {
        int index = input.LastIndexOf(".");
        if (index < 0)
        {
            return null;
        }
        else
        {
            return input.Substring(index);
        }
    }

    // poglej ali je kreiran JSON in potem ali so ustvarjene ustrezne datoteke
    // kasneje naredi funkcije ki nastavljajo title/thumbnail in v tem primeru ce
    // video ze obstaja v tempu nastavi vrednosti UI-ja in ustrezno zakljuci process/thread
    private bool videoExists()
    {
        if (System.IO.File.Exists(Path.Combine(Preferences.tempDirectoy, ".info.json")))
        {
            return true;
        }
        return false;
    }

    private Random r1 = new Random();
    private double sizePercent = 0;
    private bool growing = true;
    private bool canAnimateThumbnail = true;
    private int perSide = 1; // 8
    private Color c1 = Preferences.colorAccent1;
    private Image getImageFrame()
    {
        for (int i = 0; i < 100; i++)
        {
            r1.Next(0, 1);
        }
        int side = 256; // 256 width height in pixels
        Image im = new Bitmap(side, side);
        using (Graphics g = Graphics.FromImage(im))
        {
            for (int i = 0; i < perSide; i++)
            {
                for (int j = 0; j < perSide; j++)
                {
                    int sideA = (int)((side / perSide));
                    g.FillRectangle(new SolidBrush(c1), ((side / perSide) * i) + (float)(((sideA - (sideA * sizePercent)) / 2)), ((side / perSide) * j) + (float)(((sideA - (sideA * sizePercent)) / 2)), (float)(sideA * sizePercent), (float)(sideA * sizePercent));
                }
            }
        }
        return im;
    }

    private static Image getImage(string path)
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

    private void resize()
    {
        this.SuspendLayout();

        if (downloadNode.Previous != null)
        {
            this.Top = downloadNode.Previous.Value.Top + downloadNode.Previous.Value.Height + 15;
            this.Left = downloadNode.Previous.Value.Left;
            this.Width = downloadNode.Previous.Value.Width;
            this.Height = downloadNode.Previous.Value.Height;
        }

        this.Height = 140 /*+ 400*/;
        int widthSetup = this.Width - thumbnail.Width - this.Left - 75;

        thumbnail.Height = this.Height;
        thumbnail.Width = (thumbnail.Height / 9) * 16;//Math.Min(200, (thumbnail.Height / 9) * 16);
        thumbnail.Left = this.Width - thumbnail.Width;
        thumbnail.Top = 0;

        progressBar.Height = 34;
        progressBar.Left = 60;
        progressBar.Width = (int)((widthSetup) * progress);
        progressBar.Top = this.Height - progressBar.Height - 15 - 15;

        title.Left = progressBar.Left;
        title.Top = 15;
        title.Width = widthSetup;
        title.Height = 50;

        cancelButton.Top = title.Top + 8;
        cancelButton.Left = 25;
        cancelButton.Height = 20;
        cancelButton.Width = 20;

        outputLog.Visible = false;
        if (outputLog.Visible)
        {
            outputLog.Top = 80;//progressBar.Top + progressBar.Height + 15;
            outputLog.Left = 15;
            outputLog.Width = this.Width - 50 - thumbnail.Width;
            outputLog.Height = 350;
        }

        this.ResumeLayout();

        if (downloadNode.Next != null)
        {
            downloadNode.Next.Value.resize();
        }
    }

    private void Thumbnail_Click(object sender, EventArgs e)
    {
        if (imageExtension == null)
        {
            canAnimateThumbnail = !canAnimateThumbnail;
        }
        else
        {
            Process.Start(Path.Combine(Preferences.tempDirectoy, videoID) + imageExtension);
        }
    }
}