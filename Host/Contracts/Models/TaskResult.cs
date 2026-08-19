using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Models;
public class TaskResult
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }
}