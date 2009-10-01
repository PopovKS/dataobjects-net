﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xtensive.Storage;

namespace Test.Pilot.Kernel.ProcessingWorkflowModel
{
  public class ErrorDocument : Document
  {
    [Field]
    public string LinkedMessage { get; set; }
    
    [Field]
    public Document LinkedDocument { get; set; }

    [Field]
    public Processor ExecutableHavingCausedTheError { get; set; }

    [Field]
    public Container OriginalContainer { get; set; }

    [Field]
    public DateTime? RetryAfter { get; set; }

    public DateTime? GetDateOfFirstSimilarError()
    {
      return null;
    }
  }
}
