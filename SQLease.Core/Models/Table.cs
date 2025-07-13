using Microsoft.Data.Analysis;
using SQLease.Core.Storage;

namespace SQLease.Core.Models;

public class Table(string name)
{
    public string Name { get; } = name;
    private List<Column> Columns { get; } = [];
    private readonly ITableStorage _storage = new DataFrameTableStorage();

    public void AddColumn(string name, Type type)
    {
        Columns.Add(new Column(name, type));
        _storage.AddColumn(name, type);
    }

    public void AddDeletedColumn(string columnName)
    {
        _storage.AddDeletedColumn(columnName);
    }

    public void InsertRow(Dictionary<string, object?> row) => _storage.InsertRow(row);

    public void PrintAllRows()
    {
        foreach (var row in _storage.GetAllRows())
        {
            Console.WriteLine(string.Join(", ", row.Select(kv => $"{kv.Key}: {kv.Value}")));
        }
    }

    public int UpdateRows(Func<Dictionary<string, object?>, bool> predicate, Dictionary<string, object?> updates)
        => _storage.UpdateRows(predicate, updates);

    public int DeleteRows(Func<Dictionary<string, object?>, bool> predicate)
        => _storage.DeleteRows(predicate);

    public void Compact() => _storage.Compact();
    
    public IReadOnlyList<Column> GetColumns() => Columns;
}
