using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.Networking;

public static class MultiplayerManager
{
    private static INetGameService? _netGameService;
    private static ulong _localPlayerId;
    
    private static readonly Dictionary<ulong, StampDefinition> _playerStampDefinitions = new();
    public static HashSet<StampDefinition> SharedStamps = new();
    public static event Action? SharedStampsChanged;
    
    public static ulong LocalPlayerId => _localPlayerId;
    
    public static void Initialize(INetGameService netGameService)
    {
        _netGameService = netGameService;
        _localPlayerId = netGameService.NetId;
        netGameService.RegisterMessageHandler<StampMessage>(OnStampMessageReceived);
    }
    
    public static void BroadcastStamp(StampDefinition stamp)
    {
        if (_localPlayerId == 0 || _netGameService == null)
        {
            return;
        }
        
        var message = new StampMessage
        {
            PlayerId = LocalPlayerId,
            Stamp = stamp,
            Name = stamp.Name,
        };
        
        _netGameService.SendMessage(message);
    }
    
    public static StampDefinition? GetPlayerStampDefinition(ulong playerId)
    {
        return _playerStampDefinitions.GetValueOrDefault(playerId);
    }

    public static StampDefinition? ReadSharedStampDefinition()
    {
        var message = SharedStamps.First();
        SharedStamps.Remove(message);
        SharedStampsChanged?.Invoke();
        return message;
    }
    
    private static void OnStampMessageReceived(StampMessage message, ulong senderId)
    {
        if (Config.AcceptStamps)
        {
            _playerStampDefinitions[senderId] = message.Stamp;
            SharedStamps.Add(message.Stamp);
            SharedStampsChanged?.Invoke();
        }
    }
}