using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Data.ATT;
using Client.Data.BMD;
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Scenes;

/// <summary>Online S6 helper. The server authorizes the session; ordinary client actions execute it.</summary>
internal sealed partial class GameSceneMuHelperController
{
    private enum Phase { Idle, Starting, Running, Stopping }
    private readonly GameScene _scene;
    private readonly GameSceneSkillController _skills;
    private Phase _phase;
    private string _owner;
    private int _ownerClass, _epoch;
    private ushort _map;
    private MuHelperConfigurationData _settings;
    private Vector2 _origin, _moveTarget;
    private MonsterObject _target;
    private long _deadline, _nextTick, _nextMove, _nextPotion, _awaySince, _pickupBlockedUntil;
    private bool _ownsActions, _potionSending;
    private ushort? _pickup;
    private long _pickupAt;
    private readonly Dictionary<ushort, long> _dropRetry = new();
    private readonly Dictionary<ushort, long> _monsterRetry = new();
    private Vector2 _progressLocation;
    private long _progressAt;
    private long _sessionStarted, _sessionStopped;
    private ulong _sessionZen;
    public void ShowLog()
    {
        var elapsed = _sessionStarted == 0 ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(Math.Max(0, (_sessionStopped == 0 ? Environment.TickCount64 : _sessionStopped) - _sessionStarted));
        string status = _phase switch { Phase.Running => "Running", Phase.Starting => "Starting", Phase.Stopping => "Stopping", _ => "Stopped" };
        RequestDialog.ShowInfo($"Hunting Log\nStatus: {status}\nTime: {(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}\nZen spent: {_sessionZen:N0}");
    }
    public bool Active => _phase == Phase.Running;
    public bool Busy => _phase != Phase.Idle;

    public GameSceneMuHelperController(GameScene scene, GameSceneSkillController skills)
    { _scene = scene; _skills = skills; }

    private bool SameCharacter()
    {
        var state = MuGame.Network?.GetCharacterState();
        return state != null && state.Name == _owner && (int)state.Class == _ownerClass;
    }

    public void Toggle()
    {
        if (Busy) { Stop(); return; }
        var network = MuGame.Network;
        var state = network?.GetCharacterState();
        var hero = _scene.Hero;
        if (network?.IsConnected != true || state?.IsInGame != true || hero == null || hero.IsDead || !_scene.MuHelperWorldReady) return;
        if (_scene.World is not WalkableWorldControl world || IsSafe(world, hero.Location))
        { RequestDialog.ShowInfo("MU Helper cannot start in a safe zone."); return; }
        var settings = MuHelperControl.Instance.SavedConfigurationForCurrentCharacter();
        if (settings == null) { RequestDialog.ShowInfo("Save the MU Helper settings before starting."); return; }
        ushort id = (ushort)settings.Word(5);
        if (id != 0 && (!state.GetSkills().Any(s => s.SkillId == id) || !RepeatableAttack(id)))
        { RequestDialog.ShowInfo("Choose an available attack skill in MU Helper."); return; }
        if (!ValidCombo(settings, state))
        { RequestDialog.ShowInfo("Combo: choose a weapon skill, then two different BK finishers (for example Cyclone / Twisting Slash / Death Stab)."); return; }
        _owner = state.Name; _ownerClass = (int)state.Class; _map = state.MapId;
        _settings = settings; _origin = hero.Location;
        _sessionStarted = _sessionStopped = 0; _sessionZen = 0;
        _epoch++; _phase = Phase.Starting; _deadline = Environment.TickCount64 + 10000;
        MuHelperControl.Instance.CloseWindow();
        _ = SendStateAsync(false, _epoch);
    }

    private static bool RepeatableAttack(ushort id) => id is not (6 or 7 or 40 or 58)
        && !DarkRavenCommands.IsCommandSkill(id) && !CastleSiegeSkillRules.IsCastleSiegeOnly(id)
        && SkillDatabase.GetSkillDefinition(id)?.Type is not (TypeSkill.Buff or TypeSkill.FriendlySkill);

    public void Status(string owner, int characterClass, bool consumeMoney, bool paused, uint money)
    {
        if (!SameCharacter() || owner != _owner || characterClass != _ownerClass) return;
        if (paused)
        {
            CancelActions(); _phase = Phase.Idle; _epoch++; return;
        }
        // BF 51 also reports charges. A charge is never permission to restart.
        // OpenMU already sends the authoritative Zen balance separately.
        if (consumeMoney)
        {
            if (_phase is Phase.Running or Phase.Stopping) _sessionZen += money;
            return;
        }
        if (_phase == Phase.Starting && CanContinue())
        {
            _phase = Phase.Running; _ownsActions = true;
            _sessionStarted = Environment.TickCount64; _sessionStopped = 0;
            BeginAdvanced(_sessionStarted);
            _skills.ClearPending(); _scene.Hero.StopAfterCurrentTile();
            _nextTick = _nextMove = _nextPotion = _awaySince = _pickupBlockedUntil = 0;
            _dropRetry.Clear(); _monsterRetry.Clear(); _pickup = null; _target = null; _progressAt = 0;
        }
        else
        {
            // Ignore late start acknowledgments after a local stop/map change.
            Stop();
            if (_phase == Phase.Idle) { _phase = Phase.Stopping; _deadline = Environment.TickCount64 + 5000; _ = SendStateAsync(true, _epoch); }
        }
    }

