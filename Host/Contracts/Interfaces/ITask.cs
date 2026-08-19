using System;
using System.Collections.Generic;
using System.Text;
using Contracts.Models;

namespace Contracts.Interfaces;

public interface ITask
{
    string Id { get; }
    string Name { get; }
    string Description { get; }

    IReadOnlyList<TaskParameter> Parameters { get; }

    Task<TaskResult> ExecuteAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken,
        IProgress<double>? progress = null,
        IProgress<string>? log = null);
}