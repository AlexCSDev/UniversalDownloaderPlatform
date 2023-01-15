using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.Common.Helpers
{
    internal static class FileHashHelper
    {
        /// <summary>
        /// Calculated MD5 hash of the file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Byte array containing the hash</returns>
        public static byte[] CalculateFileHash(string path)
        {
            using (var inputStream = File.Open(path, FileMode.Open))
            {
                var md5 = MD5.Create();
                return md5.ComputeHash(inputStream);
            }
        }
    }
}
