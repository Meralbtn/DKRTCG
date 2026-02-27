// See https://aka.ms/new-console-template for more information
using UnityServer;
Console.WriteLine("Hello, World!");
string ip = "111.229.131.240";
ushort port = 8888;
var key = Console.ReadKey(true);

try 
{
    if (key.Key == ConsoleKey.C)
    {
        CardClient client = new CardClient();
        client.Start(ip, port);
    }
    else if (key.Key == ConsoleKey.S)
    {
        CardServer server = new CardServer();
        server.Start(port);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error starting client: " + ex.Message);
}