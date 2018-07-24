using System;
using System.Collections.Generic;
using System.Net;

public class DownloaderAsync
{
    public Uri url;
    private WebClient webClient;
    public byte[] data;
    private bool _isDownloading;
    public double percent;
    public long bytesReceived;
    public long totalBytesToReceive;
    public DateTime timeStartedUTC;
    public DateTime timeFinishedUTC;
    public TimeSpan timeNeeded;
    public string filename;
    public long bytesPerSecond;
    private long bytesPerSecondLast;
    private DateTime timeElapsedLast; // since last progress update
    public List<string> outputPaths;

    public DownloaderAsync()
    {
        DownloaderAsyncInit();
    }

    public DownloaderAsync(List<string> outputPaths)
    {
        this.outputPaths = outputPaths;
        DownloaderAsyncInit();
    }

    private void DownloaderAsyncInit()
    {
        this.webClient = new WebClient();
        this.webClient.DownloadDataCompleted += WebClient_DownloadDataCompleted;
        this.webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
        this.data = new byte[0];
        this._isDownloading = false;
        this.percent = 0;
        this.bytesReceived = 0;
        this.totalBytesToReceive = 0;
        this.timeStartedUTC = DateTime.UtcNow;
        this.timeFinishedUTC = timeStartedUTC;
        this.timeNeeded = (this.timeFinishedUTC - this.timeStartedUTC);
        this.filename = "";
        this.bytesPerSecond = 0;
        this.bytesPerSecondLast = 0;
        this.timeElapsedLast = timeStartedUTC;
    }

    private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        this.bytesReceived = e.BytesReceived;
        this.totalBytesToReceive = e.TotalBytesToReceive;
        this.percent = e.ProgressPercentage;
        DateTime newLast = DateTime.UtcNow;
        long millisecondsElapsed = (long)(newLast - timeElapsedLast).TotalMilliseconds;
        long newBytes = bytesReceived - bytesPerSecondLast;
        bytesPerSecondLast = bytesReceived;
        bytesPerSecond = ((newBytes * 1000) / (Math.Max(1, millisecondsElapsed)));
        this.timeElapsedLast = newLast;
        OnDownloadChanged(this, e);
    }

    private void WebClient_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
    {
        this._isDownloading = false;
        this.timeFinishedUTC = DateTime.UtcNow;
        this.timeNeeded = (this.timeFinishedUTC - this.timeStartedUTC);
        this.data = e.Result;
        OnDownloadCompleted(this, e);
    }

    public void download(string url)
    {
        if (_isDownloading == false)
        {
            this.filename = getFilename(url);
            this.percent = 0;
            this.bytesReceived = 0;
            this.totalBytesToReceive = 0;
            this.timeStartedUTC = DateTime.UtcNow;
            this.data = new byte[0];
            this._isDownloading = true;
            this.url = new Uri(url);
            this.webClient.Proxy = null;
            this.timeElapsedLast = timeStartedUTC;
            this.bytesPerSecondLast = 0;
            this.bytesPerSecond = 0;
            this.webClient.DownloadDataAsync(this.url);
        }
    }

    public void cancel()
    {
        if (_isDownloading)
        {
            this.webClient.CancelAsync();
        }
    }

    public string getFilename(string url)
    {
        int index = url.LastIndexOf("/");
        if (index > -1)
        {
            url = url.Substring(index + 1, url.Length - index - 1);
            return url;
        }
        return "undefined";
    }

    public event MyDownloadCompletedEventHandler MyDownloadCompleted;
    public delegate void MyDownloadCompletedEventHandler(DownloaderAsync sender, DownloadDataCompletedEventArgs e);
    private void OnDownloadCompleted(DownloaderAsync sender, DownloadDataCompletedEventArgs e)
    {
        MyDownloadCompleted?.Invoke(this, e);
    }

    public event MyDownloadChangedEventHandler MyDownloadChanged;
    public delegate void MyDownloadChangedEventHandler(DownloaderAsync sender, DownloadProgressChangedEventArgs e);
    private void OnDownloadChanged(DownloaderAsync sender, DownloadProgressChangedEventArgs e)
    {
        MyDownloadChanged?.Invoke(this, e);
    }
}