    public void Stop()
    {
        if (_phase == Phase.Idle || _phase == Phase.Stopping) return;
        CancelActions(); _epoch++; _phase = Phase.Stopping;
        _deadline = Environment.TickCount64 + 5000;
        if (MuGame.Network?.IsConnected == true && SameCharacter()) _ = SendStateAsync(true, _epoch);
        else _phase = Phase.Idle;
    }

    public void Dispose()
    {
        Stop(); CancelActions(); _epoch++; _phase = Phase.Idle; _owner = null;
    }

    private void CancelActions()
    {
        EndAdvanced();
        if (_ownsActions)
        {
            _sessionStopped = Environment.TickCount64;
            _skills.ClearPending();
            _scene.Hero?.StopAfterCurrentTile();
        }
        _ownsActions = false; _target = null; _pickup = null; _dropRetry.Clear(); _monsterRetry.Clear(); _potionSending = false;
    }

    private async Task SendStateAsync(bool paused, int epoch)
    {
        var service = MuGame.Network?.GetCharacterService();
        bool sent = service != null && await service.SetMuHelperPausedAsync(paused);
        if (!sent) MuGame.ScheduleOnMainThread(() =>
        {
            if (_epoch != epoch) return;
            CancelActions(); _phase = Phase.Idle; _epoch++;
            RequestDialog.ShowInfo("MU Helper request could not be sent.");
        });
    }

    private bool CanContinue()
    {
        var network = MuGame.Network;
        var state = network?.GetCharacterState();
        return network?.IsConnected == true && SameCharacter() && state.IsInGame && state.MapId == _map
            && _scene.MuHelperWorldReady && _scene.Hero is { IsDead: false } hero
            && state.CurrentHealth > 0 && _scene.World is WalkableWorldControl world && !IsSafe(world, hero.Location)
            && !_scene.MuHelperBlockingWindow;
    }

    /// <summary>Runs before world input and queued skills so cancellation cannot emit a later queued cast.</summary>
    public void BeforeWorldUpdate()
    {
        if (_phase is Phase.Running or Phase.Starting)
        {
            var game = MuGame.Instance;
            bool worldClick = game.UiMouseState.LeftButton == ButtonState.Pressed && game.PrevUiMouseState.LeftButton == ButtonState.Released
                && !_scene.IsInteractiveUIUnderMouse();
            bool manualSkill = game.UiMouseState.RightButton == ButtonState.Pressed && game.PrevUiMouseState.RightButton == ButtonState.Released
                && !_scene.IsInteractiveUIUnderMouse();
            bool manualPickup = _scene.FocusControl is not TextFieldControl
                && game.Keyboard.IsKeyDown(Keys.Space) && game.PrevKeyboard.IsKeyUp(Keys.Space);
            if (!CanContinue() || worldClick || manualSkill || manualPickup || _scene.IsMobileJoystickCapturingMouse) Stop();
        }
        long now = Environment.TickCount64;
        if (_phase == Phase.Starting && now > _deadline)
        {
            Stop(); RequestDialog.ShowInfo("MU Helper was not started. Check the server messages and settings.");
        }
        else if (_phase == Phase.Stopping && now > _deadline)
        { CancelActions(); _phase = Phase.Idle; _epoch++; }
    }

