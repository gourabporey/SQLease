using SQLease.Core.Storage;

namespace SQLease.Tests.Storage;

public class DataFrameTableStorageTests
{
    private static DataFrameTableStorage CreateSampleStorage()
    {
        var storage = new DataFrameTableStorage();
        storage.AddColumn("Id", typeof(int));
        storage.AddColumn("Name", typeof(string));
        storage.AddDeletedColumn("__Deleted");
        return storage;
    }

    [Fact]
    public void InsertRow_ShouldInsertAndReturnOneRow()
    {
        var storage = CreateSampleStorage();

        storage.InsertRow(new Dictionary<string, object?>
        {
            { "Id", 1 },
            { "Name", "Alice" }
        });

        var rows = storage.GetAllRows().ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0]["Id"]);
        Assert.Equal("Alice", rows[0]["Name"]);
    }

    [Fact]
    public void DeleteRows_ShouldTombstoneRows()
    {
        var storage = CreateSampleStorage();

        storage.InsertRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
        storage.InsertRow(new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } });

        var deleted = storage.DeleteRows(row => (int)row["Id"] == 1);

        var remaining = storage.GetAllRows().ToList();

        Assert.Equal(1, deleted);
        Assert.Single(remaining);
        Assert.Equal(2, remaining[0]["Id"]);
    }

    [Fact]
    public void UpdateRows_ShouldModifyMatchingRows()
    {
        var storage = CreateSampleStorage();

        storage.InsertRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
        storage.InsertRow(new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } });

        var updated = storage.UpdateRows(
            row => (int)row["Id"] == 2,
            new Dictionary<string, object?> { { "Name", "Robert" } }
        );

        var rows = storage.GetAllRows().ToList();

        Assert.Equal(1, updated);
        Assert.Contains(rows, r => r["Name"]!.Equals("Robert"));
    }

    [Fact]
    public void Compact_ShouldRemoveDeletedRowsPhysically()
    {
        var storage = CreateSampleStorage();

        storage.InsertRow(new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Alice" } });
        storage.InsertRow(new Dictionary<string, object?> { { "Id", 2 }, { "Name", "Bob" } });

        storage.DeleteRows(r => (int)r["Id"] == 1);
        storage.Compact();

        var result = storage.GetAllRows(true).ToList();
        Assert.Single(result); // physically only 1 row now
        Assert.Equal(2, result[0]["Id"]);
    }

    [Fact]
    public void AddColumn_ShouldThrowIfDuplicate()
    {
        var storage = CreateSampleStorage();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            storage.AddColumn("Name", typeof(string))
        );

        Assert.Contains("exists", ex.Message);
    }

    [Fact]
    public void InsertRow_ShouldThrowIfMissingColumn()
    {
        var storage = CreateSampleStorage();

        Assert.Throws<InvalidOperationException>(() =>
            storage.InsertRow(new Dictionary<string, object?> { { "Id", 1 } })
        );
    }
}