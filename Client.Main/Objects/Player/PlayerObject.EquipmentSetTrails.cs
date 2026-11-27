using System;
using System.Collections.Generic;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Objects.Player
{
    public partial class PlayerObject
    {
        private ClassicFxRuntime _equipmentSetTrailRuntime;
        private readonly List<ClassicFxHandle> _equipmentSetTrails = new(32);
        private int _equipmentSetLevel;
        private int _equipmentSetNumber = -1;
        private double _equipmentSetTickPhase;
        private int _equipmentSetTick;

        // Original CheckFullSet compares armor item numbers, not Excellent or
        // Ancient flags. The minimum level selects the common-set effects.
        private int GetEquipmentSetLevel(out int setNumber)
        {
            setNumber = Boots?.ItemNumber ?? -1;
            if (setNumber < 0) return 0;
            int minimum = 15;
            bool Check(ModelObject part, int group, int number, ref int level)
            {
                if (part == null || part.Model == null || part.ItemGroup != group ||
                    part.ItemNumber != number || part.ItemLevel < 9) return false;
                level = Math.Min(level, part.ItemLevel);
                return true;
            }
            if (!Check(Boots, 11, setNumber, ref minimum) ||
                !Check(Pants, 9, setNumber, ref minimum) ||
                !Check(Armor, 8, setNumber, ref minimum)) return 0;
            bool magicGladiator = CharacterClass is CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster;
            if (!magicGladiator && !Check(Helm, 7, setNumber, ref minimum)) return 0;
            if (!IsRageFighterClass(CharacterClass) && !Check(Gloves, 10, setNumber, ref minimum)) return 0;
            return minimum;
        }

        private void ClearEquipmentSetTrails()
        {
            if (_equipmentSetTrailRuntime != null && !_equipmentSetTrailRuntime.IsDisposed)
                foreach (var handle in _equipmentSetTrails) _equipmentSetTrailRuntime.ReleaseJoint(handle);
            _equipmentSetTrails.Clear();
            _equipmentSetLevel = 0;
            _equipmentSetNumber = -1;
            _equipmentSetTickPhase = 0;
            _equipmentSetTick = 0;
        }

        private void UpdateEquipmentSetTrails(GameTime gameTime)
        {
            var fx = World?.ClassicFx;
            if (!ReferenceEquals(fx, _equipmentSetTrailRuntime))
            {
                ClearEquipmentSetTrails();
                _equipmentSetTrailRuntime = fx;
            }
            int level = GetEquipmentSetLevel(out int number);
            double seconds = gameTime.ElapsedGameTime.TotalSeconds;
            if (level < 11 || !Visible || IsDead || TotalAlpha <= 0.01f ||
                fx == null || fx.IsDisposed || !fx.Enabled || IsChaosCastleMap(World.WorldIndex) ||
                MuGame.Instance?.IsActive != true || seconds > 0.25)
            {
                ClearEquipmentSetTrails();
                return;
            }
            if (_equipmentSetLevel != level || _equipmentSetNumber != number)
            {
                ClearEquipmentSetTrails();
                _equipmentSetLevel = level;
                _equipmentSetNumber = number;
            }
            for (int i = _equipmentSetTrails.Count - 1; i >= 0; i--)
                if (!fx.TryGetJoint(_equipmentSetTrails[i], out _)) _equipmentSetTrails.RemoveAt(i);

            // CreateJointSync/checkNormalizer limits random emission to native
            // reference frames. No replay loop or burst accumulation on resume.
            if (seconds <= 0) return;
            _equipmentSetTickPhase += Math.Min(1.0, seconds * ClassicFxClock.ReferenceFps);
            if (_equipmentSetTickPhase < 1.0 - 0.0000001) return;
            _equipmentSetTickPhase = Math.Max(0.0, _equipmentSetTickPhase - 1.0);
            _equipmentSetTick++;
            if (Random.Shared.Next(20) != 0) return;

            // ZzzCharacter.cpp: EquipmentLevelSet > 9; the existing per-piece
            // +15 linked models remain separate. Port only the missing joints.
            if (level == 11)
            {
                if (Random.Shared.Next(8) == 0) EmitEquipmentSetTrail(fx, 0, 0);
                return;
            }
            if (_equipmentSetTick % 6 == 0)
            {
                EmitEquipmentSetTrail(fx, 18, 0);
                EmitEquipmentSetTrail(fx, 18, 1);
            }
            if (level >= 13 && _equipmentSetTick % 4 == 0)
                EmitEquipmentSetTrail(fx, 7, 1, plus: true);
        }

        private void EmitEquipmentSetTrail(ClassicFxRuntime fx, int subtype, ushort skill, bool plus = false)
        {
            var handle = fx.CreateEquipmentSetTrail(ClassicFxOwner.FromWorldObject(this),
                new Vector3(MathHelper.ToDegrees(Angle.X), MathHelper.ToDegrees(Angle.Y), MathHelper.ToDegrees(Angle.Z)),
                subtype, skill, plus);
            if (!handle.IsValid) return;
            if (_equipmentSetTrails.Count >= 32)
            {
                fx.ReleaseJoint(_equipmentSetTrails[0]);
                _equipmentSetTrails.RemoveAt(0);
            }
            _equipmentSetTrails.Add(handle);
        }
    }
}
