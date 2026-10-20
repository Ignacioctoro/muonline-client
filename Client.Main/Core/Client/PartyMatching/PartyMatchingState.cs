using System;
using System.Buffers.Binary;
using System.Text.Json;

namespace Client.Main.Core.Client.PartyMatching;

public sealed class PartyMatchingState
{
    public PartyMatchingSnapshot Snapshot { get; private set; } = new();
    public string Notice { get; set; } = "Open Party Matching to search this server.";
    public int Revision { get; private set; }
    public bool Waiting { get; private set; }
    public bool Available { get; private set; }
    private DateTime _deadline;
    public void Reset()
    { Snapshot = new(); Waiting = false; Available = false; Notice = "Open Party Matching to search this server."; Revision++; }
    public bool Begin()
    {
        if (Waiting) return false;
        Waiting = true; _deadline = DateTime.UtcNow.AddSeconds(10); return true;
    }
    public void Fail(string message) { Waiting = false; Notice = message; Revision++; }
    public void Update()
    {
        if (Waiting && DateTime.UtcNow >= _deadline)
        { Available = false; Fail("No response. Update OpenMU and enable the Party Matching plugins."); }
    }
    public void Receive(byte[] packet)
    {
        if (packet.Length is < 7 or > 16384 || packet[0] != 0xC2 || packet[4] != 1 ||
            BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(1, 2)) != packet.Length) return;
        try
        {
            var next = JsonSerializer.Deserialize<PartyMatchingSnapshot>(packet.AsSpan(5), new JsonSerializerOptions { MaxDepth = 8 });
            if (next?.Ads == null || next.Requests == null || next.Ads.Length > 8 || next.Requests.Length > 20) return;
            Waiting = false; Available = true;
            bool applicationEnded = Snapshot.PendingAd.Length > 0 && next.Status != 9 && next.PendingAd.Length == 0;
            if (next.Status != 9) Snapshot = next;
            if (applicationEnded && next.Status == 0)
                Notice = MuGame.Network.GetPartyManager().GetPartyMembers().Count > 0 ? "Joined the party." : "Application ended: rejected, withdrawn, expired, or recruitment stopped.";
            if (next.Status != 0) Notice = next.Status switch
            {
                1 => "Done.", 2 => "Player unavailable: close other dialogs or try again.",
                3 => "Only the party leader can recruit.", 4 => "Check the title, level range, classes and password.",
                5 => "Party full, already in a party, or application pending.", 6 => "This advertisement or application is no longer available.",
                7 => "Party requirements or server rules do not allow joining.", 8 => "Incorrect password.",
                9 => "Please wait before trying again.", 10 => "Application sent. Waiting for the leader.",
                11 => "Joined the party.", _ => "Server could not complete the request."
            };
            else if (Notice.StartsWith("Open Party") || Notice.StartsWith("No response")) Notice = "Party list updated.";
            Revision++;
        }
        catch (JsonException) { Fail("Invalid Party Matching response."); }
    }
}
