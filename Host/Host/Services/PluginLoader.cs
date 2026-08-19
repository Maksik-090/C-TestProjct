using Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Host.Services;

public class PluginLoader
{
    public IReadOnlyList<IPlugin> LoadPlugins(string pluginsDirectory)
    {
        var plugins = new List<IPlugin>();
        if (!Directory.Exists(pluginsDirectory))
        {
            Directory.CreateDirectory(pluginsDirectory);
            return plugins;
        }

        var dllFiles = Directory.GetFiles(
            pluginsDirectory,
            "*.dll",
            SearchOption.TopDirectoryOnly);

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);

                var pluginTypes = assembly
                    .GetTypes()
                    .Where(type =>
                        typeof(IPlugin).IsAssignableFrom(type) &&
                        !type.IsInterface &&
                        !type.IsAbstract);

                foreach (var pluginType in pluginTypes)
                {
                    if (Activator.CreateInstance(pluginType) is IPlugin plugin)
                    {
                        plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Ошибка загрузки плагина {dllFile}: {ex.Message}");
            }
        }

        return plugins;
    }
}
