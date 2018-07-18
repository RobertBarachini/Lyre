using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Windows.Forms;

public class Shared
{
    // Main form
    public static Lyre.Form1 mainForm;

    public static object lockHistory = new object();
    [JsonProperty]
    public static LinkedList<HistoryItem> history = new LinkedList<HistoryItem>();
    public static readonly string filenameHistory = "history.json";
    public static readonly string filenameDlQueue = "downloadsQueue.json";
    public static string filePreferences = "preferences.json";
    // debug mode - print process output, ...
    public static bool debugMode = false; // true = debug mode enabled

    // shared preferences variable
    public static Preferences preferences = new Preferences(); // preferences object

    // resources
    public static readonly string resourcesWebsiteURL = "https://robertbarachini.github.io/projects/Lyre/resources/";
    public static readonly string resourcesDirectory = "resources";
    public static string FormControls_Minimize = "FormButtons_Minimize.png";
    public static string FormControls_Maximize = "FormButtons_Maximize.png";
    public static string FormControls_CloseSmall = "FormButtons_CloseSmall.png";
    public static string FormControls_CloseBig = "FormButtons_CloseBig.png";
    public static string FormControls_IMG_Directory = "IMG_Directory.png";
    public static string FormControls_IMG_Settings = "IMG_Settings.png";

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
}