using System;
using System.Collections.Generic;
using System.Text;

namespace Host.Models;

public class ExecutionItem
{
    public string TaskName { get; set; } = "";
    public string Status { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string Result { get; set; } = "";
    public double Progress { get; set; }
}