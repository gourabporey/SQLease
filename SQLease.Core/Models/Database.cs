namespace SQLease.Core.Models;

public class Database
{
    private readonly Dictionary<string, Table> _tables = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, Table> Tables => _tables;

    public void CreateTable(string tableName, Dictionary<string, Type> columns)
    {
        // Check if any table with the same name exists
        if (_tables.ContainsKey(tableName))
            throw new InvalidOperationException($"Table {tableName} already exists");
        
        // Create the table
        var table = new Table(tableName);
        
        // for every column insert the value
        foreach (var column in columns)
        {
            table.AddColumn(column.Key, column.Value);
        }
        
        // Add the table against the table name
        _tables.Add(tableName, table);
    }

    public Table GetTable(string tableName)
    {
        if (!_tables.TryGetValue(tableName, out var table))
            throw new InvalidOperationException($"Table {tableName} does not exist");
        
        return table;
    }
    
    public bool TableExists(string tableName) => _tables.ContainsKey(tableName);
}