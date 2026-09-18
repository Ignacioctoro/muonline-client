using Client.Main.Controls.UI;
using Client.Main.Core.Utilities;
using Client.Main.Scenes;
using Client.Main.Objects.Effects;
using Client.Main.Objects.Player;
using Client.Main.Controls;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client.Main.Networking.PacketHandling.Handlers
{
    /// <summary>
    /// Implements IGamePacketHandler to process server and chat messages.
    /// </summary>
    public class ChatMessageHandler : IGamePacketHandler
    {
        private readonly ILogger<ChatMessageHandler> _logger;

        private static readonly List<(ServerMessage.MessageType Type, string Message)>
            _pendingServerMessages = new();

        private static readonly object _pendingServerMessagesLock = new();

        private static readonly Regex _leadingZerosRegex = new Regex(
            @"^0+(?=\S)",
            RegexOptions.Compiled);

        private static readonly object _lastMsgLock = new();

        private static (DateTime Time, string Text) _lastBlueSystemMessage;

        public ChatMessageHandler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ChatMessageHandler>();
        }

        // ───────────────────── SERVER MESSAGE ─────────────────────

        [PacketHandler(0x0D, PacketRouter.NoSubCode)]
        public Task HandleServerMessageAsync(Memory<byte> packet)
        {
            var serverMsg = new ServerMessage(packet);

            string original = serverMsg.Message;
            string cleaned = _leadingZerosRegex.Replace(original, "");

            var scene = MuGame.Instance?.ActiveScene as GameScene;

            if (scene == null)
            {
                _logger.LogWarning(
                    "GameScene is null when handling ServerMessage (0x0D). Queuing message: Type={Type}, Content='{Message}'",
                    serverMsg.Type,
                    cleaned);

                lock (_pendingServerMessagesLock)
                {
                    _pendingServerMessages.Add(
                        (serverMsg.Type, cleaned));
                }

                return Task.CompletedTask;
            }

            try
            {
                _logger.LogInformation(
                    "Received ServerMessage (0x0D): Type={Type}, Original='{Original}', Cleaned='{Cleaned}'",
                    serverMsg.Type,
                    original,
                    cleaned);

                if (serverMsg.Type == ServerMessage.MessageType.BlueNormal &&
                    !string.IsNullOrWhiteSpace(cleaned))
                {
                    lock (_lastMsgLock)
                    {
                        _lastBlueSystemMessage =
                            (DateTime.UtcNow, cleaned);
                    }

                    var characterState =
                        MuGame.Network?.GetCharacterState();

                    if (characterState != null &&
                        characterState.LastNpcNetworkId != 0)
                    {
                        ushort npcId =
                            characterState.LastNpcNetworkId;

                        ushort npcType =
                            characterState.LastNpcTypeNumber;

                        MuGame.ScheduleOnMainThread(() =>
                        {
                            if (scene?.World == null)
                            {
                                return;
                            }

                            var world = scene.World;

                            if (world.TryGetWalkerById(
                                    npcId,
                                    out var npc) &&
                                npc != null)
                            {
                                string npcName =
                                    NpcDatabase.GetNpcName(npcType);

                                var bubble =
                                    new ChatBubbleObject(
                                        cleaned,
                                        npcId,
                                        npcName);

                                world.Objects.Add(bubble);

                                _logger.LogDebug(
                                    "Created chat bubble for NPC {NpcId} ({NpcName}): '{Message}'",
                                    npcId,
                                    npcName,
                                    cleaned);
                            }
                        });
                    }
                }

                if (serverMsg.Type != ServerMessage.MessageType.BlueNormal)
                {
                    scene.ShowNotificationMessage(
                        serverMsg.Type,
                        cleaned);
                }

                string prefix =
                    serverMsg.Type switch
                    {
                        ServerMessage.MessageType.GoldenCenter
                            => "[GOLDEN]: ",

                        ServerMessage.MessageType.BlueNormal
                            => "[SYSTEM]: ",

                        ServerMessage.MessageType.GuildNotice
                            => "[GUILD_NOTICE]: ",

                        _ => "[SERVER]: "
                    };

                Console.WriteLine(
                    $"{prefix}{cleaned}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing ServerMessage (0x0D).");
            }

            return Task.CompletedTask;
        }

        // ───────────────────── OBJECT MESSAGE ─────────────────────

        [PacketHandler(0x01, PacketRouter.NoSubCode)]
        public Task HandleObjectMessageAsync(
            Memory<byte> packet)
        {
            try
            {
                var msg =
                    new ObjectMessage(packet);

                ushort targetId =
                    (ushort)(msg.ObjectId & 0x7FFF);

                string text =
                    msg.Message ?? string.Empty;

                _logger.LogInformation(
                    "Received ObjectMessage (0x01): Target={TargetId:X4}, Text='{Text}'",
                    targetId,
                    text);

                MuGame.ScheduleOnMainThread(() =>
                {
                    var scene =
                        MuGame.Instance?.ActiveScene as GameScene;

                    if (scene?.World == null)
                    {
                        return;
                    }

                    var world = scene.World;

                    if (world.TryGetWalkerById(
                            targetId,
                            out var target) &&
                        target != null)
                    {
                        var bubble =
                            new ChatBubbleObject(
                                text,
                                targetId,
                                target.DisplayName);

                        world.Objects.Add(bubble);
                    }

                    scene.ChatLog?.AddMessage(
                        "System",
                        text,
                        Client.Main.Models.MessageType.System);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error parsing ObjectMessage (0x01).");
            }

            return Task.CompletedTask;
        }

        // ───────────────────── NORMAL CHAT 0x00 ─────────────────────

        [PacketHandler(0x00, PacketRouter.NoSubCode)]
        public Task HandleChatMessageAsync(
            Memory<byte> packet)
        {
            return HandleChatPacketAsync(
                packet,
                forceWhisper: false);
        }

        // ───────────────────── WHISPER 0x02 ─────────────────────
        //
        // IMPORTANTE:
        // Si entra por 0x02, se considera Whisper SIEMPRE.
        // No dependemos de chatMsg.Type.

        [PacketHandler(0x02, PacketRouter.NoSubCode)]
        public Task HandleWhisperMessageAsync(
            Memory<byte> packet)
        {
            return HandleChatPacketAsync(
                packet,
                forceWhisper: true);
        }

        // ───────────────────── SHARED CHAT PARSER ─────────────────────

        private Task HandleChatPacketAsync(
            Memory<byte> packet,
            bool forceWhisper)
        {
            var scene =
                MuGame.Instance?.ActiveScene as GameScene;

            if (scene == null)
            {
                _logger.LogWarning(
                    "GameScene is null when handling ChatMessage.");

                return Task.CompletedTask;
            }

            try
            {
                var chatMsg =
                    new ChatMessage(packet);

                bool isWhisper =
                    forceWhisper ||
                    chatMsg.Type ==
                    ChatMessage.ChatMessageType.Whisper;

                _logger.LogInformation(
                    "Received ChatMessage: From={Sender}, PacketWhisper={ForceWhisper}, ParsedType={ParsedType}, EffectiveWhisper={IsWhisper}, Message='{Message}'",
                    chatMsg.Sender,
                    forceWhisper,
                    chatMsg.Type,
                    isWhisper,
                    chatMsg.Message);

                string sender =
                    chatMsg.Sender ?? string.Empty;

                string rawText =
                    chatMsg.Message ?? string.Empty;

                var dispatch =
                    new ChatDispatch(
                        scene,
                        sender,
                        rawText,
                        isWhisper);

                MuGame.ScheduleOnMainThread(
                    ProcessChatOnMainThread,
                    dispatch);

                if (isWhisper)
                {
                    Console.WriteLine(
                        $"Whisper [{sender}]: {rawText}");
                }
                else
                {
                    Console.WriteLine(
                        $"[{sender}]: {rawText}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error parsing ChatMessage.");
            }

            return Task.CompletedTask;
        }

        // ───────────────────── UI PROCESSING ─────────────────────

        private void ProcessChatOnMainThread(
            ChatDispatch dispatch)
        {
            var scene =
                dispatch.Scene;

            if (scene == null)
            {
                return;
            }

            var chatLog =
                scene.ChatLog;

            if (chatLog == null)
            {
                _logger.LogWarning(
                    "ChatLogWindow not found for ChatMessage from {Sender}.",
                    dispatch.Sender);

                return;
            }

            string rawText =
                dispatch.RawText;

            string display =
                rawText;

            Models.MessageType uiType;

            // ───────────────── WHISPER ─────────────────
            //
            // Esto ahora depende de IsWhisper,
            // que para un paquete 0x02 siempre será TRUE.

            if (dispatch.IsWhisper)
            {
                uiType =
                    Models.MessageType.Whisper;
            }

            // ───────────────── PARTY ─────────────────

            else if (rawText.StartsWith("~"))
            {
                uiType =
                    Models.MessageType.Party;

                display =
                    rawText[1..].TrimStart();
            }

            // ───────────────── ALLIANCE ─────────────────

            else if (rawText.StartsWith("@@"))
            {
                uiType =
                    Models.MessageType.Union;

                display =
                    rawText[2..].TrimStart();
            }

            // ───────────────── GUILD ─────────────────

            else if (rawText.StartsWith("@"))
            {
                uiType =
                    Models.MessageType.Guild;

                display =
                    rawText[1..].TrimStart();
            }

            // ───────────────── GENS ─────────────────

            else if (rawText.StartsWith("$"))
            {
                uiType =
                    Models.MessageType.Gens;

                display =
                    rawText[1..].TrimStart();
            }

            // ───────────────── NORMAL ─────────────────

            else
            {
                uiType =
                    Models.MessageType.Chat;
            }

            _logger.LogInformation(
                "Chat UI: Sender={Sender}, IsWhisper={IsWhisper}, UiType={UiType}, Text='{Text}'",
                dispatch.Sender,
                dispatch.IsWhisper,
                uiType,
                display);

            chatLog.AddMessage(
                dispatch.Sender,
                display,
                uiType);

            // =========================================================
            // WHISPERS NEVER CREATE A WORLD CHAT BUBBLE
            // =========================================================

            if (dispatch.IsWhisper)
            {
                return;
            }

            // ───────────────── WORLD CHAT BUBBLE ─────────────────

            if (scene.World is WalkableWorldControl world)
            {
                PlayerObject player =
                    null;

                var players =
                    world.Players;

                for (int i = 0;
                     i < players.Count;
                     i++)
                {
                    var candidate =
                        players[i];

                    if (candidate != null &&
                        string.Equals(
                            candidate.Name,
                            dispatch.Sender,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        player =
                            candidate;

                        break;
                    }
                }

                if (player == null &&
                    world.Walker is PlayerObject hero &&
                    string.Equals(
                        hero.Name,
                        dispatch.Sender,
                        StringComparison.OrdinalIgnoreCase))
                {
                    player =
                        hero;
                }

                if (player != null)
                {
                    ChatBubbleObject existingBubble =
                        null;

                    var objects =
                        world.Objects.GetSnapshot();

                    for (int i = 0;
                         i < objects.Count;
                         i++)
                    {
                        if (objects[i] is ChatBubbleObject bubble &&
                            bubble.TargetId ==
                            player.NetworkId)
                        {
                            existingBubble =
                                bubble;

                            break;
                        }
                    }

                    if (existingBubble != null)
                    {
                        existingBubble.AppendMessage(
                            display);
                    }
                    else
                    {
                        var bubble =
                            new ChatBubbleObject(
                                display,
                                player.NetworkId,
                                dispatch.Sender);

                        world.Objects.Add(
                            bubble);
                    }
                }
            }
        }

        // ───────────────────────── STATIC API ─────────────────────────

        public static List<(ServerMessage.MessageType Type, string Message)>
            TakePendingServerMessages()
        {
            lock (_pendingServerMessagesLock)
            {
                var copy =
                    new List<
                        (ServerMessage.MessageType, string)>(
                        _pendingServerMessages);

                _pendingServerMessages.Clear();

                return copy;
            }
        }

        public static string TryGetRecentBlueSystemMessage(
            int maxAgeMs = 1500)
        {
            lock (_lastMsgLock)
            {
                if (string.IsNullOrEmpty(
                        _lastBlueSystemMessage.Text))
                {
                    return null;
                }

                if ((DateTime.UtcNow -
                     _lastBlueSystemMessage.Time)
                    .TotalMilliseconds <= maxAgeMs)
                {
                    return _lastBlueSystemMessage.Text;
                }

                return null;
            }
        }

        // ───────────────────────── INTERNAL DATA ─────────────────────────

        private sealed class ChatDispatch
        {
            public GameScene Scene { get; }

            public string Sender { get; }

            public string RawText { get; }

            public bool IsWhisper { get; }

            public ChatDispatch(
                GameScene scene,
                string sender,
                string rawText,
                bool isWhisper)
            {
                Scene =
                    scene;

                Sender =
                    sender;

                RawText =
                    rawText;

                IsWhisper =
                    isWhisper;
            }
        }
    }
}