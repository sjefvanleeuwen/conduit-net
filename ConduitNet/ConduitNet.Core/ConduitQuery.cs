using MessagePack;
using System.Collections.Generic;

namespace ConduitNet.Core
{
    [MessagePackObject]
    public class ConduitQuery
    {
        [Key(0)]
        public List<QueryFilter> Filters { get; set; } = new();

        [Key(1)]
        public List<QuerySort> Sorts { get; set; } = new();

        [Key(2)]
        public int? Skip { get; set; }

        [Key(3)]
        public int? Take { get; set; }

        [Key(4)]
        public List<string> SelectFields { get; set; } = new();

        [Key(5)]
        public List<QueryInclude> Includes { get; set; } = new();

        [Key(6)]
        public List<string> GroupBy { get; set; } = new();

        [Key(7)]
        public List<QueryAggregate> Aggregates { get; set; } = new();

        [Key(8)]
        public Dictionary<string, string> CustomProperties { get; set; } = new();
    }

    [MessagePackObject]
    public class QueryFilter
    {
        [Key(0)]
        public string FieldName { get; set; } = string.Empty;

        [Key(1)]
        public FilterOperator Operator { get; set; }

        [Key(2)]
        public object? Value { get; set; }

        [Key(3)]
        public LogicOperator Logic { get; set; } = LogicOperator.And;

        [Key(4)]
        public List<QueryFilter> Group { get; set; } = new();
    }

    [MessagePackObject]
    public class QuerySort
    {
        [Key(0)]
        public string FieldName { get; set; } = string.Empty;

        [Key(1)]
        public bool IsDescending { get; set; }
    }

    [MessagePackObject]
    public class QueryInclude
    {
        [Key(0)]
        public string Path { get; set; } = string.Empty;

        [Key(1)]
        public ConduitQuery Filter { get; set; } = new();
    }

    [MessagePackObject]
    public class QueryAggregate
    {
        [Key(0)]
        public AggregateType Type { get; set; }

        [Key(1)]
        public string FieldName { get; set; } = string.Empty;

        [Key(2)]
        public string Alias { get; set; } = string.Empty;
    }

    public enum FilterOperator
    {
        Eq,
        Neq,
        Gt,
        Lt,
        Gte,
        Lte,
        Contains,
        StartsWith,
        EndsWith,
        In,
        Any,
        All
    }

    public enum LogicOperator
    {
        And,
        Or
    }

    public enum AggregateType
    {
        Count,
        Sum,
        Min,
        Max,
        Avg
    }
}
