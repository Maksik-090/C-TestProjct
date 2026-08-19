using System;
using System.Collections.Generic;
using System.Text;
using Contracts.Interfaces;

namespace Host.Models;

public class TaskItem
{
    public ITask Task { get; }
    public IPlugin Plugin { get; }
    public string Name => Task.Name;
    public string Description => Task.Description;
    public string PluginName => Plugin.Name;
    public TaskItem(ITask task, IPlugin plugin)
    {
        Task = task;
        Plugin = plugin;
    }
}
