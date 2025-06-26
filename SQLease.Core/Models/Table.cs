using Microsoft.Data.Analysis;

namespace SQLease.Core.Models;

public class Table(string name)
{
    public string Name { get; set; } = name;
    private List<Column> Columns { get; set; } = [];
    private readonly DataFrame _dataFrame = new();
    private readonly Dictionary<string, Type> _columnTypes = new();

    public void AddColumn(string columnName, Type dataType)
    {
        if (_columnTypes.ContainsKey(columnName))
            throw new InvalidOperationException($"Column {columnName} already exists");
        
        var column = new Column(columnName, dataType);
        Columns.Add(column);
        _columnTypes.Add(columnName, dataType);

        DataFrameColumn dataFrameColumn = dataType switch
        {
            _ when dataType == typeof(int) => new Int32DataFrameColumn(columnName),
            _ when dataType == typeof(double) => new DoubleDataFrameColumn(columnName),
            _ when dataType == typeof(DateTime) => new DateTimeDataFrameColumn(columnName),
            _ when dataType == typeof(bool) => new BooleanDataFrameColumn(columnName),
            _ when dataType == typeof(string) => new StringDataFrameColumn(columnName),
            _ => new StringDataFrameColumn(columnName),
        };
        
        _dataFrame.Columns.Add(dataFrameColumn);
    }

    public void InsertRow(Dictionary<string, object?> rowData)
    {
        List<object> values = [];
        
        foreach (var column in Columns)
        {
            if (!rowData.TryGetValue(column.Name, out var value))
                throw new InvalidOperationException($"Column {column.Name} does not exist");
            
            if (column.DataType != value?.GetType())
                throw new InvalidOperationException($"Column {column.Name} data type does not match");
            
            values.Add(value);
        }
        
        _dataFrame.Append(values.ToArray(), inPlace: true);
    }
    
    public void PrintAllRows() => Console.WriteLine(_dataFrame);
    
    public DataFrame GetDataFrame() => _dataFrame;
}