using Client.Main.Controls.UI;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Networking.Services;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Main.Networking.PacketHandling.Handlers
{
    public class PartyHandler : IGamePacketHandler
    {
        private readonly ILogger<PartyHandler> _logger;
        private readonly PartyManager _partyManager;
        private readonly CharacterService _characterService;
        private readonly ScopeManager _scopeManager;
        private readonly CharacterState _characterState;

        public PartyHandler(ILoggerFactory loggerFactory, PartyManager partyManager, CharacterService characterService, ScopeManager scopeManager, CharacterState characterState)
        {
            _logger = loggerFactory.CreateLogger<PartyHandler>();
            _partyManager = partyManager;
            _characterService = characterService;
            _scopeManager = scopeManager;
            _characterState = characterState;
        }

        [PacketHandler(0x40, PacketRouter.NoSubCode)]
        public Task HandlePartyRequestAsync(Memory<byte> packet)
        {
            var request = new PartyRequest(packet);
            ushort requesterId = request.RequesterId;
            if (!_scopeManager.TryGetScopeObjectName(requesterId, out string requesterName))
            {
                requesterName = $"Player (ID: {requesterId & 0x7FFF})";
            }
            _logger.LogInformation("Received party request from {Name} ({Id}).", requesterName, requesterId);

            MuGame.ScheduleOnMainThread(() =>
            {
                RequestDialog.Show(
                    $"{requesterName} has invited you to a party.",
                    onAccept: () =>
                    {
                        _ = _characterService.SendPartyResponseAsync(true, requesterId);
                        _logger.LogInformation("Accepted party invite from {Name} ({Id}).", requesterName, requesterId);
                    },
                    onReject: () =>
                    {
                        _ = _characterService.SendPartyResponseAsync(false, requesterId);
                        _logger.LogInformation("Rejected party invite from {Name} ({Id}).", requesterName, requesterId);
                    }
                );
            });
            return Task.CompletedTask;
        }

        [PacketHandler(0x42, PacketRouter.NoSubCode)]
        public Task HandlePartyListAsync(Memory<byte> packet)
        {
            _logger.LogInformation("Received party list.");
            var partyListPacket = new PartyList(packet);
            var members = new List<PartyMemberInfo>();
            for (int i = 0; i < partyListPacket.Count; i++)
            {
                var memberData = partyListPacket[i];

                ushort memberId = 0;
                var scopeObject = _scopeManager.GetScopeItems(ScopeObjectType.Player)
                                               .OfType<PlayerScopeObject>()
                                               .FirstOrDefault(p => p.Name == memberData.Name);
                if (scopeObject != null)
                {
                    memberId = scopeObject.Id;
                }
                else if (_characterState?.Name == memberData.Name)
                {
                    memberId = _characterState.Id;
                }

                if (memberId == 0)
                {
                    _logger.LogWarning("Could not find an ID for party member '{Name}'. They might be out of scope.", memberData.Name);
                }

                members.Add(new PartyMemberInfo
                {
                    Id = memberId,
                    Index = memberData.Index,
                    Name = memberData.Name,
                    MapId = memberData.MapId,
                    PositionX = memberData.PositionX,
                    PositionY = memberData.PositionY,
                    CurrentHealth = memberData.CurrentHealth,
                    MaximumHealth = memberData.MaximumHealth
                });
            }
            _partyManager.UpdatePartyList(members);
            return Task.CompletedTask;
        }

        [PacketHandler(0x43, PacketRouter.NoSubCode)]
        public Task HandlePartyMemberKickedAsync(Memory<byte> packet)
        {
            var kickedPacket = new RemovePartyMember(packet);
            _logger.LogInformation("Party member at index {Index} removed.", kickedPacket.Index);
            _partyManager.RemoveMember(kickedPacket.Index);
            return Task.CompletedTask;
        }

        [PacketHandler(0x44, PacketRouter.NoSubCode)]
        public Task HandlePartyHealthUpdateAsync(Memory<byte> packet)
        {
            // S6: 4 + count bytes. Extended: 4 + count * 17 bytes, all uints little endian.
            // Validate the complete payload before changing any member state.
            var data = packet.Span;
            if (data.Length < 4 || data[0] != 0xC1 || data[1] != data.Length) return Task.CompletedTask;
            int count = data[3];
            if (count > 0 && data.Length == 4 + count * 17)
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = data.Slice(4 + i * 17, 17);
                    _partyManager.UpdateMemberVitals(entry[0],
                        BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(1)),
                        BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(5)),
                        BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(9)),
                        BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(13)));
                }
                return Task.CompletedTask;
            }
            if (data.Length != 4 + count) return Task.CompletedTask;
            var healthUpdate = new PartyHealthUpdate(packet);
            for (int i = 0; i < healthUpdate.Count; i++)
            {
                var memberHealth = healthUpdate[i];
                float healthPercentage = memberHealth.Value / 10.0f;
                _partyManager.UpdateMemberHealth(memberHealth.Index, healthPercentage);
            }
            return Task.CompletedTask;
        }
    }
}