using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

public class Shared
{
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
}