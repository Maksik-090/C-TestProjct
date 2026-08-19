using System;
using System.Collections.Generic;
using System.Text;
using Contracts.Interfaces;
using Plugins.Command.Tasks;

namespace Plugins.Command.Plugin;

public class CommandPlugin : IPlugin
{
    public string Name => "Command Plugin";
    public string Version => "1.0.0";
    public IReadOnlyList<ITask> GetTasks()
    {
        return new List<ITask>
        {
            new ShellCommandTask()
        };
    }
}