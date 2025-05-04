using SQLease.Core.Models;

Console.WriteLine("SQLease DB started");

var database = new Database();
var columnDetails = new Dictionary<string, Type>
{
    { "Username", typeof(string) },
    { "Email", typeof(string) },
    { "Password", typeof(string) }
};
database.CreateTable("Users", columnDetails);

var user = new Dictionary<string, object?>()
{
    ["Username"] = "gourabporey",
    ["Email"] = "gourabporey@gmail.com",
    ["Password"] = "gourabporey"
};

var usersTable = database.GetTable("Users");

usersTable.InsertRow(user);
Console.WriteLine("Inserted user");

foreach (var cell in usersTable.Rows.SelectMany(row => row.Data))
{
    Console.WriteLine($"{cell.Key}: {cell.Value}");
}