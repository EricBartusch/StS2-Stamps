using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.Networking;

public static class MultiplayerManager
{
    private static INetGameService? _netGameService;
    private static ulong _localPlayerId;
    
    private static readonly Dictionary<ulong, StampDefinition> _playerStampDefinitions = new();
    private static readonly HashSet<ulong> _playersInStampMode = new();
    public static HashSet<StampDefinition> SharedStamps = new();
    public static event Action? SharedStampsChanged;

    public static ulong LocalPlayerId => _localPlayerId;

    public static void Initialize(INetGameService netGameService)
    {
        _netGameService = netGameService;
        _localPlayerId = netGameService.NetId;
        _playersInStampMode.Clear();
        netGameService.RegisterMessageHandler<ZZStampMessage>(OnStampMessageReceived);
        netGameService.RegisterMessageHandler<ZZStampModeMessage>(OnStampModeMessageReceived);
    }

    public static void BroadcastStampMode(bool inStampMode)
    {
        if (_localPlayerId == 0 || _netGameService == null)
        {
            return;
        }

        _netGameService.SendMessage(new ZZStampModeMessage { InStampMode = inStampMode });
    }

    public static bool IsPlayerInStampMode(ulong playerId)
    {
        return _playersInStampMode.Contains(playerId);
    }
    
    public static void BroadcastStamp(StampDefinition stamp)
    {
        if (_localPlayerId == 0 || _netGameService == null)
        {
            return;
        }
        
        var message = new ZZStampMessage
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
    
    private static void OnStampMessageReceived(ZZStampMessage message, ulong senderId)
    {
        if (Config.AcceptStamps)
        {
            _playerStampDefinitions[senderId] = message.Stamp;
            SharedStamps.Add(message.Stamp);
            SharedStampsChanged?.Invoke();
        }
    }

    private static void OnStampModeMessageReceived(ZZStampModeMessage message, ulong senderId)
    {
        if (message.InStampMode)
            _playersInStampMode.Add(senderId);
        else
            _playersInStampMode.Remove(senderId);

        NGame.Instance?.RemoteCursorContainer.DrawingCursorStateChanged(senderId);
    }
}