// See https://aka.ms/new-console-template for more information
using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.Net;
using LightBlueFox.Connect.Net.ConnectionSources;
using LightBlueFox.Connect.Structure;
using LightBlueFox.Games.Poker;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.PlayerHandles.Remote;

Console.WriteLine("Creating new game.");
Game game = new("test");

Console.WriteLine("Created game..");
Console.WriteLine("Starting server on port 12332...");

ProtocolServer s = new(PokerProtocol.BuildProtocol(), new[] { new TcpSource(12332) });
s.OnConnectionValidated += newPlayer;

Console.ReadKey(true);




while (true) {
    if (game.Players.Count > 1) {
        Console.WriteLine("Enough players connected. Starting (next) round.");
        game.startRound();
    }
    Task.Delay(100).Wait(); 
}


void newPlayer(ProtocolConnection c, ProtocolServer sender)
{
    Console.WriteLine("New Player Connected.");
    game.AddPlayer(RemotePlayer.CreatePlayer(c));
}