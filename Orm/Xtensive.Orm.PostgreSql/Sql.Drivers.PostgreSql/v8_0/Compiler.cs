// Copyright (C) 2003-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Linq;
using Xtensive.Orm.Providers.PostgreSql;
using Xtensive.Sql.Compiler;
using Xtensive.Sql.Dml;
using SqlCompiler = Xtensive.Sql.Compiler.SqlCompiler;

namespace Xtensive.Sql.Drivers.PostgreSql.v8_0
{
  internal class Compiler : SqlCompiler
  {
    private static readonly SqlNative OneYearInterval = SqlDml.Native("interval '1 year'");
    private static readonly SqlNative OneMonthInterval = SqlDml.Native("interval '1 month'");
    private static readonly SqlNative OneDayInterval = SqlDml.Native("interval '1 day'");
    private static readonly SqlNative OneMinuteInterval = SqlDml.Native("interval '1 minute'");
    private static readonly SqlNative OneSecondInterval = SqlDml.Native("interval '1 second'");

    public override void Visit(SqlDeclareCursor node)
    {
    }

    public override void Visit(SqlOpenCursor node)
    {
      base.Visit(node.Cursor.Declare());
    }

    public override void Visit(SqlBinary node)
    {
      var right = node.Right as SqlArray;
      if (!right.IsNullReference() && (node.NodeType==SqlNodeType.In || node.NodeType==SqlNodeType.NotIn)) {
        var row = SqlDml.Row(right.GetValues().Select(value => SqlDml.Literal(value)).ToArray());
        base.Visit(node.NodeType==SqlNodeType.In ? SqlDml.In(node.Left, row) : SqlDml.NotIn(node.Left, row));
      }
      else {
        switch (node.NodeType) {
          case SqlNodeType.DateTimeOffsetMinusDateTimeOffset:
            (node.Left - node.Right).AcceptVisitor(this);
            return;
          case SqlNodeType.DateTimeOffsetMinusInterval:
            (node.Left - node.Right).AcceptVisitor(this);
            return;
          case SqlNodeType.DateTimeOffsetPlusInterval:
            (node.Left + node.Right).AcceptVisitor(this);
            return;
        }
        base.Visit(node);
      }
    }

    public override void Visit(SqlFunctionCall node)
    {
      const double nanosecondsPerSecond = 1000000000.0;

      switch (node.FunctionType) {
      case SqlFunctionType.PadLeft:
      case SqlFunctionType.PadRight:
        SqlHelper.GenericPad(node).AcceptVisitor(this);
        return;
      case SqlFunctionType.Rand:
        SqlDml.FunctionCall(translator.Translate(SqlFunctionType.Rand)).AcceptVisitor(this);
        return;
      case SqlFunctionType.Square:
        SqlDml.Power(node.Arguments[0], 2).AcceptVisitor(this);
        return;
      case SqlFunctionType.IntervalConstruct:
        ((node.Arguments[0] / SqlDml.Literal(nanosecondsPerSecond)) * OneSecondInterval).AcceptVisitor(this);
        return;
      case SqlFunctionType.IntervalToMilliseconds:
        SqlHelper.IntervalToMilliseconds(node.Arguments[0]).AcceptVisitor(this);
        return;
      case SqlFunctionType.IntervalToNanoseconds:
        SqlHelper.IntervalToNanoseconds(node.Arguments[0]).AcceptVisitor(this);
        return;
      case SqlFunctionType.IntervalAbs:
        SqlHelper.IntervalAbs(node.Arguments[0]).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeConstruct:
        var newNode = (SqlDml.Literal(new DateTime(2001, 1, 1))
                       + OneYearInterval * (node.Arguments[0] - 2001)
                       + OneMonthInterval * (node.Arguments[1] - 1)
                       + OneDayInterval * (node.Arguments[2] - 1));
        newNode.AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeTruncate:
        (SqlDml.FunctionCall("date_trunc", "day", node.Arguments[0])).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeAddMonths:
        (node.Arguments[0] + node.Arguments[1] * OneMonthInterval).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeAddYears:
        (node.Arguments[0] + node.Arguments[1] * OneYearInterval).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeToStringIso:
        DateTimeToStringIso(node.Arguments[0]).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeOffsetTimeOfDay:
        DateTimeOffsetTimeOfDay(node.Arguments[0]).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeOffsetAddMonths:
        SqlDml.Cast(node.Arguments[0] + node.Arguments[1] * OneMonthInterval, SqlType.DateTimeOffset).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeOffsetAddYears:
        SqlDml.Cast(node.Arguments[0] + node.Arguments[1] * OneYearInterval, SqlType.DateTimeOffset).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeOffsetConstruct:
        ConstructDateTimeOffset(node.Arguments[0], node.Arguments[1]).AcceptVisitor(this);
        return;
      case SqlFunctionType.DateTimeToDateTimeOffset:
        SqlDml.Cast(node.Arguments[0], SqlType.DateTimeOffset).AcceptVisitor(this);
        return;
      }
      base.Visit(node);
    }

