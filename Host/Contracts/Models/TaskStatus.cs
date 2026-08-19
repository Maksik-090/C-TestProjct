using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Models
{
    internal enum TaskStatus
    {
            Pending,
            Running,
            Completed,
            Failed,
            Cancelled
    }
}
