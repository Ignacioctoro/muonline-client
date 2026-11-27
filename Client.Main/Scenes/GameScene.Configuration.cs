using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Scenes;

public partial class GameScene
{
    private static readonly Keys[] ConfigurationItemKeys = { Keys.Q, Keys.W, Keys.E, Keys.R };
    private int _hudSession = -1, _hudRevision = -1, _hudSkillRevision = -1;
    private bool _applyingHud, _hudDirty, _hudSaving;
    private byte[] _hudBaseline, _hudRestore;
    private long _nextHudCheck;

    private void MarkHudConfigurationChanged()
    {
        if (!_applyingHud && _hudSession >= 0)
        { _hudDirty = true; _hudRestore = null; _nextHudCheck = 0; }
    }

    private void UpdateHudConfiguration()
    {
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || !state.IsInGame || _skillSelectionPanel == null || _inventoryControl == null) return;
        if (_hudSession != state.SkillSessionRevision)
        {
            _hudSession = state.SkillSessionRevision; _hudRevision = -1; _hudSkillRevision = -1;
            _hudRestore = null; _hudDirty = false;
            _applyingHud = true;
            foreach (var key in ConfigurationItemKeys) _inventoryControl.ClearItemHotkey(key);
            _applyingHud = false;
            _hudBaseline = CaptureHudConfiguration(null);
        }
        int revision = state.KeyConfigurationRevision;
        if (revision != _hudRevision)
        {
            _hudRevision = revision;
            var data = state.GetKeyConfiguration();
            if (data != null && data.Length >= 30 && !_hudDirty)
            { _hudRestore = data; _hudSkillRevision = -1; }
        }
        if (_hudRestore != null && _hudSkillRevision != state.SkillListRevision)
        {
            _hudSkillRevision = state.SkillListRevision;
            ApplyHudConfiguration(_hudRestore, state);
            _hudBaseline = CaptureHudConfiguration(_hudRestore);
        }
        long now = Environment.TickCount64;
        if (now < _nextHudCheck || _hudSaving) return;
        _nextHudCheck = now + 250;
        var snapshot = CaptureHudConfiguration(_hudBaseline);
        if (!_hudDirty && _hudBaseline != null && snapshot.AsSpan().SequenceEqual(_hudBaseline)) return;
        _hudRestore = null; _hudDirty = true;
        _ = SaveHudConfigurationAsync(snapshot, state.Name, _hudSession);
    }

    private async Task SaveHudConfigurationAsync(byte[] snapshot, string owner, int session)
    {
        _hudSaving = true;
        bool sent = await MuGame.Network.GetCharacterService().SaveHudConfigurationAsync(snapshot, owner, session);
        MuGame.ScheduleOnMainThread(() =>
        {
            _hudSaving = false;
            if (_hudSession != session) return;
            if (sent)
            {
                _hudBaseline = snapshot; _hudDirty = false;
                var state = MuGame.Network?.GetCharacterState();
                if (state?.Name == owner && state.SkillSessionRevision == session)
                {
                    state.ReceiveKeyConfiguration(snapshot);
                    _hudRevision = state.KeyConfigurationRevision;
                }
            }
            else _nextHudCheck = Environment.TickCount64 + 2000;
        });
    }

    private byte[] CaptureHudConfiguration(byte[] previous)
    {
        // First 30 bytes match the original S6 option packet. The versioned tail
        // preserves the HUD bank and consumables outside item group 14.
        var data = new byte[51];
        if (previous?.Length >= 30) previous.AsSpan(0, 30).CopyTo(data);
        for (int i = 0; i < 10; i++)
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(i * 2, 2), _main.SkillHotkeys.GetSkill(i)?.SkillId ?? ushort.MaxValue);
        data[30] = (byte)'M'; data[31] = (byte)'G'; data[32] = (byte)'H'; data[33] = 1;
        data[34] = (byte)_main.SkillHotkeyBank;
        for (int i = 0; i < 4; i++)
        {
            bool assigned = _inventoryControl.TryGetItemHotkey(ConfigurationItemKeys[i], out int group, out int id, out int level, out _);
            int at = 35 + i * 4;
            data[at] = assigned ? (byte)group : byte.MaxValue;
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(at + 1, 2), assigned ? (ushort)id : ushort.MaxValue);
            data[at + 3] = (byte)level;
            data[i < 3 ? 21 + i : 25] = assigned && group == 14 && id < 255 ? (byte)id : byte.MaxValue;
            data[26 + 3 - i] = (byte)level; // Little-endian QWERLevel: Q is its high byte.
        }
        return data;
    }

    private void ApplyHudConfiguration(byte[] data, CharacterState state)
    {
        _applyingHud = true;
        try
        {
            var skills = state.GetSelectableSkills().ToArray();
            for (int i = 0; i < 10; i++)
            {
                ushort id = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(i * 2, 2));
                var skill = skills.FirstOrDefault(s => s.SkillId == id);
                if (skill != null) _skillSelectionPanel.AssignSkillToQuickSlot(i, skill);
                else _skillSelectionPanel.ClearQuickSlot(i);
            }
            bool extended = data.Length >= 51 && data[30] == 'M' && data[31] == 'G' && data[32] == 'H' && data[33] == 1;
            int bank = extended && data[34] == 1 ? 1 : 0;
            _main.SetSkillHotkeyBank(bank); _skillSelectionPanel.SetQuickSlotBank(bank);
            for (int i = 0; i < 4; i++)
            {
                int at = 35 + i * 4;
                int group = extended ? data[at] : 14;
                int id = extended ? BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at + 1, 2)) : data[i < 3 ? 21 + i : 25];
                int level = extended ? data[at + 3] : data[26 + 3 - i];
                _inventoryControl.RestoreItemHotkey(ConfigurationItemKeys[i], group, id, level);
            }
        }
        finally { _applyingHud = false; }
    }
}
