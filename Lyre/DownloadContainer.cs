using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TagLib;

//
//// TODO
//
// - check if the download and encoding has been a success - check at event "Process.Exit" = mp3/video exists

class DownloadContainer : CcPanel
{
    private static object downloadsLock = new object();
    private static LinkedList<DownloadContainer> downloads = new LinkedList<DownloadContainer>();
    private static object downloadsQueueLock = new object();
    private static LinkedList<DownloadContainer> downloadsQueue = new LinkedList<DownloadContainer>();
    private static object activeProcessesLock = new object();
    private static int activeProcesses = 0;
    private LinkedListNode<DownloadContainer> downloadNode;
    private static System.Windows.Forms.Timer downloadsHandler = new System.Windows.Forms.Timer();

    public string getURL()
    {
        return url.ToString();
    }

    public static void removeAllControls()
    {
        lock (downloadsLock)
        {
            try
            {
                foreach (DownloadContainer dlC in downloads)
                {
                    try
                    {
                        dlC.removeThis();
                    }
                    catch (Exception ex) { }
                }
            }
            catch(Exception ex) { }
        }
    }

    public static int getActiveProcessesCount()
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

    public static int getDownloadsQueueCount()
    {
        lock(downloadsQueueLock)
        {
            return downloadsQueue.Count;
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
    private Process singleEncoder;
    private JObject infoJSON;
    private JObject mainJSON;
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
    private Status status;
    private string dlOutputPath;
    private string outputPath;

    public DownloadContainer()
    {
        InitThis();
    }

    private void InitThis()
    {
        status = Status.Idle;
        success = false;
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

    // Use for tracking information regarding process progress
    enum Status
    {
        Idle,
        Downloading,
        DownloadExited,
        Enconding,
        EndocingExited,
        WritingOutput,
        Done,
        Closing
    };

    private bool success;

    public Process getSingleDownload()
    {
        return singleDownload;
    }

    private bool isUniqueDownloadsID(string videoID, bool canConvertVal)
    {
        lock(downloadsLock)
        {
            foreach (DownloadContainer dc in downloads)
            {
                if(dc != this && dc.videoID.Equals(videoID) && dc.canConvert == canConvertVal)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void DownloadsHandler_Tick(object sender, EventArgs e)
    {
        lock (activeProcessesLock)
        {
            if (activeProcesses < Shared.preferences.maxActiveProcesses)
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

                            if (activeProcesses == Shared.preferences.maxActiveProcesses)
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
                progressLabel.Text = (newProgress * 100).ToString("0.0") + " %";
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

    private void initComp()
    {
        instanceRemoved = false;
        downloadStarted = false;
        processOutput = new StringBuilder();
        progress = 0;

        DoubleBuffered = true;
        BackColor = Shared.preferences.colorBackground;
        Font = Shared.preferences.fontDefault;

        thumbnail = new PictureBox()
        {
            Parent = this,
            BackColor = Shared.preferences.colorBackground,
            SizeMode = PictureBoxSizeMode.StretchImage, // Zoom should be used - change thumbnail.Left
            Image = getImageFrame(),//getImage("squareAnimation256.gif");
            Cursor = Cursors.Hand
        };
        thumbnail.Click += Thumbnail_Click;
        Controls.Add(thumbnail);

        title = new Label()
        {
            Parent = this,
            //Font = Shared.preferences.fontDefault,
            Text = "Queued for download ...",
            ForeColor = Shared.preferences.colorFontDefault,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font(Shared.preferences.fontDefault.FontFamily, 22, GraphicsUnit.Pixel),
            Cursor = Cursors.Hand
        };
        title.Click += Title_Click;
        Controls.Add(title);

        progressBar = new Panel()
        {
            Parent = this,
            BackColor = Shared.preferences.colorAccent1
        };
        Controls.Add(progressBar);

        progressLabel = new Label()
        {
            Parent = progressBar,
            Dock = DockStyle.Fill,
            ForeColor = Shared.preferences.colorFontDefault,
            BackColor = Shared.preferences.colorAccent1,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "",
            Font = new Font(Shared.preferences.fontDefault.FontFamily, 20, GraphicsUnit.Pixel)
        };
        progressLabel.Click += ProgressLabel_Click;
        progressBar.Controls.Add(progressLabel);

        outputLog = new RichTextBox()
        {
            Parent = this,
            BorderStyle = BorderStyle.None,
            BackColor = Shared.preferences.colorForeground,
            ForeColor = Shared.preferences.colorFontDefault,
            Font = new Font(Shared.preferences.fontDefault.FontFamily, 14, GraphicsUnit.Pixel)
        };
        Controls.Add(outputLog);

        cancelButton = new Panel()
        {
            Parent = this,
            BackColor = Shared.preferences.colorAccent2,
            Cursor = Cursors.Hand
        };
        //cancelButton.BackgroundImage = getImage("squareAnimation64.gif");
        cancelButton.Click += CancelButton_Click;
        Controls.Add(cancelButton);

        updateProgress(0);
        resize();
        previousProgress = progress;

        animateProgress = new System.Windows.Forms.Timer()
        {
            Interval = 50
        };
        animateProgress.Tick += AnimateProgress_Tick;
        animateProgress.Start();

        animateGeneral = new System.Windows.Forms.Timer()
        {
            Interval = 50
        };
        animateGeneral.Tick += AnimateGeneral_Tick;
        if (Shared.preferences.enableThumbnailAnimations)
        {
            animateGeneral.Start();
        }
    }

    private void Title_Click(object sender, EventArgs e)
    {
        if (url != null)
        {
            Process.Start(url.ToString());
        }
    }

    private void ProgressLabel_Click(object sender, EventArgs e)
    {
        if(status == Status.Done)
        {
            if(success)
            {
                Process.Start(outputPath);
            }
            else
            {
                this.Cursor = Cursors.Default;
                retryDownload();
            }
        }
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
                    if (chosen == 0) { c1 = Shared.preferences.colorAccent1; }
                    if (chosen == 1) { c1 = Shared.preferences.colorAccent2; }
                    if (chosen == 2) { c1 = Shared.preferences.colorAccent3; }
                    if (chosen == 3) { c1 = Shared.preferences.colorAccent4; }
                    //c1 = Shared.preferences.colorAccent1;
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

        if (Shared.preferences.enableThumbnailAnimations)
        {
            animateGeneral.Start();
        }
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

    private void retryDownload()
    {
        progress = 0;
        updateProgress(0);
        success = false;
        status = Status.Idle;
        animateProgress.Start();
        if (Shared.preferences.enableThumbnailAnimations)
        {
            animateGeneral.Start();
        }
        this.title.Invoke((MethodInvoker)delegate
        {
            this.title.Text = "Waiting for download";
        });
        this.progressLabel.Invoke((MethodInvoker)delegate
        {
            progressLabel.BackColor = Shared.preferences.colorAccent1;
        });
        this.progressBar.Invoke((MethodInvoker)delegate
        {
            progressLabel.BackColor = Shared.preferences.colorAccent1;
        });
        download_p(url.ToString(), destinationDirectory, canConvert);
    }

    public void download(string url, string destinationDirectory, bool canConvert)
    {
        this.videoID = SharedFunctions.getVideoID(url);
        string path = Path.Combine(Shared.preferences.tempDirectoy, videoID + ".info.json");


        //check if the output file is already on disk
        bool outputFileExists = false;
        
        if (System.IO.File.Exists(path) && canConvert)
        {
            infoJSON = JObject.Parse(System.IO.File.ReadAllText(path));
            string outputFilename = "";
            outputFilename = SharedFunctions.getValidFileName(infoJSON.GetValue("fulltitle").ToString());
            if(System.IO.File.Exists(Path.Combine(Shared.preferences.downloadsDirectory, outputFilename + ".mp3")))
            {
                outputFileExists = true;
                outputPath = Path.Combine(Shared.preferences.downloadsDirectory, outputFilename + ".mp3");
            }
        }

        if (outputFileExists)
        {
            finished = true;
            animateGeneral.Stop();
            updateProgress(1);
            this.progressLabel.Invoke((MethodInvoker)delegate
            {
                progressLabel.BackColor = Shared.preferences.colorAccent4;
                progressLabel.Text = "Already on disk";
                progressLabel.Cursor = Cursors.Hand;
                status = Status.Done;
                success = true;
            });
            animateProgress.Stop();
            this.title.Invoke((MethodInvoker)delegate
            {
                this.title.Text = infoJSON.GetValue("fulltitle").ToString();
            });
            string fileURL = infoJSON["thumbnails"].First["url"].ToString();
            imageExtension = SharedFunctions.getExtension(fileURL);
            string filename = infoJSON.GetValue("display_id").ToString();
            this.thumbnail.Invoke((MethodInvoker)delegate
            {
                thumbnail.Image = SharedFunctions.getImage(Path.Combine(Shared.preferences.tempDirectoy, filename + imageExtension));
            });
        }
        else
        {
            // only add to downloads if the videoID is unique = don't download twice
            if (isUniqueDownloadsID(SharedFunctions.getVideoID(url), canConvert))
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
            else
            {
                removeThis();
            }
        }
    }

    private void download_p(string url, string destinationDirectory, bool canConvert)
    {
        //animateProgress.Start();

        // Push the newly activated download to the top of the list
        lock (downloadsLock)
        {
            downloads.Remove(this);
            downloads.AddFirst(this);
            downloadNode = downloads.First;
        }
        // Call resize to change the order of download controls shown in download container
        Shared.mainForm.Invoke((MethodInvoker)delegate
        {
            Shared.mainForm.resizeDcMain();
        });

        if (Directory.Exists(Shared.preferences.tempDirectoy) == false)
        {
            Directory.CreateDirectory(Shared.preferences.tempDirectoy);
        }
        if (Directory.Exists(destinationDirectory) == false)
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        this.videoID = SharedFunctions.getVideoID(url);
        string arguments = "";

        singleDownload = new Process();
        {
            if (canConvert)
            {
                //arguments = "--extract-audio --audio-format mp3 " + "-o \"" + Path.Combine(Shared.preferences.tempDirectoy, videoID + ".%(ext)s") + "\" " + url + " --write-thumbnail --write-info-json --audio-quality 0";
                arguments = "--extract-audio " + "-o \"" + Path.Combine(Shared.preferences.tempDirectoy, videoID + ".%(ext)s") + "\" " + url + " --write-thumbnail --write-info-json --audio-quality 0";
            }
            else
            {
                arguments = "-o \"" + Path.Combine(Shared.preferences.tempDirectoy, videoID + ".%(ext)s") + "\" " + url + " --write-thumbnail --write-info-json -f bestvideo[height<=?" + Shared.getVideoQualityStringPure(Shared.preferences.maxVideoQualitySelector) + "][fps<=?" + Shared.getVideoFrameRatePure(Shared.preferences.maxVideoFrameRateSelector) + "]+bestaudio";
            }
            singleDownload.StartInfo.FileName = @"youtube-dl.exe";
            singleDownload.StartInfo.Arguments = arguments;
            singleDownload.StartInfo.CreateNoWindow = true;
            singleDownload.StartInfo.UseShellExecute = false;
            singleDownload.StartInfo.RedirectStandardOutput = true;
            singleDownload.OutputDataReceived += SingleDownload_OutputDataReceived;
            singleDownload.EnableRaisingEvents = true;
            singleDownload.Exited += SingleDownload_Exited;
            singleDownload.Start();
            status = Status.Downloading;
            singleDownload.BeginOutputReadLine();
        }
    }

    private void SingleDownload_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (status == Status.Closing)
        {
            return;
        }

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
                string path = Path.Combine(Shared.preferences.tempDirectoy, videoID + ".info.json");
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
                imageExtension = SharedFunctions.getExtension(fileURL);
                this.thumbnail.Invoke((MethodInvoker)delegate
                {
                    thumbnail.Image = SharedFunctions.getImage(Path.Combine(Shared.preferences.tempDirectoy, infoJSON.GetValue("display_id").ToString() + imageExtension));
                });
            }
            else if(data.Contains("[ffmpeg] Destination: "))
            {
                dlOutputPath = data.Substring("[ffmpeg] Destination: ".Length);
            }
            else if(data.Contains("[download] Destination: "))
            {
                dlOutputPath = data.Substring("[download] Destination: ".Length);
            }
            else if(data.Contains("[ffmpeg] Merging formats into"))
            {
                dlOutputPath = data.Substring("[ffmpeg] Merging formats into \"".Length);
                dlOutputPath = dlOutputPath.Substring(0, dlOutputPath.Length - 1);
                infoJSON["ext"] = dlOutputPath.Substring(dlOutputPath.LastIndexOf(".") + 1);
            }
            else if(data.Contains("has already been downloaded"))
            {
                data = data.Substring(data.LastIndexOf(".") + 1);
                data = data.Substring(0, data.IndexOf(" "));
                infoJSON["ext"] = data;
            }
            else if(data.ToLower().Contains("warning") || data.ToLower().Contains("error"))
            {
                //finishProcess();
                //reportDownloadError("❌ ERROR - Cannot download video - check video source");
                singleDownload.Kill();
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
        string path = Path.Combine(Shared.preferences.tempDirectoy, videoID + ".info.json");
        if (infoJSON == null)
        {
            if (System.IO.File.Exists(path))
            {
                infoJSON = JObject.Parse(System.IO.File.ReadAllText(path));
            }
            else
            {
                reportDownloadError();
            }
        }

        status = Status.DownloadExited;

        if (Shared.debugMode)
        {
            System.IO.File.WriteAllText("crashlogDowExi.txt", processOutput.ToString());
        }

        // Construct the start of mainJSON
        try
        {
            constructMainJSON(0);
        }
        catch (Exception ex) { }

        if(dlOutputPath == null) // how to catch output path in any given situation ?
        {
            reportDownloadError();
        }
        if (canConvert)
        {
            if (progress == 1)
            {
                encodeOutput();
            }
            else // something unexpected happened - report it
            {
                reportDownloadError();
            }
        }
        else
        {
            finishProcess();
        }
    }

    private void reportDownloadError()
    {
        reportDownloadError("❌ Failed - Click here to retry");
    }

    private void reportDownloadError(string message)
    {
        status = Status.Done;
        success = false;
        animateProgress.Stop();
        updateProgress(1);
        this.progressLabel.Invoke((MethodInvoker)delegate
        {
            progressLabel.Cursor = Cursors.Hand;
            this.progressLabel.Text = message;
            progressLabel.BackColor = Shared.preferences.colorAccent4;
        });
        this.title.Invoke((MethodInvoker)delegate
        {
            this.title.Text = url.ToString();
        });
    }

    private void encodeOutput()
    {
        string arguments = "";

        singleEncoder = new Process();
        {
            string newPath = dlOutputPath.Substring(0, dlOutputPath.LastIndexOf("."));
            arguments = "-v debug -i \"" + dlOutputPath /*Path.Combine(Shared.preferences.tempDirectoy, videoID) + ".webm"*/ + "\" -f mp3 -b:a 320k \"" + /*Path.Combine(Shared.preferences.downloadsDirectory, videoID)*//* + ".mp3"*/newPath + ".mp3\" -y";
            singleEncoder.StartInfo.FileName = @"ffmpeg.exe";
            singleEncoder.StartInfo.Arguments = arguments;
            singleEncoder.StartInfo.CreateNoWindow = true;
            singleEncoder.StartInfo.UseShellExecute = false;
            singleEncoder.StartInfo.RedirectStandardOutput = true;
            singleEncoder.StartInfo.RedirectStandardError = true;
            singleEncoder.OutputDataReceived += SingleEncoder_OutputDataReceived;
            singleEncoder.ErrorDataReceived += SingleEncoder_ErrorDataReceived;
            singleEncoder.EnableRaisingEvents = true;
            singleEncoder.Exited += SingleEncoder_Exited;
            singleEncoder.Start();
            singleEncoder.BeginOutputReadLine();
            singleEncoder.BeginErrorReadLine();
            status = Status.Enconding;
            this.progressLabel.Invoke((MethodInvoker)delegate
            {
                progressLabel.BackColor = Shared.preferences.colorAccent3;
                progressBar.BackColor = Shared.preferences.colorAccent3;
            });
            progress = 0;
            updateProgress(0);
        }
    }

    private void constructMainJSON(int stage)
    {
        if (stage == 0)
        {
            mainJSON = new JObject();
            string propertyName = "";
            //
            propertyName = "uploader";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "title";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "fulltitle";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "uploader_id";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //
            propertyName = "player_url";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "webpage_url";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "uploader_url";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "display_id";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "id";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "url";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "webpage_url_basename";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "protocol";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //
            propertyName = "description";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "thumbnail";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "_filename";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //
            propertyName = "filesize";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "duration";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "view_count";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "average_count";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "like_count";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "dislike_count";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "upload_date";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //
            propertyName = "license";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "format";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "format_note";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "ext";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "acodec";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "vcodec";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //
            propertyName = "thumbnails";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "categories";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            propertyName = "tags";
            mainJSON.Add(propertyName, infoJSON.GetValue(propertyName));
            //////

        }

        string jsonString = JsonConvert.SerializeObject(mainJSON, Formatting.Indented);
        System.IO.File.WriteAllText(Path.Combine(Shared.preferences.tempDirectoy, videoID + ".main.json"), jsonString);
    }

    private string duration;
    private void SingleEncoder_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (status == Status.Closing)
        {
            return;
        }

        if (e.Data != null)
        {
            processOutput.Append("STD_ERR: " + e.Data + Environment.NewLine);
            if(e.Data.Contains("Duration: "))
            {
                duration = e.Data.Substring(e.Data.IndexOf("Duration: ") + "Duration: ".Length);
                duration = duration.Substring(0, duration.IndexOf(","));
            }
            else if(e.Data.Contains("time="))
            {
                string currentDuration = e.Data.Substring(e.Data.IndexOf("time=") + "time=".Length);
                currentDuration = currentDuration.Substring(0, currentDuration.IndexOf(" bitrate="));
                double totalSeconds = durationToSeconds(duration);
                double currentSeconds = durationToSeconds(currentDuration);
                double somePregress = currentSeconds / totalSeconds;
                //updateProgress(somePregress);
                progress = somePregress;
            }
        }
    }

    private double durationToSeconds(string inputDuration)
    {
        string[] timeSlices = inputDuration.Split(':');
        double seconds = 0;
        seconds += Convert.ToDouble(timeSlices[0]) * 3600; // hours
        seconds += Convert.ToDouble(timeSlices[1]) * 60; // minutes
        seconds += Convert.ToDouble(timeSlices[2], CultureInfo.InvariantCulture); // seconds
        return seconds;
    }

    private void SingleEncoder_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if(status == Status.Closing)
        {
            return;
        }

        if (Shared.debugMode)
        {
            System.IO.File.WriteAllText("crashlogEncOUT.txt", processOutput.ToString());
        }

        if (e.Data != null)
        {
            processOutput.Append("STD_OUT: " + e.Data + Environment.NewLine);
        }
    }

    private void SingleEncoder_Exited(object sender, EventArgs e)
    {
        finishProcess();
    }

    private void finishProcess()
    {
        if (canConvert)
        {
            status = Status.EndocingExited;
        }
        else
        {
            status = Status.DownloadExited;
        }

        if (Shared.debugMode)
        {
            System.IO.File.WriteAllText("crashlog.txt", processOutput.ToString());
        }

        finished = true;
        animateProgress.Stop();
        progress = 1;
        updateProgress(1);

        if (instanceRemoved == false)
        {
            lock (activeProcessesLock)
            {
                activeProcesses--;
            }
            instanceRemoved = true;
        }

        if (canConvert)
        {
            try
            {
                TagLib.File soundFile = TagLib.File.Create(Path.Combine(Shared.preferences.tempDirectoy, videoID + ".mp3"));
                IPicture albumArt = new Picture(Path.Combine(Shared.preferences.tempDirectoy, videoID + imageExtension));
                soundFile.Tag.Pictures = new IPicture[1] { albumArt };
                soundFile.Tag.Comment = videoID;
                soundFile.Save();
            }
            catch (Exception ex) { }
        }

        try
        {
            string outputFileName = infoJSON.GetValue("fulltitle").ToString();
            outputFileName = SharedFunctions.getValidFileName(outputFileName);
            status = Status.WritingOutput;

            string extension = "";
            extension = canConvert ? ".mp3" : "." + infoJSON.GetValue("ext").ToString(); // ext / _filename

            outputPath = Path.Combine(destinationDirectory, outputFileName + extension);

            try
            {
                System.IO.File.Move(Path.Combine(Shared.preferences.tempDirectoy, videoID) + extension, outputPath);
            }
            catch (Exception ex) { }

            if (System.IO.File.Exists(outputPath))
            {
                success = true;
            }
            else
            {
                success = false;
            }

            try
            {
                System.IO.File.Delete(Path.Combine(Shared.preferences.tempDirectoy, videoID) + extension);
            }
            catch (Exception ex) { }

            try
            {
                System.IO.File.Delete(dlOutputPath);
            }
            catch (Exception ex) { }

            //EXAMPLE : Path.GetFullPath((new Uri(absolute_path)).LocalPath);
            HistoryItem hi = new HistoryItem()
            {
                path_output = Path.GetFullPath(Path.Combine(destinationDirectory, outputFileName + extension)),
                path_thumbnail = Path.GetFullPath(Path.Combine(Shared.preferences.tempDirectoy, videoID + imageExtension)),
                title = infoJSON.GetValue("fulltitle").ToString(),
                url = infoJSON.GetValue("webpage_url").ToString(),
                time_created_UTC = DateTime.UtcNow
            };

            lock (Shared.lockHistory)
            {
                Shared.history.AddFirst(hi);
            }
        }
        catch(Exception ex) { }

        this.progressLabel.Invoke((MethodInvoker)delegate
        {
            if (success)
            {
                this.progressLabel.Text = "✔";
                this.progressLabel.Cursor = Cursors.Hand;
            }
            else
            {
                this.progressLabel.Text = "❌ Try downloading again";
            }
            progressLabel.BackColor = Shared.preferences.colorAccent4;
        });

        System.IO.File.WriteAllText(Path.Combine(Shared.preferences.tempDirectoy, videoID) + ".txt", processOutput.ToString());
        status = Status.Done;
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        removeThis();
    }

    private void removeThis()
    {
        status = Status.Closing;
        animateProgress.Stop();
        animateGeneral.Stop();

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
                singleDownload.OutputDataReceived -= SingleDownload_OutputDataReceived;
                singleDownload.Exited -= SingleDownload_Exited;
                singleDownload.Kill();
            }
        }
        catch (Exception ex)
        {
        }

        try
        {
            if(singleEncoder != null && singleEncoder.HasExited == false)
            {
                singleEncoder.OutputDataReceived -= SingleEncoder_OutputDataReceived;
                singleEncoder.ErrorDataReceived -= SingleEncoder_ErrorDataReceived;
                singleEncoder.Exited -= SingleEncoder_Exited;
            }
        }
        catch(Exception ex)
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

    // poglej ali je kreiran JSON in potem ali so ustvarjene ustrezne datoteke
    // kasneje naredi funkcije ki nastavljajo title/thumbnail in v tem primeru ce
    // video ze obstaja v tempu nastavi vrednosti UI-ja in ustrezno zakljuci process/thread
    private bool videoExists()
    {
        if (System.IO.File.Exists(Path.Combine(Shared.preferences.tempDirectoy, ".info.json")))
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
    private Color c1 = Shared.preferences.colorAccent1;
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
            Process.Start(Path.Combine(Shared.preferences.tempDirectoy, videoID) + imageExtension);
        }
    }
}