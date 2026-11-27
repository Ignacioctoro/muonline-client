using System;
using System.Linq;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game
{
    public partial class ChaosMixControl
    {
        private const string SocketAtlasPath = "Interface/GFx/SocketMix_I1.ozd";
        private Texture2D _socketAtlas; // Shared TextureLoader-owned asset.
        private int _selectedSocket = -1;
        private byte _socketPreviewMix;
        private byte _socketPreviewRate;
        private uint _socketPreviewPrice;

        private bool IsSocketMix => _mixInventoryType is MixInventoryType.ExtractSeed
            or MixInventoryType.SeedSphere or MixInventoryType.AttachSocket or MixInventoryType.DetachSocket;
        private bool UsesCompactLayout => IsTrainerMix || IsSocketMix;
        private bool IsSocketSelection => _mixInventoryType is MixInventoryType.AttachSocket or MixInventoryType.DetachSocket;
        private int SocketMixId => _mixInventoryType switch
        {
            MixInventoryType.ExtractSeed => 42,
            MixInventoryType.SeedSphere => 43,
            MixInventoryType.AttachSocket => 44,
            MixInventoryType.DetachSocket => 45,
            _ => 0,
        };
        private string SocketOperationTitle => _mixInventoryType switch
        {
            MixInventoryType.ExtractSeed => "Seed Extraction",
            MixInventoryType.SeedSphere => "Seed Sphere Assembly",
            MixInventoryType.AttachSocket => "Mount Seed Sphere",
            _ => "Remove Seed Sphere",
        };
        private string SocketActionText => _mixInventoryType switch
        {
            MixInventoryType.ExtractSeed => "EXTRACT",
            MixInventoryType.SeedSphere => "ASSEMBLE",
            MixInventoryType.AttachSocket => "MOUNT",
            _ => "REMOVE",
        };

        // Called only after OpenMU acknowledges the NPC conversation.
        public void ShowSocketSelection(bool researcher)
        {
            RequestDialog.ShowChoice(researcher ? "Seed Researcher" : "Seed Master",
                () => OpenMode(researcher ? MixInventoryType.AttachSocket : MixInventoryType.ExtractSeed),
                () => OpenMode(researcher ? MixInventoryType.DetachSocket : MixInventoryType.SeedSphere),
                () => { _ = MuGame.Network?.GetCharacterService()?.SendCloseNpcRequestAsync(); },
                researcher ? "Mount Seed Sphere" : "Seed Extraction",
                researcher ? "Remove Seed Sphere" : "Seed Sphere Assembly");
        }

        private void ResetSocketPreview()
        {
            _socketPreviewMix = 0;
            _socketPreviewRate = 0;
            _socketPreviewPrice = 0;
        }

        public void UpdateSocketPreview(byte mixId, byte successRate, uint price)
        {
            if (!Visible || !IsSocketMix) return;
            _socketPreviewMix = mixId;
            _socketPreviewRate = successRate;
            _socketPreviewPrice = price;
        }

        private InventoryItem SocketEquipment => _items.FirstOrDefault(i => i.Details.HasSockets && i.Details.SocketCount > 0);

        private bool CanSelectSocket(int index)
        {
            var equipment = SocketEquipment;
            if (equipment == null || index < 0 || index >= Math.Min(5, (int)equipment.Details.SocketCount)) return false;
            var options = equipment.Details.SocketOptions;
            if (options == null || index >= options.Length || options[index] == 0xFF) return false;
            bool empty = options[index] == 0xFE;
            return _mixInventoryType == MixInventoryType.AttachSocket ? empty : !empty;
        }

        private void UpdateSocketSelection()
        {
            ResetSocketPreview(); // A subsequent server preview describes the new contents.
            if (!IsSocketSelection || CanSelectSocket(_selectedSocket)) return;
            _selectedSocket = -1;
            for (int i = 0; i < 5; i++)
                if (CanSelectSocket(i)) { _selectedSocket = i; break; }
        }

        private Rectangle SocketChoiceRect(int index) => new(_infoRect.X + 14,
            _infoRect.Y + 124 + index * 24, _infoRect.Width - 28, 22);

        private bool HandleSocketClick(Point mouse)
        {
            if (!IsSocketMix || !IsSocketSelection) return false;
            for (int i = 0; i < 5; i++)
            {
                if (!Translate(SocketChoiceRect(i)).Contains(mouse)) continue;
                if (!_mixInProgress && _draggedItem == null && InventoryControl.Instance.GetDraggedItem() == null && CanSelectSocket(i))
                    _selectedSocket = i;
                Scene?.SetMouseInputConsumed();
                return true;
            }
            return false;
        }

        private void DrawSocketInfoText(SpriteBatch sb)
        {
            int x = DisplayRectangle.X + _infoRect.X + 14;
            int top = DisplayRectangle.Y + _infoRect.Y + 10;
            void Line(string text, int y, Color color, float scale = 0.37f)
            {
                float width = _font.MeasureString(text).X;
                if (width > 0) scale = Math.Min(scale, (_infoRect.Width - 28) / width);
                var pos = new Vector2(x, y);
                sb.DrawString(_font, text, pos + Vector2.One, Color.Black * 0.7f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                sb.DrawString(_font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
            Line("Required materials", top, Theme.TextGold);
            var materials = _mixInventoryType switch
            {
                MixInventoryType.ExtractSeed => new[] { "Excellent item +4 or higher; Ancient item +4 or higher", "Jewel of Chaos + Jewel of Creation + Jewel of Harmony" },
                MixInventoryType.SeedSphere => new[] { "Seed + empty Sphere", "Jewel of Chaos + Jewel of Creation" },
                MixInventoryType.AttachSocket => new[] { "Socket equipment + Seed Sphere", "Jewel of Chaos + Jewel of Creation" },
                _ => new[] { "Equipment with a mounted Seed Sphere", "Choose the socket to clear below." },
            };
            for (int i = 0; i < materials.Length; i++) Line(materials[i], top + 23 + i * 20, Theme.TextGray);
            bool preview = _socketPreviewMix == SocketMixId;
            Line(preview ? $"Success rate: {_socketPreviewRate}%" : "Success rate: --", top + 69, Theme.TextWhite);
            Line(preview ? $"Base cost: {FormatZen(_socketPreviewPrice)} Zen" : "Base cost: -- (waiting for valid materials)", top + 89, Theme.TextWhite);

            if (IsSocketSelection)
            {
                var equipment = SocketEquipment;
                for (int i = 0; i < 5; i++)
                {
                    var rect = Translate(SocketChoiceRect(i));
                    if (_socketAtlas != null && !_socketAtlas.IsDisposed)
                        sb.Draw(_socketAtlas, rect, new Rectangle(330, 8, 488, 320), Color.White);
                    else sb.Draw(GraphicsManager.Instance.Pixel, rect, Theme.BgDarkest);
                    if (_selectedSocket == i) sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Y, 3, rect.Height), Theme.TextGold);
                    bool exists = equipment != null && i < equipment.Details.SocketCount &&
                        equipment.Details.SocketOptions != null && i < equipment.Details.SocketOptions.Length && equipment.Details.SocketOptions[i] != 0xFF;
                    string status = !exists ? "Unavailable" : equipment.Details.SocketOptions[i] == 0xFE ? "Empty" : "Occupied";
                    Line($"{(_selectedSocket == i ? ">" : " ")} Socket {i + 1}: {status}", rect.Y + 3,
                        !CanSelectSocket(i) ? Theme.TextDark : _selectedSocket == i ? Theme.TextGold : Theme.TextWhite, 0.35f);
                }
            }
            else
            {
                Line("Place the materials in the combination box.", top + 132, Theme.TextGray);
                Line("The server checks the recipe and final cost.", top + 155, Theme.TextGray);
            }
            Line(_mixInventoryType == MixInventoryType.DetachSocket
                ? "Removal destroys the Seed Sphere and may remove its bonus."
                : "Failure may consume materials.", DisplayRectangle.Y + _infoRect.Bottom - 21, Theme.Warning, 0.31f);
        }

        private void TrySocketMix()
        {
            if (_items.Count == 0 || _draggedItem != null || InventoryControl.Instance.GetDraggedItem() != null)
            {
                RequestDialog.ShowInfo("Place the required materials in the combination box first.");
                return;
            }
            if (IsSocketSelection && _items.Count(i => i.Details.HasSockets && i.Details.SocketCount > 0) != 1)
            {
                RequestDialog.ShowInfo("Place only one piece of socket equipment in the combination box.");
                return;
            }
            if (IsSocketSelection && !CanSelectSocket(_selectedSocket))
            {
                RequestDialog.ShowInfo(_mixInventoryType == MixInventoryType.AttachSocket
                    ? "Choose an empty socket on the equipment." : "Choose an occupied socket on the equipment.");
                return;
            }
            int mixId = SocketMixId;
            string price = _socketPreviewMix == mixId
                ? $"\nSuccess rate: {_socketPreviewRate}%\nBase cost: {FormatZen(_socketPreviewPrice)} Zen"
                : "\nThe server validates the materials, success rate and cost.";
            string socket = IsSocketSelection ? $"\nSocket: {_selectedSocket + 1}" : string.Empty;
            string warning = _mixInventoryType == MixInventoryType.DetachSocket
                ? "\nThe mounted Seed Sphere is destroyed. Its socket bonus may also be lost."
                : "\nFailure may consume materials.";
            RequestDialog.Show(SocketOperationTitle + "?" + socket + price + warning,
                onAccept: () => SendMixRequest(mixId), onReject: null, acceptText: SocketActionText, rejectText: "Cancel");
        }
    }
}