    public override void Visit(SqlCustomFunctionCall node)
    {
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPointExtractX) {
        NpgsqlPointExtractPart(node.Arguments[0], 0).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPointExtractY) {
        NpgsqlPointExtractPart(node.Arguments[0], 1).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlTypeExtractPoint) {
        NpgsqlTypeExtractPoint(node.Arguments[0], node.Arguments[1]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlBoxExtractHeight) {
        NpgsqlBoxExtractHeight(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlBoxExtractWidth) {
        NpgsqlBoxExtractWidth(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlCircleExtractCenter) {
        NpgsqlCircleExtractCenter(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlCircleExtractRadius) {
        NpgsqlCircleExtractRadius(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPathAndPolygonCount) {
        NpgsqlPathAndPolygonCount(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPathAndPolygonOpen) {
        NpgsqlPathAndPolygonOpen(node.Arguments[0]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPathAndPolygonContains) {
        NpgsqlPathAndPolygonContains(node.Arguments[0], node.Arguments[1]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlTypeOperatorEquality) {
        NpgsqlTypeOperatorEquality(node.Arguments[0], node.Arguments[1]).AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlPointConstructor) {
        var newNode = SqlDml.RawConcat(
          NpgsqlTypeConstructor(node.Arguments[0], node.Arguments[1], "point'"),
          SqlDml.Native("'"));
        newNode.AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlBoxConstructor) {
        NpgsqlTypeConstructor(node.Arguments[0], node.Arguments[1], "box").AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlCircleConstructor) {
        NpgsqlTypeConstructor(node.Arguments[0], node.Arguments[1], "circle").AcceptVisitor(this);
        return;
      }
      if (node.FunctionType==PostgresqlSqlFunctionType.NpgsqlLSegConstructor) {
        NpgsqlTypeConstructor(node.Arguments[0], node.Arguments[1], "lseg").AcceptVisitor(this);
        return;
      }
      base.Visit(node);
    }


    private static SqlExpression DateTimeToStringIso(SqlExpression dateTime)
    {
      return SqlDml.FunctionCall("To_Char", dateTime, "YYYY-MM-DD\"T\"HH24:MI:SS");
    }

    private static SqlExpression IntervalToIsoString(SqlExpression interval, bool signed)
    {
      if (!signed)
        return SqlDml.FunctionCall("TO_CHAR", interval, "HH24:MI");
      var hours = SqlDml.FunctionCall("TO_CHAR", SqlDml.Extract(SqlIntervalPart.Hour, interval), "SG09");
      var minutes = SqlDml.FunctionCall("TO_CHAR", SqlDml.Extract(SqlIntervalPart.Minute, interval), "FM09");
      return SqlDml.Concat(hours, ":", minutes);
    }

    protected static SqlExpression NpgsqlPointExtractPart(SqlExpression expression, int part)
    {
      return SqlDml.RawConcat(expression, SqlDml.Native($"[{part}]"));
    }

    protected static SqlExpression NpgsqlTypeExtractPoint(SqlExpression expression, SqlExpression numberPoint)
    {
      var numberPointAsInt = numberPoint as SqlLiteral<int>;
      int valueNumberPoint = numberPointAsInt!=null ? numberPointAsInt.Value : 0;

      return SqlDml.RawConcat(
        SqlDml.Native("("),
        SqlDml.RawConcat(
          expression,
          SqlDml.Native($"[{valueNumberPoint}])")));
    }

    protected static SqlExpression NpgsqlBoxExtractHeight(SqlExpression expression)
    {
      return SqlDml.FunctionCall("HEIGHT", expression);
    }

    protected static SqlExpression NpgsqlBoxExtractWidth(SqlExpression expression)
    {
      return SqlDml.FunctionCall("WIDTH", expression);
    }

    protected static SqlExpression NpgsqlCircleExtractCenter(SqlExpression expression)
    {
      return SqlDml.RawConcat(SqlDml.Native("@@"), expression);
    }

    protected static SqlExpression NpgsqlCircleExtractRadius(SqlExpression expression)
    {
      return SqlDml.FunctionCall("RADIUS", expression);
    }

    protected static SqlExpression NpgsqlPathAndPolygonCount(SqlExpression expression)
    {
      return SqlDml.FunctionCall("NPOINTS", expression);
    }

    protected static SqlExpression NpgsqlPathAndPolygonOpen(SqlExpression expression)
    {
      return SqlDml.FunctionCall("ISOPEN", expression);
    }

    protected static SqlExpression NpgsqlPathAndPolygonContains(SqlExpression expression, SqlExpression point)
    {
      return SqlDml.RawConcat(
        expression,
        SqlDml.RawConcat(
          SqlDml.Native("@>"),
          point));
    }

    protected static SqlExpression NpgsqlTypeOperatorEquality(SqlExpression left, SqlExpression right)
    {
      return SqlDml.RawConcat(left,
        SqlDml.RawConcat(
          SqlDml.Native("~="),
          right));
    }

    private static SqlExpression NpgsqlTypeConstructor(SqlExpression left, SqlExpression right, string type)
    {
      return SqlDml.RawConcat(
        SqlDml.Native($"{type}("),
        SqlDml.RawConcat(left,
          SqlDml.RawConcat(
            SqlDml.Native(","),
            SqlDml.RawConcat(
              right,
              SqlDml.Native(")")))));
    }

    public override void Visit(SqlExtract node)
    {   
      switch (node.DateTimeOffsetPart) {
        case SqlDateTimeOffsetPart.Date:
          DateTimeOffsetExtractDate(node.Operand).AcceptVisitor(this);
          return;
        case SqlDateTimeOffsetPart.DateTime:
          DateTimeOffsetExtractDateTime(node.Operand).AcceptVisitor(this);
          return;

        case SqlDateTimeOffsetPart.UtcDateTime:
          DateTimeOffsetToUtcDateTime(node.Operand).AcceptVisitor(this);
          return;
        case SqlDateTimeOffsetPart.LocalDateTime:
          DateTimeOffsetToLocalDateTime(node.Operand).AcceptVisitor(this);
          return;
        case SqlDateTimeOffsetPart.Offset:
          DateTimeOffsetExtractOffset(node);
          return;
      }
      base.Visit(node);
    }

    
    protected SqlExpression DateTimeOffsetExtractDate(SqlExpression timestamp)
    {
      return SqlDml.FunctionCall("DATE", timestamp);
    }

    protected SqlExpression DateTimeOffsetExtractDateTime(SqlExpression timestamp)
    {
      return SqlDml.Cast(timestamp, SqlType.DateTime);
    }

    protected SqlExpression DateTimeOffsetToUtcDateTime(SqlExpression timeStamp)
    {
      return GetDateTimeInTimeZone(timeStamp, TimeSpan.Zero);
    }
    
    protected SqlExpression DateTimeOffsetToLocalDateTime(SqlExpression timestamp)
    {
      return SqlDml.Cast(timestamp, SqlType.DateTime);
    }

    protected void DateTimeOffsetExtractOffset(SqlExtract node)
    {
      using (context.EnterScope(node)) {
        context.Output.AppendText(translator.Translate(context, node, ExtractSection.Entry));
        var part = node.DateTimePart!=SqlDateTimePart.Nothing
          ? translator.Translate(node.DateTimePart)
          : node.IntervalPart!=SqlIntervalPart.Nothing
            ? translator.Translate(node.IntervalPart)
            : translator.Translate(node.DateTimeOffsetPart);
        context.Output.AppendText(part);
        context.Output.AppendText(translator.Translate(context, node, ExtractSection.From));
        node.Operand.AcceptVisitor(this);
        context.Output.AppendText(translator.Translate(context, node, ExtractSection.Exit));
        context.Output.AppendText(translator.Translate(SqlNodeType.Multiply));
        OneSecondInterval.AcceptVisitor(this);
      }
    }

    protected SqlExpression DateTimeOffsetTimeOfDay(SqlExpression timestamp)
    {
      return DateTimeOffsetSubstract(timestamp, SqlDml.DateTimeTruncate(timestamp));
    }

    protected SqlExpression DateTimeOffsetSubstract(SqlExpression timestamp1, SqlExpression timestamp2)
    {
      return timestamp1 - timestamp2;
    }

    protected SqlExpression ConstructDateTimeOffset(SqlExpression dateTimeExpression, SqlExpression offsetInMinutes)
    {
      var dateTimeAsStringExpression = GetDateTimeAsStringExpression(dateTimeExpression);
      var offsetAsStringExpression = GetOffsetAsStringExpression(offsetInMinutes);
      return ConstructDateTimeOffsetFromExpressions(dateTimeAsStringExpression, offsetAsStringExpression);
    }

    protected SqlExpression ConstructDateTimeOffsetFromExpressions(SqlExpression datetimeStringExpression, SqlExpression offsetStringExpression)
    {
      return SqlDml.Cast(SqlDml.Concat(datetimeStringExpression, " ", offsetStringExpression), SqlType.DateTimeOffset);
    }

    protected SqlExpression GetDateTimeAsStringExpression(SqlExpression dateTimeExpression)
    {
      return SqlDml.FunctionCall("To_Char", dateTimeExpression, "YYYY-MM-DD\"T\"HH24:MI:SS.MS");
    }

    protected SqlExpression GetOffsetAsStringExpression(SqlExpression offsetInMinutes)
    {
      int hours = 0;
      int minutes = 0;
      //if something simple as double or int or even timespan can be separated into hours and minutes parts
      if (TryDivideOffsetIntoParts(offsetInMinutes, ref hours, ref minutes))
        return SqlDml.Native($"'{ZoneStringFromParts(hours, minutes)}'");

      var intervalExpression = offsetInMinutes * OneMinuteInterval;
      return IntervalToIsoString(intervalExpression, true);
    }

    private string ZoneStringFromParts(int hours, int minutes)
    {
      return $"{(hours < 0 ? "-" : "+")}{Math.Abs(hours):00}:{Math.Abs(minutes):00}";
    }

    private SqlExpression GetDateTimeInTimeZone(SqlExpression expression, SqlExpression zone)
    {
      return SqlDml.FunctionCall("TIMEZONE", zone, expression);
    }

    private SqlExpression GetServerTimeZone()
    {
      return SqlDml.FunctionCall("CURRENT_SETTING", SqlDml.Literal("TIMEZONE"));
    }

    private bool TryDivideOffsetIntoParts(SqlExpression offsetInMinutes, ref int hours , ref int minutes)
    {
      var offsetToDouble = offsetInMinutes as SqlLiteral<double>;
      if (offsetToDouble!=null) {
        hours = (int) offsetToDouble.Value / 60;
        minutes = Math.Abs((int) offsetToDouble.Value % 60);
        return true;
      }
      var offsetToInt = offsetInMinutes as SqlLiteral<int>;
      if (offsetToInt!=null) {
        hours = offsetToInt.Value / 60;
        minutes = Math.Abs(offsetToInt.Value % 60);
        return true;
      }
      var offsetToTimeSpan = offsetInMinutes as SqlLiteral<TimeSpan>;
      if (offsetToTimeSpan!=null) {
        var totalMinutes = offsetToTimeSpan.Value.TotalMinutes;
        hours = (int)totalMinutes / 60;
        minutes = Math.Abs((int)totalMinutes % 60);
        return true;
      }
      return false;
    }

    // Constructors

    protected internal Compiler(SqlDriver driver)
      : base(driver)
    {
    }
  }
}