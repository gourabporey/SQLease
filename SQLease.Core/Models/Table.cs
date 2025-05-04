namespace SQLease.Core.Models;

public class Table(string name)
{
    public string Name { get; set; } = name;
    private List<Column> Columns { get; set; } = [];
    public List<Row> Rows { get; set; } = [];

    public void AddColumn(string columnName, Type dataType)
    {
        if (Columns.Any(c => c.Name == columnName))
            throw new InvalidOperationException($"Column {columnName} already exists");

        Columns.Add(new Column(columnName, dataType));
    }

    public void InsertRow(Dictionary<string, object?> rowData)
    {
        foreach (var column in Columns)
        {
            if (!rowData.TryGetValue(column.Name, out var value))
                throw new InvalidOperationException($"Column {column.Name} does not exist");
            
            if (column.DataType != value?.GetType())
                throw new InvalidOperationException($"Column {column.Name} data type does not match");
        }
        
        Rows.Add(new Row(rowData));
    }
}