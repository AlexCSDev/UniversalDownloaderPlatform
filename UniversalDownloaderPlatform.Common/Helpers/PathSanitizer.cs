using System;
using System.IO;
using System.Linq;

namespace UniversalDownloaderPlatform.Common.Helpers
{
    public class PathSanitizer
    {
        private static readonly char[] _invalidPathCharacters;

        static PathSanitizer()
        {
            _invalidPathCharacters = Path.GetInvalidPathChars().Concat("\\/:*?\"<>|".ToCharArray()).ToArray();
        }

        public static string SanitizePath(string path)
        {
            foreach (char c in _invalidPathCharacters)
            {
                path = path.Replace(c, '_');
            }

            return path;
        }
    }
}
