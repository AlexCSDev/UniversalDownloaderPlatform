using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Models
{
    public class UniversalDownloaderPlatformSettings : IUniversalDownloaderPlatformSettings
    {
        private bool _overwriteFiles;
        private bool _consumed;

        public bool OverwriteFiles
        {
            get => _overwriteFiles;
            set => ConsumableSetter.Set(Consumed, ref _overwriteFiles, value);
        }

        public bool Consumed
        {
            get => _consumed;
            set => ConsumableSetter.Set(Consumed, ref _consumed, value);
        }
    }
}
