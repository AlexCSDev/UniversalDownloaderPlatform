using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.MegaDownloader
{
    internal static class MegaUrlDataExtractor
    {
        private readonly static Regex _newFormatRegex;
        private readonly static Regex _oldFormatRegex;

        static MegaUrlDataExtractor()
        {
            _newFormatRegex = new Regex(@"/(?<type>(file|folder))/(?<id>[^# ]+)(#(?<key>[a-zA-Z0-9_-]+))?");//Regex("(#F|#)![a-zA-Z0-9]{0,8}![a-zA-Z0-9_-]+");
            _oldFormatRegex = new Regex(@"#(?<type>F?)!(?<id>[^!]+)(!(?<key>[^$!\?<']+))?");
        }

        /// <summary>
        /// Extract data from mega url
        /// </summary>
        /// <param name="url"></param>
        /// <returns>type, id, key</returns>
        public static (string, string, string) Extract(string url)
        {
            Match match = _newFormatRegex.Match(url);
            if (!match.Success)
                match = _oldFormatRegex.Match(url);

            if(!match.Success)
                throw new ArgumentException("Unable to match supplied url");

            return (match.Groups["type"].Value, match.Groups["id"].Value, match.Groups["key"].Value);
        }
    }
}
