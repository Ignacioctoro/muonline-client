using System;
using System.Collections.Generic;
using System.Linq;
using Client.Data.BMD;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Player;

namespace Client.Main.Scenes;

internal sealed partial class GameSceneMuHelperController
{
    private readonly long[] _conditionalUsed = new long[2];
    private readonly Dictionary<(ushort Target, ushort Skill), long> _supportRetry = new();
    private readonly Dictionary<(ushort Target, ushort Skill), long> _supportUsed = new();
    private long _nextSupport, _comboStarted;
    private int _comboStep, _ravenMode = -1;
    private ushort _comboTarget;
    private bool _ravenOwned;

    private static ushort BaseSkill(ushort id)
    {
        var definition = SkillDatabase.GetSkillDefinition(id);
        return definition?.SkillBrand is > 0 and < 300 ? (ushort)definition.SkillBrand : id;
    }

    private static bool BuffEffect(ushort id, out byte effect)
    {
        effect = 0;
        if (SkillDatabase.GetSkillDefinition(id)?.Type is not (TypeSkill.Buff or TypeSkill.FriendlySkill)) return false;
        if (Season6BuffMapping.TryGetEffectIdFromSkillId(id, out effect)) return true;
        return BaseSkill(id) != id && Season6BuffMapping.TryGetEffectIdFromSkillId(BaseSkill(id), out effect);
    }

    private static bool ValidCombo(MuHelperConfigurationData settings, CharacterState state)
    {
        if (!settings.Flag(25, 5)) return true;
        if (((int)state.Class >> 4) != 1) return false;
        ushort first = (ushort)settings.Word(5), second = (ushort)settings.Word(7), third = (ushort)settings.Word(11);
        return new[] { first, second, third }.All(id => state.GetSkills().Any(s => s.SkillId == id))
            && BaseSkill(first) is >= 19 and <= 23 && BaseSkill(second) is 41 or 42 or 43 or 232
            && BaseSkill(third) is 41 or 42 or 43 && BaseSkill(second) != BaseSkill(third);
    }

    private void BeginAdvanced(long now)
    {
        _conditionalUsed[0] = _conditionalUsed[1] = now;
        _supportRetry.Clear(); _supportUsed.Clear(); _nextSupport = 0;
        _comboStep = 0; _comboStarted = 0; _comboTarget = 0;
        _ravenMode = -1; _ravenOwned = false;
    }

    private void EndAdvanced()
    {
        if (_ravenOwned && SameCharacter() && MuGame.Network?.IsConnected == true)
            _ = MuGame.Network.GetCharacterService().SendDarkRavenCommandAsync(0);
        _ravenOwned = false; _ravenMode = -1; _comboStep = 0;
        _supportRetry.Clear(); _supportUsed.Clear();
    }

    private void UpdateRaven(CharacterState state)
    {
        if (!_settings.Flag(26, 1) || !state.HasEquippedDarkRaven)
        {
            if (_ravenOwned && state.HasEquippedDarkRaven)
                _ = MuGame.Network.GetCharacterService().SendDarkRavenCommandAsync(0);
            _ravenOwned = false; _ravenMode = -1;
            return;
        }
        int mode = Math.Clamp(_settings.Bits(28, 0, 255), 0, 2);
        if (_ravenMode == mode) return;
        _ravenMode = mode; _ravenOwned = true;
        _ = MuGame.Network.GetCharacterService().SendDarkRavenCommandAsync((byte)mode);
    }

    private bool Resources(CharacterState state, ushort id)
    {
        var definition = SkillDatabase.GetSkillDefinition(id);
        return state.GetSkills().Any(s => s.SkillId == id) && definition != null
            && state.Level >= definition.RequiredLevel && state.TotalEnergy >= definition.RequiredEnergy
            && state.CurrentMana >= SkillDatabase.GetSkillManaCost(id) && state.CurrentAbility >= SkillDatabase.GetSkillAGCost(id);
    }

