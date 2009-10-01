﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test.Pilot.Kernel.ProcessingWorkflowModel
{
  /// <summary>
  /// Allows specifying who shall see a particular history entry.
  /// <remarks>Consider also HistoryEntry.VisibilityForAll .</remarks>
  /// </summary>
  [Flags]
  [Serializable]
  public enum HistoryEntryVisibility : int
  {
    None = 0,
    EndUser = 1,
    AdministratorUser = 2,
    Debugger = 4
  }
}
