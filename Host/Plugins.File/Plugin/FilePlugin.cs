using System;
using System.Collections.Generic;
using System.Text;
using Contracts.Interfaces;
using Plugins.File.Tasks;



namespace Plugins.File.Plugin;

public class FilePlugin : IPlugin
{
    public string Name => "File Plugin";
    public string Version => "1.0.0";
    public IReadOnlyList<ITask> GetTasks()
    {
        return new List<ITask>
        {
            new FileSearchTask(),
            new BinarySearchTask()
        };
    }
}