    public void Update()
    {
        if (!Active || !CanContinue()) { if (Active) Stop(); return; }
        long now = Environment.TickCount64;
        if (now < _nextTick) return;
        // No elapsed-time accumulator: resuming a backgrounded window never replays missed actions.
        _nextTick = now + 200;
        var hero = _scene.Hero;
        var world = (WalkableWorldControl)_scene.World;
        var state = MuGame.Network.GetCharacterState();
        UsePotion(state, now);
        UpdateRaven(state);
        float hunt = Math.Clamp(_settings.Bits(2, 0, 15), 1, 8);
        float obtain = Math.Clamp(_settings.Bits(2, 4, 15), 1, 8);
        float distanceFromOrigin = Vector2.Distance(hero.Location, _origin);
        if (distanceFromOrigin > 1f)
        {
            if (_awaySince == 0) _awaySince = now;
            if (_settings.Flag(25, 4) && now - _awaySince >= Math.Max(1, _settings.Word(3)) * 1000L
                || distanceFromOrigin > Math.Max(hunt, obtain) + 2f)
            {
                _target = null; _skills.ClearPending();
                if (!Move(world, _origin, now))
                { Stop(); RequestDialog.ShowInfo("MU Helper could not return to the starting position."); }
                return;
            }
        }
        else _awaySince = 0;
        if (TrySupport(state, now)) return;
        if (TryPickup(world, obtain, now)) return;

        if (!ValidTarget(_target, hunt)) { _target = FindTarget(hunt); _progressAt = 0; }
        if (_target == null)
        {
            _comboStep = 0;
            _skills.ClearPending();
            if (!hero.IsMoving && _settings.Flag(25, 4) && distanceFromOrigin > 1f) Move(world, _origin, now);
            return;
        }
        ushort id = SelectAdvancedAttack(state, now, out int conditional);
        var skill = id == 0 ? null : state.GetSkills().FirstOrDefault(s => s.SkillId == id);
        if (id != 0 && skill == null) { Stop(); RequestDialog.ShowInfo("The MU Helper attack skill is no longer available."); return; }
        bool enoughResources = id == 0 || state.CurrentMana >= SkillDatabase.GetSkillManaCost(id)
            && state.CurrentAbility >= SkillDatabase.GetSkillAGCost(id);
        bool basic = id == 0 || !enoughResources && _settings.Flag(29, 3);
        float range = basic ? hero.GetAttackRangeTiles() : Math.Max(1, SkillDatabase.GetSkillRange(id));
        if (Vector2.Distance(hero.Location, _target.Location) > range)
        {
            if (!Approach(world, _target.Location, now))
            {
                if (_monsterRetry.Count > 256) _monsterRetry.Clear();
                _monsterRetry[_target.NetworkId] = now + 10000; _target = null; _progressAt = 0;
            }
            return;
        }
        _progressAt = 0;
        if (hero.IsMoving) { hero.StopAfterCurrentTile(); return; }
        hero.StopAfterCurrentTile(); // Also invalidate a pathfinding result still being computed.
        if (basic) hero.Attack(_target);
        else if (enoughResources && _skills.UseSkillOnMonster(skill, _target)) AdvancedAttackSent(now, conditional);
    }

    private bool ValidTarget(MonsterObject monster, float range) => monster != null && !monster.IsDead
        && !monster.IsPlayerSummon && monster.World == _scene.World && monster.Status == GameControlStatus.Ready
        && Vector2.Distance(monster.Location, _origin) <= range
        && (!_monsterRetry.TryGetValue(monster.NetworkId, out long retry) || Environment.TickCount64 >= retry)
        && _scene.World is WalkableWorldControl world && !IsSafe(world, monster.Location);

    private MonsterObject FindTarget(float range)
    {
        MonsterObject best = null; float distance = float.MaxValue;
        foreach (var obj in _scene.World.Objects.GetSnapshot())
        {
            if (obj is not MonsterObject monster || !ValidTarget(monster, range)) continue;
            float next = Vector2.DistanceSquared(monster.Location, _scene.Hero.Location);
            if (next < distance) { best = monster; distance = next; }
        }
        return best;
    }

    private static bool IsSafe(WalkableWorldControl world, Vector2 p) =>
        world.Terrain.RequestTerrainFlag((int)p.X, (int)p.Y).HasFlag(TWFlags.SafeZone);

    private bool Move(WalkableWorldControl world, Vector2 destination, long now)
    {
        if (IsSafe(world, destination) || !world.IsWalkable(destination)) return false;
        if (_progressAt == 0 || Vector2.DistanceSquared(_progressLocation, _scene.Hero.Location) > .25f)
        { _progressAt = now; _progressLocation = _scene.Hero.Location; }
        else if (now - _progressAt > 6000) return false;
        if (now < _nextMove || _scene.Hero.IsMoving && Vector2.DistanceSquared(_moveTarget, destination) < 1f) return true;
        _nextMove = now + 750; _moveTarget = destination;
        _scene.Hero.MoveTo(destination, sendToServer: true, usePathfinding: true);
        return true;
    }

    private bool Approach(WalkableWorldControl world, Vector2 target, long now)
    {
        Vector2 best = target; float distance = float.MaxValue;
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
        {
            if (x * x + y * y > 1) continue;
            var candidate = new Vector2((int)target.X + x, (int)target.Y + y);
            if (candidate.X < 0 || candidate.Y < 0 || candidate.X >= Constants.TERRAIN_SIZE || candidate.Y >= Constants.TERRAIN_SIZE
                || !world.IsWalkable(candidate) || IsSafe(world, candidate)) continue;
            float next = Vector2.DistanceSquared(candidate, _scene.Hero.Location);
            if (next < distance) { distance = next; best = candidate; }
        }
        return distance < float.MaxValue && Move(world, best, now);
    }

