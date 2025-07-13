using Microsoft.Data.Analysis;

namespace SQLease.Core.Storage;

public class DataFrameTableStorage : ITableStorage
{
    private readonly Dictionary<string, Type> _schema = new();
    private DataFrame _df = new();
    private static string _deletedColumn = "__Deleted";

    public void AddColumn(string name, Type type)
    {
        if (_schema.ContainsKey(name))
            throw new InvalidOperationException($"Column {name} exists.");

        DataFrameColumn column = type switch
        {
            _ when type == typeof(int) => new Int32DataFrameColumn(name),
            _ when type == typeof(string) => new StringDataFrameColumn(name),
            _ when type == typeof(bool) => new BooleanDataFrameColumn(name),
            _ when type == typeof(double) => new DoubleDataFrameColumn(name),
            _ when type == typeof(DateTime) => new DateTimeDataFrameColumn(name),
            _ => throw new NotSupportedException($"Unsupported type: {type}")
        };

        _df.Columns.Add(column);
        _schema[name] = type;
    }

    public void AddDeletedColumn(string columnName)
    {
        if (_schema.ContainsKey(columnName))
        {
            throw new InvalidOperationException($"Column {columnName} already exists.");
        }
        _deletedColumn = columnName;
        _df.Columns.Add(new BooleanDataFrameColumn(columnName));
        _schema[columnName] = typeof(bool);
    }

    public void InsertRow(Dictionary<string, object?> rowValues)
    {
        var values = new List<object?>();

        foreach (var name in _schema.Keys.Where(k => k != _deletedColumn))
        {
            if (!rowValues.TryGetValue(name, out var val))
                throw new InvalidOperationException($"Missing value for {name}");

            values.Add(val);
        }

        values.Add(false); // __deleted = false
        _df.Append(values.ToArray(), true);
    }

    public IEnumerable<Dictionary<string, object?>> GetAllRows(bool includeDeleted = false)
    {
        foreach (var row in _df.Rows)
        {
            var dict = _df.Columns.ToDictionary(col => col.Name, col => row[col.Name]);
            if (!includeDeleted && Convert.ToBoolean(dict[_deletedColumn] ?? false))
                continue;

            dict.Remove(_deletedColumn);
            yield return dict;
        }
    }

    public int UpdateRows(Func<Dictionary<string, object?>, bool> predicate, Dictionary<string, object?> updates)
    {
        var updated = 0;
        for (var i = 0; i < _df.Rows.Count; i++)
        {
            var row = _df.Rows[i];
            if (Convert.ToBoolean(row[_deletedColumn]) == true) continue;

            var dict = _df.Columns.ToDictionary(col => col.Name, col => row[col.Name]);
            if (!predicate(dict)) continue;

            foreach (var update in updates)
                _df[update.Key][i] = update.Value;

            updated++;
        }
        return updated;
    }

    public int DeleteRows(Func<Dictionary<string, object?>, bool> predicate)
    {
        var deleted = 0;
        for (var i = 0; i < _df.Rows.Count; i++)
        {
            var row = _df.Rows[i];
            if (Convert.ToBoolean(row[_deletedColumn]) == true) continue;

            var dict = _df.Columns.ToDictionary(col => col.Name, col => row[col.Name]);
            if (!predicate(dict)) continue;

            _df[_deletedColumn][i] = true;
            deleted++;
        }
        return deleted;
    }

    public void Compact()
    {
        var kept = _df.Rows
            .Where(row => !Convert.ToBoolean(row[_deletedColumn] ?? false))
            .ToList();

        var newColumns = new List<DataFrameColumn>();

        foreach (var col in _df.Columns)
        {
            DataFrameColumn newCol = col.DataType switch
            {
                { } t when t == typeof(int) => new Int32DataFrameColumn(col.Name),
                { } t when t == typeof(string) => new StringDataFrameColumn(col.Name),
                { } t when t == typeof(bool) => new BooleanDataFrameColumn(col.Name),
                { } t when t == typeof(double) => new DoubleDataFrameColumn(col.Name),
                { } t when t == typeof(DateTime) => new DateTimeDataFrameColumn(col.Name),
                _ => throw new NotSupportedException($"Unsupported type: {col.DataType.Name}")
            };

            foreach (var value in kept.Select(row => row[col.Name]))
            {
                switch (newCol)
                {
                    case Int32DataFrameColumn i: i.Append(value is int vi ? vi : null); break;
                    case StringDataFrameColumn s: s.Append(value as string); break;
                    case BooleanDataFrameColumn b: b.Append(value is bool vb ? vb : null); break;
                    case DoubleDataFrameColumn d: d.Append(value is double vd ? vd : null); break;
                    case DateTimeDataFrameColumn dt: dt.Append(value is DateTime vdt ? vdt : null); break;
                    default: throw new NotSupportedException($"Unsupported column type: {col.GetType().Name}");
                }
            }

            newColumns.Add(newCol);
        }
        
        _df = new DataFrame(newColumns);
    }

}
