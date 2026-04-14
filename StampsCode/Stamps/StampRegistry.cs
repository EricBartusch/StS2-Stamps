using Stamps.StampsCode.Networking;

namespace Stamps.StampsCode.Stamps;

public static class StampRegistry
{
    private static readonly List<StampDefinition> _stamps = [];
    private static string _previousActiveStampName;

    public static IReadOnlyList<StampDefinition> All => _stamps;

    public static StampDefinition ActiveStamp { get; private set; } = null!;

    public static void NukeStamps()
    {
        _previousActiveStampName = ActiveStamp?.Name;
        _stamps.Clear();
        ActiveStamp = null!;
    }

    public static void SetActive(StampDefinition stamp)
    {
        ActiveStamp = stamp;
    }

    public static StampDefinition GetStampByName(string name)
    {
        return _stamps.FirstOrDefault(s => s.Name == name);
    }

    public static void Register(StampDefinition stamp)
    {
        foreach (var stampDefinition in _stamps)
        {
            if (stampDefinition.Name == stamp.Name)
                return;
        }
        _stamps.Add(stamp);
        if (stamp.Name == _previousActiveStampName)
            ActiveStamp = stamp;
    }
}
