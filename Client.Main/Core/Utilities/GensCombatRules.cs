using Client.Data.ATT;
using Client.Main.Controls;
using Client.Main.Core.Client;
using Client.Main.Objects.Player;
using Client.Main.Scenes;

namespace Client.Main.Core.Utilities;

/// <summary>Season 6 client rules. The native client identifies Vulcanus (63) as its Gens battle zone.</summary>
public static class GensCombatRules
{
    public const ushort VulcanusMap = 63;
    public static bool IsBattleZone => MuGame.Network?.GetCharacterState()?.MapId == VulcanusMap;

    public static bool TryGetRelation(PlayerObject player, out GensStatus relation)
    {
        relation = null;
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || player == null) return false;
        if (MuGame.Instance.ActiveScene is GameScene scene && player == scene.Hero)
        {
            relation = state.Gens;
            return relation.Received && relation.Family is 1 or 2;
        }
        return MuGame.Network.GetScopeManager().TryGetGensRelation(player.NetworkId, out relation)
            && relation.Family is 1 or 2;
    }

    public static bool IsProtectedTarget(PlayerObject player)
    {
        var state = MuGame.Network?.GetCharacterState();
        return IsBattleZone && state?.IsDuelActive != true && state?.Gens.Family is 1 or 2
            && TryGetRelation(player, out var relation) && relation.Family == state.Gens.Family;
    }

    public static bool IsEnemy(PlayerObject player)
    {
        var state = MuGame.Network?.GetCharacterState();
        if (!IsBattleZone || state?.IsDuelActive != false || state.Gens.Family is not (1 or 2)
            || MuGame.Instance.ActiveScene is not GameScene scene || !scene.MuHelperWorldReady || scene.World is not WalkableWorldControl world
            || scene.Hero == null || scene.Hero.IsDead || player == null || player == scene.Hero
            || player.IsDead || player.World != world || !TryGetRelation(player, out var relation)
            || relation.Family == state.Gens.Family) return false;
        return !world.Terrain.RequestTerrainFlag((int)scene.Hero.Location.X, (int)scene.Hero.Location.Y).HasFlag(TWFlags.SafeZone)
            && !world.Terrain.RequestTerrainFlag((int)player.Location.X, (int)player.Location.Y).HasFlag(TWFlags.SafeZone);
    }
}
