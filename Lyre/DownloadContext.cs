using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lyre
{
    public class DownloadContext
    {
        public string url;
        public bool canConvert;
        public int audioQualitySelector;
        public int videoQualitySelector;
        public int framerateSelector;
        public Website website;

        public DownloadContext(string url, bool canConvert, int audioQualitySelector, int videoQualitySelector, int framerateSelector)
        {
            this.url = url;
            this.canConvert = canConvert;
            this.audioQualitySelector = audioQualitySelector;
            this.videoQualitySelector = videoQualitySelector;
            this.framerateSelector = framerateSelector;
            this.website = Website.Youtube;
        }

        public DownloadContext(string url, bool canConvert, int audioQualitySelector, int videoQualitySelector, int framerateSelector, Website website)
        {
            this.url = url;
            this.canConvert = canConvert;
            this.audioQualitySelector = audioQualitySelector;
            this.videoQualitySelector = videoQualitySelector;
            this.framerateSelector = framerateSelector;
            this.website = website;
        }

        public enum Website
        {
            Youtube,
            Soundcloud
        };
    }
}
