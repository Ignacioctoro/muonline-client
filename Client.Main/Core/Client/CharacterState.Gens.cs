using System.Threading;

namespace Client.Main.Core.Client;

/// <summary>Server-authoritative Season 6 Gens information. NextContribution is an amount still missing.</summary>
public sealed record GensStatus(byte Family, uint Position, uint Rank, uint Contribution, uint NextContribution, bool Received = true);

public partial class CharacterState
{
    private GensStatus _gens = new(0, 0, 0, 0, 0, false);
    private int _gensRevision;
    public GensStatus Gens => Volatile.Read(ref _gens);
    public int GensRevision => Volatile.Read(ref _gensRevision);
    public void SetGensInfo(byte family, uint position, uint rank, uint contribution, uint nextContribution)
    {
        if (family > 2) return;
        Volatile.Write(ref _gens, family == 0 ? new(0, 0, 0, 0, 0) : new(family, position, rank, contribution, nextContribution));
        Interlocked.Increment(ref _gensRevision);
    }
    private void ResetGens()
    {
        Volatile.Write(ref _gens, new(0, 0, 0, 0, 0, false));
        Interlocked.Increment(ref _gensRevision);
    }
}
