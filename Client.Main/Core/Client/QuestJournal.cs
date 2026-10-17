using System.Buffers.Binary;

namespace Client.Main.Core.Client;

/// <summary>Season 6 quest state. Only server responses change active quests.</summary>
public sealed class QuestJournal
{
    public sealed record Entry(ushort Group, ushort Number, bool Active);
    public sealed record Condition(uint Type, ushort Id, uint Required, uint Current, byte[] ItemData);
    public sealed record Reward(uint Type, ushort Id, uint Count, byte[] ItemData);
    public sealed record Detail(ushort Group, ushort Number, Condition[] Conditions, Reward[] Rewards, byte RandomRewards);
    public List<Entry> Active { get; } = new();
    public List<Entry> Available { get; } = new();
    public Dictionary<ushort, Detail> Details { get; } = new();
    public ushort Npc { get; private set; }
    public (ushort Group, ushort Number)? OfferedStep { get; private set; }
    public int Revision { get; private set; }
    public string Notice { get; private set; } = "Visit a quest NPC to find available quests.";

    public void Reset()
    {
        Active.Clear(); Details.Clear(); CloseNpc();
        Notice = "Visit a quest NPC to find available quests.";
    }

    public void CloseNpc()
    {
        Npc = 0;
        Available.Clear();
        OfferedStep = null;
        Revision++;
    }

    // Reject malformed packets before mutating any state. Extended-season packets
    // intentionally use a different format and must not be interpreted as S6.
    public byte Apply(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4 || data[0] != 0xC1 || data[1] != data.Length || data[2] != 0xF6)
            throw new ArgumentException("Invalid Season 6 quest packet.");
        byte sub = data[3];
        static ushort U16(ReadOnlySpan<byte> p, int i) => BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(i, 2));
        static uint U32(ReadOnlySpan<byte> p, int i) => BinaryPrimitives.ReadUInt32LittleEndian(p.Slice(i, 4));
        if (sub is 0x0A or 0x1A)
        {
            int offset = sub == 0x0A ? 8 : 5;
            if (data.Length < offset) throw new ArgumentException("Truncated quest list.");
            int count = sub == 0x0A ? U16(data, 6) : data[4];
            if (data.Length < offset + count * 4) throw new ArgumentException("Truncated quest entries.");
            var entries = new List<Entry>();
            for (int i = 0; i < count; i++)
            {
                int at = offset + i * 4;
                ushort group = U16(data, at + 2);
                if (group != 0) entries.Add(new(group, U16(data, at), sub == 0x1A));
            }
            if (sub == 0x0A)
            {
                Npc = U16(data, 4);
                Available.Clear(); Available.AddRange(entries);
                OfferedStep = null;
                Notice = count == 0 ? "No quests available at your level." : "Select a quest to talk to this NPC.";
            }
            else
            {
                Active.Clear(); Active.AddRange(entries);
                foreach (var group in Details.Keys.ToArray())
                    if (!Active.Any(e => e.Group == group && e.Number == Details[group].Number)) Details.Remove(group);
            }
        }
        else if (sub is 0x0C or 0x1B)
        {
            if (data.Length != 251 || data[8] > 5 || data[9] > 5)
                throw new ArgumentException("Invalid quest detail counts or size.");
            var conditions = new Condition[data[8]];
            var rewards = new Reward[data[9]];
            for (int i = 0; i < conditions.Length; i++)
            {
                int at = 11 + i * 26;
                conditions[i] = new(data[at], U16(data, at + 4), U32(data, at + 6), U32(data, at + 10), data.Slice(at + 14, 12).ToArray());
            }
            for (int i = 0; i < rewards.Length; i++)
            {
                int at = 141 + i * 22;
                rewards[i] = new(data[at], U16(data, at + 4), U32(data, at + 6), data.Slice(at + 10, 12).ToArray());
            }
            ushort group = U16(data, 6), number = U16(data, 4);
            // An unsolicited progress message can advance/start a quest. A late
            // detail reply must not resurrect a cancelled or completed quest.
            if (sub == 0x0C || Active.Any(e => e.Group == group && e.Number == number))
            {
                Active.RemoveAll(e => e.Group == group);
                Active.Add(new(group, number, true));
                Details[group] = new(group, number, conditions, rewards, data[10]);
                if (OfferedStep?.Group == group) OfferedStep = null;
                Notice = "Progress updated by the server.";
            }
        }
        else if (sub == 0x0B)
        {
            if (data.Length != 11) throw new ArgumentException("Invalid quest step.");
            ushort group = U16(data, 6), number = U16(data, 4);
            OfferedStep = Npc != 0 && Available.Any(e => e.Group == group && e.Number == number) ? (group, number) : null;
        }
        else if (sub is 0x0D or 0x0F)
        {
            if (data.Length != (sub == 0x0D ? 9 : 8)) throw new ArgumentException("Invalid quest result.");
            ushort group = U16(data, 6);
            bool success = sub == 0x0F || data[8] != 0;
            if (success)
            {
                Active.RemoveAll(e => e.Group == group);
                Details.Remove(group);
                OfferedStep = null;
            }
            Notice = sub == 0x0F ? "Quest abandoned." : success ? "Quest completed!" : "Requirements are not complete. Talk to the quest NPC.";
        }
        else throw new ArgumentException("Unsupported quest response.");
        Revision++;
        return sub;
    }

    public static byte[] Request(byte sub, ushort group = 0, ushort number = 0)
    {
        int length = sub switch { 0x1A or 0x30 => 4, 0x0A or 0x0B => 9, 0x0D or 0x0F or 0x1B => 8, _ => throw new ArgumentException("Unsupported quest request.") };
        var packet = new byte[length];
        packet[0] = 0xC1; packet[1] = (byte)length; packet[2] = 0xF6; packet[3] = sub;
        if (length > 4)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4), number);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6), group);
        }
        if (sub == 0x0B) packet[8] = 1; // Accept the NPC-confirmed starting step.
        return packet;
    }
}
