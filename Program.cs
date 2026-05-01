// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CardGameServer;
try
{
    DatabaseManager.Instance.Init(
    host: "localhost",
    database: "cardgame",
    user: "cardgame",
    password: "opq2000ll");
    CardConfigManager.Instance.Load("cards.json");
    var server = new CardServer();
    server.Open();
    Console.WriteLine("服务器已启动，按任意键退出...");
    Console.ReadKey();
}
catch (Exception ex)
{
    Console.WriteLine($"服务器异常: {ex.Message}");
}