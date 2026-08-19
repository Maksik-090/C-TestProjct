using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Interfaces;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    IReadOnlyList<ITask> GetTasks();
}
