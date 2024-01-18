using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.Net;
using LightBlueFox.Connect.Structure;
using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.PlayerHandles.Remote;

// See https://aka.ms/new-console-template for more information

Console.Write("Enter your name: ");
var name = Console.ReadLine();

ProtocolConnection c = ProtocolConnection.CreateWithValidation(PokerProtocol.BuildProtocol(), new TcpConnection("localhost", 12332), ConnectionNegotiationPosition.Challenger);

ConsolePlayer p = new ConsolePlayer(name);
Task.Delay(1000).Wait();    
RemoteReceiver recv = new(p, c);
while (true) { Task.Delay(100).Wait(); }