using System;
using System.Collections.Generic;

using Newtonsoft.Json;

public class Shared // Common functions, variables for Lyre downloader project
{
    // Main form
    public static Lyre.Form1 mainForm;

    public static object lockHistory = new object();
    [JsonProperty]
    public static LinkedList<HistoryItem> history = new LinkedList<HistoryItem>();
    public static readonly string filenameHistory = "history.json";
    public static readonly string filenameDlQueue = "downloadsQueue.json";
    public static string filePreferences = "preferences.json";
    public static string downloadsDataDirectory = "data";
    public static string thumbnailsDirecotory = "thumbnails";
    // debug mode - print process output, ...
    public static bool debugMode = false; // true = debug mode enabled
    public static bool updatePressed = false;

    // shared preferences variable
    public static Preferences preferences = new Preferences(); // preferences object

    public static string getAudioQualityString(int qualitySelector)
    {
        if(qualitySelector == 0)
        {
            return "mp3 - Auto bitrate";
        }

        string selectedQ = getAudioQualityStringPure(qualitySelector);

        return "mp3 - " + selectedQ + "kbps";
    }

    public static string getAudioQualityStringPure(int qualitySelector)
    {
        qualitySelector = Math.Max(Math.Min(qualitySelector, 5), 0); // Currently 5 are supported
        string selectedQ = "320";

        if (qualitySelector == 0) { selectedQ = ""; }
        else if (qualitySelector == 1) { selectedQ = "128"; }
        else if (qualitySelector == 2) { selectedQ = "192"; }
        else if (qualitySelector == 3) { selectedQ = "256"; }
        else if (qualitySelector == 4) { selectedQ = "320"; }
        // AUTO, 128k, 192k, 256k, 320k
        return selectedQ;
    }

    public static string getVideoQualityString(int qualitySelector)
    {
        string selectedQ = getVideoQualityStringPure(qualitySelector);

        if (selectedQ.Equals("1440")) { selectedQ = "QHD"; }
        else if (selectedQ.Equals("2160")) { selectedQ = "4K UHD"; }
        else if (selectedQ.Equals("4320")) { selectedQ = "8K UHD"; }
        else { selectedQ += "p"; }

        return selectedQ;
    }

    public static string getVideoQualityStringPure(int qualitySelector)
    {
        qualitySelector = Math.Max(Math.Min(qualitySelector, 8), 0); // Currently 8 are supported
        string selectedQ = "1080";

        if (qualitySelector == 0) { selectedQ = "144"; }
        else if (qualitySelector == 1) { selectedQ = "240"; }
        else if (qualitySelector == 2) { selectedQ = "360"; }
        else if (qualitySelector == 3) { selectedQ = "480"; }
        else if (qualitySelector == 4) { selectedQ = "720"; }
        else if (qualitySelector == 5) { selectedQ = "1080"; }
        else if (qualitySelector == 6) { selectedQ = "1440"; }
        else if (qualitySelector == 7) { selectedQ = "2160"; }
        else if (qualitySelector == 8) { selectedQ = "4320"; }

        return selectedQ;
    }

    public static string getVideoFrameRatePure(int fpsSelector)
    {
        fpsSelector = Math.Max(Math.Min(fpsSelector, 5), 0); // 24, 25, 30, 48, 50, 60
        string fps = "30";

        if (fpsSelector == 0) { fps = "24"; }
        else if (fpsSelector == 1) { fps = "25"; }
        else if (fpsSelector == 2) { fps = "30"; }
        else if (fpsSelector == 3) { fps = "48"; }
        else if (fpsSelector == 4) { fps = "50"; }
        else if (fpsSelector == 5) { fps = "60"; }

        return fps.ToString();
    }

    public static void increaseAudioQuality()
    {
        preferences.maxAudioQualitySelector++;
        if(preferences.maxAudioQualitySelector > 4)
        {
            preferences.maxAudioQualitySelector = 0;
        }
    }

    public static void decreaseAudioQuality()
    {
        preferences.maxAudioQualitySelector--;
        if(preferences.maxAudioQualitySelector < 0)
        {
            preferences.maxAudioQualitySelector = 4;
        }
    }

    public static void increaseVideoQuality()
    {
        if(preferences.maxVideoFrameRateSelector == 2)
        {
            preferences.maxVideoFrameRateSelector = 5;
        }
        else
        {
            preferences.maxVideoFrameRateSelector = 2;
            preferences.maxVideoQualitySelector++;
            if(preferences.maxVideoQualitySelector > 8)
            {
                preferences.maxVideoQualitySelector = 0;
            }
        }
    }

    public static void decreaseVideoQuality()
    {
        if (preferences.maxVideoFrameRateSelector == 5)
        {
            preferences.maxVideoFrameRateSelector = 2;
        }
        else
        {
            preferences.maxVideoFrameRateSelector = 5;
            preferences.maxVideoQualitySelector--;
            if (preferences.maxVideoQualitySelector < 0)
            {
                preferences.maxVideoQualitySelector = 8;
            }
        }
    }

    public static int getUnfinishedDownloadsCount()
    {
        int downloadsActive = DownloadContainer.getActiveProcessesCount();
        int downloadsInQueue = DownloadContainer.getDownloadsQueueCount();
        int downloadsInPreQueue = mainForm.getDownloadsPreQueueCount();

        int downloadsUnfinished = downloadsActive + downloadsInQueue + downloadsInPreQueue;

        return downloadsUnfinished;
    }

    public static string instructionsBasic = @"Copy a YouTube link and press 'ctrl + v' anywhere inside this program to start the download.";

    public static string instructions = @"General directions

To use the downloader section of the program you can copy and paste YouTube links (or text containing links) anywhere inside the program. If the links are registered, the download should start.

Preferences

If you wish to increase the maximum video quality you can click on the top left button, labeled as (1080p 30). To decrease the maximum video quality use the right mouse button. To increase maximum number of concurrent downloads (waiting to start) click the button to the right of the component labeled as 'Downloads:'. To increase the number of concurrent videos that are being downloaded and converted click the button to the right of component, labeled as 'Active downloads:'. To decrease each of both values, use the right mouse button. We suggest you keep these settings at default for best performance. If you wish to convert future downloads to '.mp3' click the toggle button to the right of component labeled as 'Convert to.mp3'.

Download panel

If you wish to cancel a download you can click the small square which is located to the left of the video title. To play the finished download click the progress bar.To view the video thumbnail, click the on the image which is located at the very right of a download panel.

History viewer

To open history viewer (containing downloads history), click the button located at the top right of this program. To play the video use left mouse button on the title of a video. To visit the download's source use the right mouse button.

To gain access to more settings click the hexagonal button at the top left of the program.
To gain even more information regarding the program or to give feedback, please feel free to contact me at program.lyre@gmail.com.

To support the project, visit https://robertbarachini.github.io/donate.
To learn more about the coding side, visit us at https://github.com/RobertBarachini/Lyre.

I hope you will enjoy my program!

Best wishes,
Robert Barachini";
}