using MegaCrit.Sts2.Core.Multiplayer.Game;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.Networking;

public static class MultiplayerManager
{
    private static INetGameService? _netGameService;
    private static ulong _localPlayerId;
    
    private static readonly Dictionary<ulong, StampDefinition> _playerStampDefinitions = new();
    
    public static ulong LocalPlayerId => _localPlayerId;
    
    public static void Initialize(INetGameService netGameService)
    {
        _netGameService = netGameService;
        _localPlayerId = netGameService.NetId;
        netGameService.RegisterMessageHandler<StampMessage>(OnStampMessageReceived);

    }
    
    public static void BroadcastStamp()
    {
        if (_localPlayerId == 0 || _netGameService == null)
        {
            return;
        }
        
        var message = new StampMessage
        {
            PlayerId = LocalPlayerId,
            Stamp = StampRegistry.ActiveStamp
        };
        
        _netGameService.SendMessage(message);
    }

    public static StampDefinition? GetPlayerStampDefinition(ulong playerId)
    {
        return _playerStampDefinitions.GetValueOrDefault(playerId);
    }
    
    private static void OnStampMessageReceived(StampMessage message, ulong senderId)
    {
        _playerStampDefinitions[senderId] = message.Stamp;
    }
}