    private bool TryPickup(WalkableWorldControl world, float range, long now)
    {
        var state = MuGame.Network.GetCharacterState();
        var scope = MuGame.Network.GetScopeManager();
        if (_pickup.HasValue && !state.PendingPickupRawId.HasValue && now - _pickupAt > 500)
        {
            var remaining = scope.GetScopeObjectByMaskedId((ushort)(_pickup.Value & 0x7FFF));
            if (remaining != null && remaining.RawId == _pickup.Value) _pickupBlockedUntil = now + 5000;
            _pickup = null;
        }
        if (state.PendingPickupRawId.HasValue)
        {
            if (_pickup.HasValue && now - _pickupAt > 10000)
            { Stop(); RequestDialog.ShowInfo("Waiting for an inventory response. Check the connection before restarting MU Helper."); }
            return true;
        }
        if (now < _pickupBlockedUntil) return false;
        if (_dropRetry.Count > 256) _dropRetry.Clear();
        ScopeObject best = null; float distance = float.MaxValue;
        foreach (var type in new[] { ScopeObjectType.Item, ScopeObjectType.Money })
        foreach (var drop in scope.GetScopeItems(type))
        {
            if (_dropRetry.TryGetValue(drop.RawId, out long retry) && now < retry || !Wants(drop)) continue;
            var p = new Vector2(drop.PositionX, drop.PositionY);
            if (Vector2.Distance(p, _origin) > range || IsSafe(world, p)) continue;
            float next = Vector2.DistanceSquared(p, _scene.Hero.Location);
            if (next < distance) { distance = next; best = drop; }
        }
        if (best == null) return false;
        var destination = new Vector2(best.PositionX, best.PositionY);
        if (distance > 4f)
        {
            _skills.ClearPending();
            if (Approach(world, destination, now)) return true;
            _dropRetry[best.RawId] = now + 10000; _progressAt = 0; return false;
        }
        if (_scene.Hero.IsMoving) { _scene.Hero.StopAfterCurrentTile(); return true; }
        _scene.Hero.StopAfterCurrentTile();
        if (_dropRetry.Count > 256) _dropRetry.Clear();
        _dropRetry[best.RawId] = now + 10000;
        _pickup = best.RawId; _pickupAt = now; _pickupBlockedUntil = now + 750;
        _scene.TryPickupGroundItem(best.RawId);
        return true;
    }

    private bool Wants(ScopeObject drop)
    {
        if (_settings.Flag(27, 6)) return true;
        if (!_settings.Flag(27, 7)) return false;
        if (drop is MoneyScopeObject) return _settings.Flag(1, 6);
        if (drop is not ItemScopeObject item) return false;
        var definition = ItemDatabase.GetItemDefinition(item.ItemData.Span);
        if (definition == null) return false;
        string name = definition.Name ?? string.Empty;
        var details = ItemDatabase.ParseItemDetails(item.ItemData.Span);
        if (_settings.Flag(1, 3) && (definition.IsJewel() || definition.IsPackedJewel()
            || name.IndexOf("Gemstone", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
        if (_settings.Flag(1, 4) && details.IsAncient || _settings.Flag(1, 5) && details.IsExcellent) return true;
        if (_settings.Flag(1, 7)) for (int i = 0; i < 12; i++)
        {
            string filter = _settings.ItemName(i);
            if (filter.Length > 0 && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    private void UsePotion(CharacterState state, long now)
    {
        if (!_settings.Flag(25, 0) || _potionSending || now < _nextPotion || state.MaximumHealth == 0
            || (double)state.CurrentHealth * 100 >= (double)state.MaximumHealth * Math.Clamp(_settings.Bits(23, 0, 15), 1, 10) * 10) return;
        byte? slot = null; int strength = -1;
        foreach (var entry in state.GetInventoryItems())
        {
            if (entry.Key < 12) continue;
            var definition = ItemDatabase.GetItemDefinition(entry.Value);
            if (definition?.Group != 14 || definition.Id is < 0 or > 3 || definition.Id < strength
                || ItemDatabase.ParseItemDetails(entry.Value).Durability == 0) continue;
            slot = entry.Key; strength = definition.Id;
        }
        _nextPotion = now + 1500;
        if (slot.HasValue) { _potionSending = true; _ = SendPotionAsync(slot.Value, _epoch); }
    }

    private async Task SendPotionAsync(byte slot, int epoch)
    {
        bool sent = false;
        try { await MuGame.Network.GetCharacterService().SendConsumeItemRequestAsync(slot); sent = true; }
        catch { }
        MuGame.ScheduleOnMainThread(() =>
        {
            if (_epoch != epoch) return;
            _potionSending = false;
            if (sent) SoundController.Instance.PlayBuffer("Sound/pDrink.wav");
            else Stop();
        });
    }
}
