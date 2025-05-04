namespace SQLease.Core.Models;

public class Row(Dictionary<string, object?> data)
{
    public Dictionary<string, object?> Data { get; set; } = data;
}