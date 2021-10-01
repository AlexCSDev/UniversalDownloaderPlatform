using System.IO;
using System.Linq;

namespace UniversalDownloaderPlatform.Common.Helpers
{
    public class UniversalDirectoryPatternFormat
    {
        public static readonly char[] InvalidPathChars;

        static UniversalDirectoryPatternFormat()
        {
            InvalidPathChars = Path.GetInvalidPathChars().Concat("\\/:*?\"<>|".ToCharArray()).ToArray();
        }
    }
}
