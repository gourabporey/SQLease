namespace SQLease.Core.Storage;

public interface ITableStorage
{
    void AddColumn(string name, Type type);
    void AddDeletedColumn(string columnName);
    void InsertRow(Dictionary<string, object?> rowValues);
    IEnumerable<Dictionary<string, object?>> GetAllRows(bool includeDeleted = false);
    int UpdateRows(Func<Dictionary<string, object?>, bool> predicate, Dictionary<string, object?> newValues);
    int DeleteRows(Func<Dictionary<string, object?>, bool> predicate);
    void Compact();
}