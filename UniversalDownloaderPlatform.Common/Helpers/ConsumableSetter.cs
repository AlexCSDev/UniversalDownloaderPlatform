// From: https://github.com/commandlineparser/commandline/blob/master/src/CommandLine/Infrastructure/PopsicleSetter.cs

using System;

namespace UniversalDownloaderPlatform.Common.Helpers
{
    public static class ConsumableSetter
    {
        public static void Set<T>(bool consumed, ref T field, T value)
        {
            if (consumed)
            {
                throw new InvalidOperationException();
            }

            field = value;
        }
    }
}