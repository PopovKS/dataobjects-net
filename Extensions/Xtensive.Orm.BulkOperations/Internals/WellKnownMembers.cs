using System;
using System.Linq;
using System.Reflection;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.BulkOperations
{
  internal static class WellKnownMembers
  {
    public static readonly Type IncludeAlgorithmType = typeof(IncludeAlgorithm);
    public static readonly Type QueryableExtensionsType = typeof(QueryableExtensions);
    public const string InMethodName = nameof(QueryableExtensions.In);

    public static readonly MethodInfo TranslateQueryMethod =
      typeof(QueryBuilder).GetMethod(nameof(QueryBuilder.TranslateQuery));

    public static readonly MethodInfo InMethod = GetInMethod();

    private static MethodInfo GetInMethod()
    {
      foreach (var method in QueryableExtensionsType.GetMethods().Where(a => a.Name == InMethodName)) {
        var parameters = method.GetParameters();
        if (parameters.Length == 3 && parameters[2].ParameterType.Name == "IEnumerable`1") {
          return method;
        }
      }

      return null;
    }
  }
}