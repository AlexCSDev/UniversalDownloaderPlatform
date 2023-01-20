﻿namespace UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's request object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public interface IWebRequest
    {
        string Url { get; }
    }
}
