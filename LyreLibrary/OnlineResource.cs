using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public class OnlineResource
{
    public string credit; // who to credit - can be a name or a link to their site
    public string url; // location of resource file online
    public List<string> paths; // where to write the resource - resource can be present at multiple locations on disk
    public bool askForPermission; // wait for users' approval to download // TO DO
    public bool waitForUser; // wait for user to close a finished download // TO DO
    public int iteration; // indicator of each item update iteration - sort of like a file version number
                          // increase this number each time you update the appropriate file from local to online repository

    public OnlineResource(string credit, string url, List<string> paths, bool askForPermission, bool waitForUser, int iteration)
    {
        this.credit = credit;
        this.url = url;
        this.paths = paths;
        this.askForPermission = askForPermission;
        this.waitForUser = waitForUser;
        this.iteration = iteration;
    }

    // resources
    public static readonly string resourcesWebsiteURL = "https://robertbarachini.github.io/projects/Lyre/resources/";
    public static readonly string resourcesDirectory = "resources";
    public static string FormControls_Minimize = "FormButtons_Minimize.png";
    public static string FormControls_Maximize = "FormButtons_Maximize.png";
    public static string FormControls_CloseSmall = "FormButtons_CloseSmall.png";
    public static string FormControls_CloseBig = "FormButtons_CloseBig.png";
    public static string FormControls_IMG_Directory = "IMG_Directory.png";
    public static string FormControls_IMG_Settings = "IMG_Settings.png";

    public static string LyreDownloaderString = "Lyre.exe";
    public static string LyreUpdaterString = "LyreUpdater.exe";
    //public static string LyreUpdaterLocation = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine("Updater", LyreUpdaterString));
    //public static string LyreDownloaderLocation = Path.Combine(Directory.GetParent(LyreUpdaterLocation).Parent.FullName, LyreDownloaderString);

    public static string pathToThis = "";
    public static string context = "";
    public static string LyreDownloaderLocation = "";//Directory.GetParent(LyreUpdaterLocation).FullName;
    public static string LyreUpdaterLocation = setContext();

    private static string setContext()
    {
        string dName = "";
        pathToThis = getAssemblyDirectory();

        if(File.Exists(Path.Combine(pathToThis, "LyreUpdater.exe")) == false)
        {
            context = "Lyre.exe";
            dName = Path.Combine(getAssemblyDirectory(), "updater");
            LyreDownloaderLocation = pathToThis;
        }
        else
        {
            context = "LyreUpdater.exe";
            dName = pathToThis;
            LyreDownloaderLocation = Directory.GetParent(dName).FullName;
        }

        return dName;

        //string dName = Directory.GetCurrentDirectory();
        //dName = dName.Substring(dName.LastIndexOf("\\") + 1);
        //return Path.Combine(Directory.GetCurrentDirectory(), File.Exists("resources.json")/*dName.Equals("updater")*/ ? "" : "updater");
    }

    private static string getAssemblyDirectory()
    {
        // https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        return Path.GetDirectoryName(codeBase).Substring(6);
        //UriBuilder uri = new UriBuilder(codeBase);
        //string path = Uri.UnescapeDataString(uri.Path);
        //return Path.GetDirectoryName(path);
    }

    // contains all resource and dependency links for Lyre Downloader
    public static List<OnlineResource> resourcesListDownloader = new List<OnlineResource>()
    {
        new OnlineResource
        (
            "https://www.ffmpeg.org/",
            resourcesWebsiteURL + "ffmpeg.exe",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "ffmpeg.exe")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://www.ffmpeg.org/",
            resourcesWebsiteURL + "ffprobe.exe",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "ffprobe.exe")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_CloseBig,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_CloseBig)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_CloseSmall,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_CloseSmall)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_Maximize,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_Maximize)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_Minimize,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_Minimize)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + "Icon3.png",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, "Icon3.png")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_IMG_Directory,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_IMG_Directory)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_IMG_Settings,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, resourcesDirectory, FormControls_IMG_Settings)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + "Lyre.ico",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "Lyre.ico"),
                Path.Combine(LyreUpdaterLocation, "Lyre.ico")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://www.newtonsoft.com/json",
            resourcesWebsiteURL + "Newtonsoft.Json.dll",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "Newtonsoft.Json.dll"),
                Path.Combine(LyreUpdaterLocation, "Newtonsoft.Json.dll")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://www.newtonsoft.com/json",
            resourcesWebsiteURL + "Newtonsoft.Json.xml",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "Newtonsoft.Json.xml"),
                Path.Combine(LyreUpdaterLocation, "Newtonsoft.Json.xml")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "policy.2.0.taglib-sharp.config",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "policy.2.0.taglib-sharp.config")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "policy.2.0.taglib-sharp.dll",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "policy.2.0.taglib-sharp.dll")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "taglib-sharp.dll",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "taglib-sharp.dll")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "https://rg3.github.io/youtube-dl/",
            resourcesWebsiteURL + "youtube-dl.exe",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "youtube-dl.exe")
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + LyreUpdaterString,
            new List<string>
            {
                Path.Combine(LyreUpdaterLocation, LyreUpdaterString)
            },
            false,
            false,
            1
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + "LyreLibrary.dll",
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, "LyreLibrary.dll"),
                Path.Combine(LyreUpdaterLocation, "LyreLibrary.dll")
            },
            false,
            false,
            1
        ),
    };

    public static List<OnlineResource> resourcesListUpdater = new List<OnlineResource>()
    {
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + LyreDownloaderString,
            new List<string>
            {
                Path.Combine(LyreDownloaderLocation, LyreDownloaderString),
            },
            false,
            false,
            1
        )
    };
}
