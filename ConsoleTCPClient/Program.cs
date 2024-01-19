using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.Net;
using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.PlayerHandles.Remote;
using System.Net;

// See https://aka.ms/new-console-template for more information
string addr = "localhost"; int port = 12332;

if(args.Length == 2)
{
    addr = (args[0] == "localhost" ? "localhost" : IPAddress.Parse(args[0]).ToString());
    port = int.Parse(args[1]);
}
 

Console.WriteLine($"IP: {addr}, Port: {port}");
Console.Write("Enter your name: ");
var name = Console.ReadLine();

ProtocolConnection c = ProtocolConnection.CreateWithValidation(PokerProtocol.BuildProtocol(), new TcpConnection(addr.ToString(), port), ConnectionNegotiationPosition.Challenger);

ConsolePlayer p = new ConsolePlayer(name);
Task.Delay(1000).Wait();    
RemoteReceiver recv = new(p, c);
while (true) { Task.Delay(100).Wait(); }