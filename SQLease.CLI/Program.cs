using SQLease.Core.Models;

Console.WriteLine("SQLease DB started");

var database = new Database();
var columnDetails = new Dictionary<string, Type>
{
    { "Username", typeof(string) },
    { "Email", typeof(string) },
    { "Password", typeof(string) },
    { "dob", typeof(DateTime)}
};
database.CreateTable("Users", columnDetails);

var user = new Dictionary<string, object?>()
{
    ["Username"] = "gourabporey",
    ["Email"] = "gourabporey@gmail.com",
    ["Password"] = "gourabporey",
    ["dob"] = new DateTime(2001, 12, 12, 12, 12, 12)
};

var usersTable = database.GetTable("Users");

usersTable.InsertRow(user);
Console.WriteLine("Inserted user");

usersTable.PrintAllRows();