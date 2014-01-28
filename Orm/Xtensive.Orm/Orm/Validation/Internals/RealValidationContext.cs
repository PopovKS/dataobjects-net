// Copyright (C) 2013 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2013.09.12

using System;
using System.Collections.Generic;
using System.Linq;
using Xtensive.Orm.Model;

namespace Xtensive.Orm.Validation
{
  internal sealed class RealValidationContext : ValidationContext
  {
    private Dictionary<Entity, EntityErrorInfo> entitiesToValidate;

    public override void Reset()
    {
      entitiesToValidate = null;
    }

    public override void Validate(Entity target)
    {
      var result = ValidateAndGetErrors(target);

      if (result.Count > 0)
        throw new ValidationFailedException(GetErrorMessage(ValidationReason.UserRequest)) {
          ValidationErrors = new List<EntityErrorInfo> {new EntityErrorInfo(target, result)}
        };
    }

    public override void ValidateSetAttempt(Entity target, FieldInfo field, object value)
    {
      if (!field.HasImmediateValidators)
        return;

      foreach (var validator in field.Validators.Where(v => v.IsImmediate)) {
        var result = validator.Validate(target, value);
        if (result.IsError)
          throw new ArgumentException(result.ErrorMessage, "value");
      }
    }

    public override void RegisterForValidation(Entity target)
    {
      RegisterForValidation(target, null);
    }

    public override void Validate(ValidationReason reason)
    {
      var errors = ValidateAndGetErrors();
      if (errors.Count > 0)
        throw new ValidationFailedException(GetErrorMessage(reason)) {ValidationErrors = errors};
    }

    public override IList<ValidationResult> ValidateAndGetErrors(Entity target)
    {
      var result = new List<ValidationResult>();
      GetValidationErrors(target, result);
      if (result.Count==0)
        return EmptyValidationResultCollection;
      var lockedResult = result.AsReadOnly();
      var errorInfo = new EntityErrorInfo(target, lockedResult);
      RegisterForValidation(target, errorInfo);
      return lockedResult;
    }

    public override IList<ValidationResult> ValidateOnceAndGetErrors(Entity target)
    {
      EntityErrorInfo errorInfo;
      if (entitiesToValidate!=null && entitiesToValidate.TryGetValue(target, out errorInfo) && errorInfo!=null)
        return errorInfo.Errors;
      return ValidateAndGetErrors(target);
    }

    public override IList<EntityErrorInfo> ValidateAndGetErrors()
    {
      var errors = new List<EntityErrorInfo>();
      GetValidationErrors(errors);
      return errors.Count==0 ? EmptyEntityErrorCollection : errors.AsReadOnly();
    }

    private void RegisterForValidation(Entity target, EntityErrorInfo previousStatus)
    {
      if (!target.TypeInfo.HasValidators)
        return;

      if (entitiesToValidate==null)
        entitiesToValidate = new Dictionary<Entity, EntityErrorInfo>();
      entitiesToValidate[target] = previousStatus;
    }

    private void GetValidationErrors(List<EntityErrorInfo> output)
    {
      if (entitiesToValidate==null)
        return;

      var currentEntityErrors = new List<ValidationResult>();
      var entitiesToProcess = entitiesToValidate;
      entitiesToValidate = null;

      foreach (var entity in entitiesToProcess.Keys)
        if (entity.CanBeValidated) {
          GetValidationErrors(entity, currentEntityErrors);
          if (currentEntityErrors.Count > 0) {
            var errorInfo = new EntityErrorInfo(entity, currentEntityErrors.AsReadOnly());
            RegisterForValidation(entity, errorInfo);
            output.Add(errorInfo);
            currentEntityErrors = new List<ValidationResult>();
          }
        }
    }

    private void GetValidationErrors(Entity target, List<ValidationResult> output)
    {
      foreach (var field in target.TypeInfo.Fields) {
        if (!field.HasValidators)
          continue;
        var value = target.GetFieldValue(field);
        foreach (var validator in field.Validators) {
          var result = validator.Validate(target, value);
          if (result.IsError)
            output.Add(result);
        }
      }

      foreach (var validator in target.TypeInfo.Validators) {
        var result = validator.Validate(target);
        if (result.IsError)
          output.Add(result);
      }
    }

    private string GetErrorMessage(ValidationReason reason)
    {
      switch (reason) {
      case ValidationReason.UserRequest:
        return Strings.ExValidationFailed;
      case ValidationReason.Commit:
        return Strings.ExCanNotCommitATransactionEntitiesValidationFailed;
      default:
        throw new ArgumentOutOfRangeException("reason");
      }
    }
  }
}