    private IEnumerable<(PartyMemberInfo Member, PlayerObject Player)> PartyTargets(CharacterState state)
    {
        foreach (var member in MuGame.Network.GetPartyManager().GetPartyMembers())
        {
            if (member.Name == state.Name || member.MapId != state.MapId) continue;
            foreach (var obj in _scene.World.Objects.GetSnapshot())
                if (obj is PlayerObject player && player != _scene.Hero && !player.IsDead && player.World == _scene.World
                    && player.Status == GameControlStatus.Ready && player.Name == member.Name)
                { yield return (member, player); break; }
        }
    }

    private bool SupportCast(CharacterState state, ushort id, PlayerObject target, long now)
    {
        if (target == null || !Resources(state, id)) return false;
        ushort targetId = target == _scene.Hero ? state.Id : target.NetworkId;
        var key = (Target: (ushort)(targetId & 0x7FFF), Skill: id);
        if (_supportRetry.TryGetValue(key, out long retry) && now < retry) return false;
        if (target != _scene.Hero && (!SkillDatabase.IsFriendlySkill(id)
            || Microsoft.Xna.Framework.Vector2.Distance(target.Location, _scene.Hero.Location) > Math.Max(1, SkillDatabase.GetSkillRange(id)))) return false;
        _supportRetry[key] = now + 3000;
        var entry = state.GetSkills().First(s => s.SkillId == id);
        if (!_skills.UseHelperSupportSkill(entry, target)) return false;
        _supportUsed[key] = now; _nextSupport = now + 600;
        if (BaseSkill(id) == 26) _supportRetry[key] = now + 600;
        _comboStep = 0;
        return true;
    }

    private bool NeedsBuff(CharacterState state, PlayerObject target, ushort id, bool periodic, long now)
    {
        if (!BuffEffect(id, out byte effect)) return false;
        ushort targetId = target == _scene.Hero ? state.Id : target.NetworkId;
        bool active = state.GetActiveBuffsForPlayer(targetId).Any(b => b.EffectId == effect
            && (!b.ExpiresAt.HasValue || b.ExpiresAt.Value > DateTime.UtcNow));
        if (!active) return true;
        if (!periodic || _settings.Word(15) == 0) return false;
        long last = _supportUsed.TryGetValue(((ushort)(targetId & 0x7FFF), id), out long used) ? used : _sessionStarted;
        return now - last >= _settings.Word(15) * 1000L;
    }

    private bool TrySupport(CharacterState state, long now)
    {
        if (now < _nextSupport || _scene.Hero.IsMoving || _scene.Hero.IsAttackOrSkillAnimationPlaying()) return false;
        if (_supportRetry.Count > 128) { _supportRetry.Clear(); _supportUsed.Clear(); }
        var heal = state.GetSkills().FirstOrDefault(s => BaseSkill(s.SkillId) == 26);
        if (_settings.Flag(25, 1) && heal != null && state.MaximumHealth > 0
            && (double)state.CurrentHealth * 100 < (double)state.MaximumHealth * Math.Clamp(_settings.Bits(23, 4, 15), 1, 10) * 10
            && SupportCast(state, heal.SkillId, _scene.Hero, now)) return true;
        if (_settings.Flag(25, 6) && _settings.Flag(25, 7) && heal != null)
        {
            foreach (var pair in PartyTargets(state))
                if (pair.Member.MaximumHealth > 0 && pair.Member.CurrentHealth > 0
                    && (double)pair.Member.CurrentHealth * 100 < (double)pair.Member.MaximumHealth * Math.Clamp(_settings.Bits(24, 0, 15), 1, 10) * 10
                    && SupportCast(state, heal.SkillId, pair.Player, now)) return true;
        }
        if (!_settings.Flag(26, 2) || _comboStep != 0) return false;
        for (int slot = 0; slot < 3; slot++)
        {
            ushort id = (ushort)_settings.Word(17 + slot * 2);
            if (id == 0 || !BuffEffect(id, out _)) continue;
            if (NeedsBuff(state, _scene.Hero, id, true, now) && SupportCast(state, id, _scene.Hero, now)) return true;
            if (!_settings.Flag(25, 6) || !SkillDatabase.IsFriendlySkill(id)) continue;
            foreach (var pair in PartyTargets(state))
                if (NeedsBuff(state, pair.Player, id, _settings.Flag(26, 0), now)
                    && SupportCast(state, id, pair.Player, now)) return true;
        }
        return false;
    }

