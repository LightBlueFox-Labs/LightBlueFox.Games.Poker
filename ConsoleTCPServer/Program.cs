// See https://aka.ms/new-console-template for more information
using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.Net;
using LightBlueFox.Connect.Net.ConnectionSources;
using LightBlueFox.Connect.Structure;
using LightBlueFox.Games.Poker;
using LightBlueFox.Games.Poker.PlayerHandles;
using LightBlueFox.Games.Poker.PlayerHandles.Remote;
using System.Net;

IPAddress listenerAddr = args[0] == "ALL" ? IPAddress.Any : IPAddress.Parse(args[0]);
int port = int.Parse(args[1]);
Dictionary<ProtocolConnection, PlayerHandle> playerz = new();

Console.WriteLine("[SERVER] Creating new game.");
Game game = new("test");
Console.WriteLine("[SERVER] Created game..");

Console.WriteLine($"[SERVER] Starting server {listenerAddr}:{port}...");

ProtocolServer s = new(PokerProtocol.BuildProtocol(), new[] { new TcpSource(12332) });
s.OnConnectionValidated += newPlayer;
s.OnConnectionDisconnected += playerDisconnect;



Console.WriteLine("[SERVER] Now listening.");


Console.ReadKey(true);




while (true) {
    if (game.Players.Count > 1) {
        Console.WriteLine("[SERVER] Enough players connected. Starting (next) round.");
        game.startRound();
    }
    Task.Delay(100).Wait(); 
}


void newPlayer(ProtocolConnection c, ProtocolServer sender)
{
    Console.WriteLine("[SERVER] New Player Connected.");
    var p = RemotePlayer.CreatePlayer(c);
    playerz.Add(c, p);
	game.AddPlayer(p);
}

void playerDisconnect(ProtocolConnection c, ProtocolServer sender)
{
	if(playerz.ContainsKey(c))
    {
        var p = playerz[c];
        playerz.Remove(c);
        game.RemovePlayer(p);
    }
}