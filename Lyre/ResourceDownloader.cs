using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LyreLibrary
{
    public class ResourceDownloader
    {
        private static LinkedList<ResourceDownloader> downloads = new LinkedList<ResourceDownloader>();
        private DownloaderAsync downloadAssistant;
        private StringBuilder logBuilder;
        private string outputPath;
        private string url;
        private string filename;
        private double progress;

        public LinkedList<ResourceDownloader> getDownloads()
        {
            lock (downloads)
            {
                return downloads;
            }
        }

        public ResourceDownloader()
        {
            progress = 0;
            logBuilder = new StringBuilder();
        }

        public void download(string url, string outputPath, string filename)
        {
            this.url = url;
            this.outputPath = outputPath;
            this.filename = filename;
            setDownloaderEvents();
            downloadAssistant.download(url);
        }

        public void setDownloaderEvents()
        {
            downloadAssistant = new DownloaderAsync();
            downloadAssistant.MyDownloadCompleted += DA_MyDownloadCompleted;
            downloadAssistant.MyDownloadChanged += DA_MyDownloadChanged;
        }

        private void DA_MyDownloadChanged(DownloaderAsync sender, DownloadProgressChangedEventArgs e)
        {
            progress = (double)e.BytesReceived / (double)e.TotalBytesToReceive;
            try
            {
                logBuilder.Append("[PROGRESS] " + (progress * 100).ToString("0.000") + " % " + Environment.NewLine);
            }
            catch (Exception ex) { }
        }

        private void DA_MyDownloadCompleted(DownloaderAsync sender, DownloadDataCompletedEventArgs e)
        {
            logBuilder.Append("[PROGRESS] Done");
            logBuilder.Append(sender.url.ToString() + ":" + Environment.NewLine);
            logBuilder.Append("[BYTES SIZE] " + sender.totalBytesToReceive.ToString() + Environment.NewLine);
            logBuilder.Append("[TIME NEEDED] (ms): " + sender.timeNeeded.TotalMilliseconds + Environment.NewLine + Environment.NewLine);
            try
            {
                Directory.CreateDirectory(outputPath);
                File.Create(Path.Combine(outputPath, filename)).Close();
                File.WriteAllBytes(Path.Combine(outputPath, filename), sender.data);
            }
            catch (Exception ex)
            {
                logBuilder.Append(ex.ToString() + Environment.NewLine + Environment.NewLine);
            }
        }
    }
}