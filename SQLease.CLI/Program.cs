using SQLease.Core.Models;

Console.WriteLine("SQLease DB started");

var database = new Database();
var columnDetails = new Dictionary<string, Type>
{
    { "Username", typeof(string) },
    { "Email", typeof(string) },
    { "DateOfBirth", typeof(DateTime)}
};
database.CreateTable("Users", columnDetails);

var user1 = new Dictionary<string, object?>()
{
    ["Username"] = "gourabporey",
    ["Email"] = "gourabporey@gmail.com",
    ["DateOfBirth"] = new DateTime(2001, 12, 12, 12, 12, 12)
};

var user2 = new Dictionary<string, object?>()
{
    ["Username"] = "crishem",
    ["Email"] = "crishem@gmail.com",
    ["DateOfBirth"] = new DateTime(1990, 12, 12, 12, 12, 12)
};

var user3 = new Dictionary<string, object?>()
{
    ["Username"] = "georgia",
    ["Email"] = "ge@gmail.com",
    ["DateOfBirth"] = new DateTime(2005, 12, 12, 12, 12, 12)
};

var usersTable = database.GetTable("Users");

usersTable.InsertRow(user1);
usersTable.InsertRow(user2);
usersTable.InsertRow(user3);

Console.WriteLine("Inserted users");

usersTable.PrintAllRows();

Console.WriteLine("Deleting user gourabporey");
usersTable.DeleteRows(r => (string)r["Username"] == "gourabporey");
Console.WriteLine("Deleted user");

usersTable.PrintAllRows();