using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Models;

public class TaskParameter
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public string DefaultValue { get; set; } = string.Empty;
}