using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class OnlineResource
{
    public string credit; // who to credit - can be a name or a link to their site
    public string url; // location of resource file online
    public string path; // where to write the resource
    public bool askForPermission; // wait for users' approval to download // TO DO
    public bool waitForUser; // wait for user to close a finished download // TO DO

    public OnlineResource(string credit, string url, string path, bool askForPermission, bool waitForUser)
    {
        this.credit = credit;
        this.url = url;
        this.path = path;
        this.askForPermission = askForPermission;
        this.waitForUser = waitForUser;
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

    // contains all resource and dependency links
    public static readonly List<OnlineResource> resourcesList = new List<OnlineResource>()
    {
        new OnlineResource
        (
            "https://www.ffmpeg.org/",
            resourcesWebsiteURL + "ffmpeg.exe",
            Path.Combine("ffmpeg.exe"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://www.ffmpeg.org/",
            resourcesWebsiteURL + "ffprobe.exe",
            Path.Combine("ffprobe.exe"),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_CloseBig,
            Path.Combine(resourcesDirectory, FormControls_CloseBig),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_CloseSmall,
            Path.Combine(resourcesDirectory, FormControls_CloseSmall),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_Maximize,
            Path.Combine(resourcesDirectory, FormControls_Maximize),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_Minimize,
            Path.Combine(resourcesDirectory, FormControls_Minimize),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + "Icon3.png",
            Path.Combine(resourcesDirectory, "Icon3.png"),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_IMG_Directory,
            Path.Combine(resourcesDirectory, FormControls_IMG_Directory),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + FormControls_IMG_Settings,
            Path.Combine(resourcesDirectory, FormControls_IMG_Settings),
            false,
            false
        ),
        new OnlineResource
        (
            "Robert Barachini",
            resourcesWebsiteURL + "Lyre.ico",
            Path.Combine("Lyre.ico"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://www.newtonsoft.com/json",
            resourcesWebsiteURL + "Newtonsoft.Json.dll",
            Path.Combine("Newtonsoft.Json.dll"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://www.newtonsoft.com/json",
            resourcesWebsiteURL + "Newtonsoft.Json.xml",
            Path.Combine("Newtonsoft.Json.xml"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "policy.2.0.taglib-sharp.config",
            Path.Combine("policy.2.0.taglib-sharp.config"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "policy.2.0.taglib-sharp.dll",
            Path.Combine("policy.2.0.taglib-sharp.dll"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://github.com/mono/taglib-sharp",
            resourcesWebsiteURL + "taglib-sharp.dll",
            Path.Combine("taglib-sharp.dll"),
            false,
            false
        ),
        new OnlineResource
        (
            "https://rg3.github.io/youtube-dl/",
            resourcesWebsiteURL + "youtube-dl.exe",
            Path.Combine("youtube-dl.exe"),
            false,
            false
        )
    };
}
