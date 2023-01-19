﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.Engine.Helpers;

namespace UniversalDownloaderPlatform.Engine
{
    internal sealed class PluginManager : IPluginManager
    {
        private static string _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly List<IPlugin> _plugins;
        private readonly IPlugin _defaultPlugin;

        public PluginManager(IPlugin defaultPlugin, IDependencyResolver dependencyResolver)
        {
            _defaultPlugin = defaultPlugin;

            if (!Directory.Exists(_pluginsDirectory))
                Directory.CreateDirectory(_pluginsDirectory);

            _plugins = new List<IPlugin>();
            IEnumerable<string> files = Directory.EnumerateFiles(_pluginsDirectory);
            foreach (string file in files)
            {
                try
                {
                    if (!file.EndsWith(".dll"))
                        continue;

                    string filename = Path.GetFileName(file);

                    Assembly assembly = Assembly.LoadFrom(file);

                    Type[] types = assembly.GetTypes();

                    Type pluginType = types.SingleOrDefault(x => x.GetInterfaces().Contains(typeof(IPlugin)));
                    if (pluginType == null)
                        continue;

                    _logger.Debug($"New plugin found: {filename}");

                    IPlugin plugin = Activator.CreateInstance(pluginType) as IPlugin;
                    if (plugin == null)
                    {
                        _logger.Error($"Invalid plugin {filename}: IPlugin interface could not be created");
                        continue;
                    }

                    plugin.OnLoad(dependencyResolver);

                    _plugins.Add(plugin);
                    
                    _logger.Info(
                        $"Loaded plugin: {plugin.Name}"); // {assembly.GetName().Version} by {plugin.Author} ({plugin.ContactInformation})
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to load plugin {file}: {ex}");
                }
            }
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            foreach (IPlugin plugin in _plugins)
            {
                await plugin.BeforeStart(settings);
            }

            await _defaultPlugin.BeforeStart(settings);
        }

        public async Task DownloadCrawledUrl(ICrawledUrl crawledUrl)
        {
            if(crawledUrl == null)
                throw new ArgumentNullException(nameof(crawledUrl));

            IPlugin downloadPlugin = _defaultPlugin;

            if (_plugins != null && _plugins.Count > 0)
            {
                foreach (IPlugin plugin in _plugins)
                {
                    if (await plugin.IsSupportedUrl(crawledUrl.Url))
                    {
                        downloadPlugin = plugin;
                        break;
                    }
                }
            }

            await downloadPlugin.Download(crawledUrl);
        }

        public async Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            if(string.IsNullOrWhiteSpace(htmlContents))
                return new List<string>();

            HashSet<string> retHashSet = new HashSet<string>();
            if (_plugins != null && _plugins.Count > 0)
            {
                foreach (IPlugin plugin in _plugins)
                {
                    List<string> pluginRetList = await plugin.ExtractSupportedUrls(htmlContents);
                    if (pluginRetList != null && pluginRetList.Count > 0)
                    {
                        foreach(string url in pluginRetList)
                            retHashSet.Add(url);
                    }
                }
            }

            List<string> defaultPluginRetList = await _defaultPlugin.ExtractSupportedUrls(htmlContents);
            if (defaultPluginRetList != null && defaultPluginRetList.Count > 0)
            {
                foreach (string url in defaultPluginRetList)
                {
                    if(!retHashSet.Contains(url))
                        retHashSet.Add(url);
                }
            }

            return retHashSet.ToList();
        }
    }
}
