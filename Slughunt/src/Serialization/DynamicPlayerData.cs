using System.Collections.Generic;
using RainMeadow;

namespace Slughunt.Serialization;

public class DynamicPlayerData : DynamicDictionary<OnlinePlayer, PlayerData, DynamicPlayerData> {
    public DynamicPlayerData() { }

    public DynamicPlayerData(Dictionary<OnlinePlayer, PlayerData> list) {
        added = [];
        foreach (KeyValuePair<OnlinePlayer, PlayerData> x in list)
            added.Add(x.Key, x.Value with { });
    }

    public override void CustomSerialize(Serializer serializer) {
        Serialize(serializer, ref added);
        if (serializer.IsDelta)
            Serialize(serializer, ref removed);
    }

    private static void Serialize(Serializer serializer, ref Dictionary<OnlinePlayer, PlayerData>? data) {
        if (serializer.IsWriting)
            Write(serializer, data);
        if (serializer.IsReading)
            Read(serializer, out data);
        // can a serializer be not reading and not writing or both reading and writing?????
    }

    private static void Write(Serializer serializer, Dictionary<OnlinePlayer, PlayerData>? data) {
        serializer.writer.Write((byte)(data?.Count ?? 0));
        if (data is null)
            return;
        foreach (KeyValuePair<OnlinePlayer, PlayerData> x in data) {
            serializer.writer.Write(x.Key.inLobbyId);
            x.Value.Write(serializer.writer);
        }
    }

    private static void Read(Serializer serializer, out Dictionary<OnlinePlayer, PlayerData>? data) {
        byte count = serializer.reader.ReadByte();
        if (count == 0) {
            data = null;
            return;
        }
        data = [];
        for (int i = 0; i < count; ++i) {
            ushort id = serializer.reader.ReadUInt16();
            PlayerData value = PlayerData.Read(serializer.reader);
            OnlinePlayer? player = OnlineManager.lobby?.PlayerFromId(id);
            if (player is null) {
                Plugin.logger.LogError($"player not found {id}");
                continue;
            }
            data.Add(player, value);
        }
    }
}
