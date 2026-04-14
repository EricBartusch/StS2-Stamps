using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using Stamps.StampsCode.Stamps;

namespace Stamps.StampsCode.Networking;

public class StampMessage : INetMessage
{
    public ulong PlayerId;
    public StampDefinition Stamp;
    public string Name;
    public bool Sharing;
    
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Info;

    public void Serialize(PacketWriter writer)
    {
        var stamp = StampRegistry.ActiveStamp;
        writer.WriteULong(PlayerId);
        writer.WriteString(Name);
        writer.WriteBool(Sharing);
        writer.WriteInt(stamp.Strokes.Count);

        foreach (var stroke in stamp.Strokes)
        {
            writer.WriteInt(stroke.Points.Count);

            foreach (var point in stroke.Points)
            {
                writer.WriteFloat(point.X);
                writer.WriteFloat(point.Y);
            }
            writer.WriteBool(stroke.Erase);
        }
    }

    void IPacketSerializable.Deserialize(PacketReader reader)
    {
        PlayerId = reader.ReadULong();
        Name = reader.ReadString();
        Sharing = reader.ReadBool();
        var strokesCount = reader.ReadInt();
        var strokes = new List<StampStroke>();

        for (var i = 0; i < strokesCount; i++)
        {
            var pointCount = reader.ReadInt();
            var points = new List<Vector2>(pointCount);

            for (var j = 0; j < pointCount; j++)
            {
                points.Add(new Vector2
                {
                    X = reader.ReadFloat(),
                    Y = reader.ReadFloat()
                });
            }
            strokes.Add(new StampStroke(points, reader.ReadBool()));
        }
        Stamp = new StampDefinition(Name, strokes, PlayerId); 
    }
}