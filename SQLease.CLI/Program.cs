using SQLease.Core.Models;

Console.WriteLine("SQLease DB started");

var usersTable = new Table("Users");
usersTable.AddColumn("Username", typeof(string));
usersTable.AddColumn("Email", typeof(string));
usersTable.AddColumn("Password", typeof(string));

var user = new Dictionary<string, object?>()
{
    ["Username"] = "gourabporey",
    ["Email"] = "gourabporey@gmail.com",
    ["Password"] = "gourabporey"
};
usersTable.InsertRow(user);

Console.WriteLine("Inserted user");
foreach (var cell in usersTable.Rows.SelectMany(row => row.Data))
{
    Console.WriteLine($"{cell.Key}: {cell.Value}");
}