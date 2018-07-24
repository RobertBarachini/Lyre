using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace LyreUpdater
{
    //
    ////  TODO
    //
    // Updating the updater from Lyre.exe...
    // Fix LyreLibrary.dll being used by another process...

    public partial class Form1 : Form
    {
        private Panel ccContainer;
        private RichTextBox ccMainLog;

        int resourcesMissingCount = 0;
        private object resourcesMissingCountLock = new object();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Directory.SetCurrentDirectory(OnlineResource.pathToThis);

            if (Directory.Exists(Shared.tempDirectory) == false)
            {
                Directory.CreateDirectory(Shared.tempDirectory);
            }

            Text = OnlineResource.context;// + " ; " + Directory.GetCurrentDirectory();

            try
            {
                loadSources();
            }
            catch(Exception ex) { }

            InitComponents();

            ResizeComponents();

            BringToFront();

            ccMainLog.AppendText(Environment.NewLine + "Context: " + OnlineResource.context + Environment.NewLine);
            ccMainLog.AppendText("Updater folder: " + OnlineResource.LyreUpdaterLocation + Environment.NewLine);
            ccMainLog.AppendText("Downloader folder: " + OnlineResource.LyreDownloaderLocation + Environment.NewLine + Environment.NewLine);

            DownloadRemoteResourcesJson();
        }

        List<OnlineResource> remoteResourcesList;

        private void InitComponents()
        {
            //Text = "LyreUpdater - Updater for Lyre.exe";
            FormClosing += Form1_FormClosing;
            BackColor = Shared.preferences.colorForeground;
            Width = 800;
            Height = 420;
            FormClosing += Form1_FormClosing1;

            ccContainer = new Panel
            {
                Parent = this,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(ccContainer);

            ccMainLog = new RichTextBox
            {
                Parent = ccContainer,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Shared.preferences.colorBackground,
                ForeColor = Shared.preferences.colorFontDefault,
                Font = Shared.preferences.fontDefault,
                Text = "Lyre Updater" + Environment.NewLine
            };
        }

        private void Form1_FormClosing1(object sender, FormClosingEventArgs e)
        {
            //try
            //{
            //    Process p = new Process();
            //    ProcessStartInfo ps = new ProcessStartInfo();
            //    ps.FileName = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "Lyre.exe");
            //    p.StartInfo = ps;
            //    p.Start();
            //}
            //catch (Exception ex) { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveSources();
        }

        private void ResizeComponents()
        {
            // NI
        }

        private void loadSources()
        {
            SharedFunctions.loadJSON(Shared.filePreferences, ref Shared.preferences);

            if(File.Exists(Shared.fileResources))
            {
                SharedFunctions.loadJSON(Shared.fileResources, ref Shared.completeResourceList);
            }
            else
            {
                Shared.completeResourceList = new List<OnlineResource>();
                Shared.completeResourceList.AddRange(OnlineResource.resourcesListDownloader);
                Shared.completeResourceList.AddRange(OnlineResource.resourcesListUpdater);

                // Remove itself from the list
                Shared.completeResourceList.RemoveAll(onR => onR.url == OnlineResource.resourcesWebsiteURL + OnlineResource.LyreUpdaterString);
                // Save it
                SharedFunctions.saveJSON(Shared.fileResources, Shared.completeResourceList);
            }
        }

        private void saveSources()
        {
            SharedFunctions.saveJSON(Shared.filePreferences, Shared.preferences);
            //SharedFunctions.saveJSON(Shared.fileResources, remoteResourcesList);
        }

        // Actual updater section

        private void DownloadRemoteResourcesJson()
        {
            Text = OnlineResource.LyreUpdaterLocation;
            DownloaderAsync daResourcesList = new DownloaderAsync(new List<string> { Path.Combine(OnlineResource.LyreUpdaterLocation, Shared.tempDirectory, "remoteResources.json") });
            daResourcesList.MyDownloadCompleted += DaResourcesList_MyDownloadCompleted;
            daResourcesList.MyDownloadChanged += DaResourcesList_MyDownloadChanged;
            daResourcesList.download(OnlineResource.resourcesWebsiteURL + "resources.json");
        }

        private void DaResourcesList_MyDownloadChanged(DownloaderAsync sender, DownloadProgressChangedEventArgs e)
        {
        }

        private void DaResourcesList_MyDownloadCompleted(DownloaderAsync sender, DownloadDataCompletedEventArgs e)
        {
            // write the file
            File.Create(Path.Combine(OnlineResource.LyreUpdaterLocation, Shared.tempDirectory, "remoteResources.json")).Close();
            File.WriteAllBytes(Path.Combine(OnlineResource.LyreUpdaterLocation, Shared.tempDirectory, "remoteResources.json"), sender.data);
            SharedFunctions.loadJSON(sender.outputPaths[0], ref remoteResourcesList);

            // this could be improved by matching version/iteration numbers of 
            // local files where there is more than one file of the same resource instance
            int hits = 0;
            foreach (OnlineResource onR in remoteResourcesList)
            {
                foreach (string path in onR.paths)
                {
                    if (File.Exists(path) == false || newerAvailable(onR))
                    {
                        hits++;
                        lock (resourcesMissingCountLock)
                        {
                            resourcesMissingCount++;
                            // NAH download the file to a temporary folder and replace the older one only after 
                            // NAH a successful download
                            DownloaderAsync newDA = new DownloaderAsync(onR.paths); //Shared.tempDirectory + Path.GetFileName(path));
                            setDownloaderEvents(newDA);
                            newDA.download(onR.url);
                        }
                        break;
                    }
                }
            }
            if(hits == 0)
            {
                ccMainLog.AppendText("No new updates found..." + Environment.NewLine);
            }
        }

        private bool newerAvailable(OnlineResource onR)
        {
            foreach(OnlineResource onRLocal in Shared.completeResourceList)
            {
                if(onRLocal.url.Equals(onR.url) && onRLocal.iteration < onR.iteration)
                {
                    return true;
                }
            }

            return false;
        }

        private void setDownloaderEvents(DownloaderAsync whichToSet)
        {
            whichToSet.MyDownloadCompleted += DA_MyDownloadCompleted;
        }

        private void DA_MyDownloadChanged(DownloaderAsync sender, DownloadProgressChangedEventArgs e)
        {
            double progress = (double)e.BytesReceived / (double)e.TotalBytesToReceive;
            try
            {
                ccMainLog.AppendText(sender.filename + " : [PROGRESS] " + (progress * 100).ToString("0.000") + " % " + Environment.NewLine);
                ccMainLog.ScrollToCaret();
            }
            catch (Exception ex) { }
        }

        private void DA_MyDownloadCompleted(DownloaderAsync sender, DownloadDataCompletedEventArgs e)
        {
            ccMainLog.AppendText(sender.filename + " : [PROGRESS] Done" + Environment.NewLine);
            ccMainLog.AppendText(sender.filename + " : " + sender.url.ToString() + ":" + Environment.NewLine);
            ccMainLog.AppendText(sender.filename + " : [BYTES SIZE] " + sender.totalBytesToReceive.ToString() + Environment.NewLine);
            ccMainLog.AppendText(sender.filename + " : [TIME NEEDED] (ms): " + Math.Round(sender.timeNeeded.TotalMilliseconds) + Environment.NewLine + Environment.NewLine);
            ccMainLog.ScrollToCaret();

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
                ccMainLog.AppendText(sender.filename + Environment.NewLine + ex.ToString() + Environment.NewLine + Environment.NewLine);
            }

            lock (resourcesMissingCountLock)
            {
                if (resourcesMissingCount == 0)
                {
                    ccMainLog.AppendText("All resources downloaded..." + Environment.NewLine);
                    ccMainLog.AppendText("Saving update log..." + Environment.NewLine);
                    ccMainLog.AppendText("Replacing resources.json..." + Environment.NewLine + Environment.NewLine);
                    ccMainLog.AppendText("ALL DONE :)");
                    File.WriteAllText("lastUpdateLog.txt", ccMainLog.Text);
                    // replace the local resources list with the remote one
                    File.Create(Path.Combine(OnlineResource.LyreUpdaterLocation, "resources.json")).Close();
                    SharedFunctions.saveJSON(Path.Combine(OnlineResource.LyreUpdaterLocation, "resources.json"), remoteResourcesList);
                    ccMainLog.ScrollToCaret();
                }
            }
        }
    }
}
