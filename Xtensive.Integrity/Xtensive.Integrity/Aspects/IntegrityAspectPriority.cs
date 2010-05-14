// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2007.12.17

namespace Xtensive.Integrity.Aspects
{
  public enum IntegrityAspectPriority
  {
    InconsistentRegion = -99100,
    PropertyConstraint = -99050,
    Atomic             = -99000
  }
}