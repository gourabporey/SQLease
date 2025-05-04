namespace SQLease.Core.Models;

public class Column(string name, Type dataType)
{
    public string Name { get; set; } = name;
    public Type DataType { get; set; } = dataType;
}