    private ushort SelectAdvancedAttack(CharacterState state, long now, out int conditional)
    {
        conditional = -1;
        if (_settings.Flag(25, 5))
        {
            if (_comboTarget != _target.NetworkId || _comboStep > 0 && now - _comboStarted > 2800)
                _comboStep = 0;
            _comboTarget = _target.NetworkId;
            return (ushort)_settings.Word(_comboStep == 0 ? 5 : _comboStep == 1 ? 7 : 11);
        }
        // Drain Life is recovery, not a permanent buff.
        if (_settings.Flag(25, 2) && state.MaximumHealth > 0
            && (double)state.CurrentHealth * 100 < (double)state.MaximumHealth * Math.Clamp(_settings.Bits(24, 4, 15), 1, 10) * 10)
        {
            var drain = state.GetSkills().FirstOrDefault(s => BaseSkill(s.SkillId) == 214);
            if (drain != null && Resources(state, drain.SkillId)) return drain.SkillId;
        }
        for (int slot = 0; slot < 2; slot++)
        {
            ushort id = (ushort)_settings.Word(slot == 0 ? 7 : 11);
            if (!RepeatableAttack(id) || !Resources(state, id)) continue;
            int flags = slot == 0 ? 26 : 27, shift = slot == 0 ? 3 : 0;
            bool timer = _settings.Flag(flags, shift), condition = _settings.Flag(flags, shift + 1);
            bool due;
            if (timer) // Legacy configurations with both bits follow the original client's timer precedence.
            {
                int seconds = _settings.Word(slot == 0 ? 9 : 13);
                due = seconds > 0 && now - _conditionalUsed[slot] >= seconds * 1000L;
            }
            else due = condition && MonsterCount(_settings.Flag(flags, shift + 2), now)
                >= _settings.Bits(flags, shift + 3, 3) + 2;
            if (due) { conditional = slot; return id; }
        }
        return (ushort)_settings.Word(5);
    }

    private int MonsterCount(bool attacking, long now)
    {
        int count = 0;
        ushort self = (ushort)(MuGame.Network.GetCharacterState().Id & 0x7FFF);
        float range = Math.Clamp(_settings.Bits(2, 0, 15), 1, 8);
        foreach (var obj in _scene.World.Objects.GetSnapshot())
            if (obj is MonsterObject monster && ValidTarget(monster, range)
                && (!attacking || (monster.LastAttackTargetId & 0x7FFF) == self && now - monster.LastAttackAtMs < 8000)) count++;
        return count;
    }

    private void AdvancedAttackSent(long now, int conditional)
    {
        if (conditional >= 0) _conditionalUsed[conditional] = now;
        if (!_settings.Flag(25, 5)) return;
        if (_comboStep == 0) _comboStarted = now;
        _comboStep = (_comboStep + 1) % 3;
    }

    public bool TryAcceptPartyInvite(ushort requester, string name)
    {
        if (!Active || !CanContinue() || !_settings.Flag(25, 6)) return false;
        if (!MuGame.Network.GetScopeManager().TryGetScopeObjectName(requester, out string actual) || actual != name) return false;
        var service = MuGame.Network.GetCharacterService();
        bool friend = _settings.Flag(29, 1) && service.Messenger.Initialized && service.Messenger.Friends.Any(f => f.Name == name);
        var roster = Client.Main.Networking.GuildInfoCache.CurrentRoster;
        bool guild = _settings.Flag(29, 2) && roster is { IsInGuild: true } && roster.FindMember(name) != null;
        if (!friend && !guild) return false;
        _ = service.SendPartyResponseAsync(true, requester);
        return true;
    }
}
