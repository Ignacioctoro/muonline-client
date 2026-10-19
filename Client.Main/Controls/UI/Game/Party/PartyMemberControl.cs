using System;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Buffs;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Party
{
    public class PartyMemberControl : UIControl
    {
        public const int RowWidth = 230;
        public const int RowHeight = 92;
        private Texture2D _background, _selected, _hp, _mp, _crown;
        private readonly ButtonControl _leaveButton;
        private readonly BuffSlotControl[] _buffs = new BuffSlotControl[10];
        private string _location = "", _healthText = "";
        private double _refreshTime;
        private bool _inScope;
        public PartyMemberInfo MemberInfo { get; private set; }
        public bool IsCurrentPlayer { get; private set; }

        public PartyMemberControl()
        {
            AutoViewSize = false;
            ViewSize = ControlSize = new Point(RowWidth, RowHeight);
            BackgroundColor = Color.Transparent;
            BorderThickness = 0;
            _leaveButton = new ButtonControl
            {
                X = 207, Y = 5, AutoViewSize = false, ViewSize = new Point(18, 18),
                TexturePath = "Interface/GFx/party_xbtn01_1.ozd", Visible = false,
                BackgroundColor = Color.Transparent, BorderThickness = 0
            };
            _leaveButton.Click += (_, _) =>
            {
                if (!IsCurrentPlayer || MemberInfo == null) return;
                Scene?.SetMouseInputConsumed();
                _ = MuGame.Network?.GetCharacterService()?.SendPartyKickRequestAsync(MemberInfo.Index);
            };
            Controls.Add(_leaveButton);
            for (int i = 0; i < _buffs.Length; i++)
            {
                _buffs[i] = new BuffSlotControl { X = 5 + i * 22, Y = 62, Visible = false };
                Controls.Add(_buffs[i]);
            }
        }

        public override async Task Load()
        {
            var loader = TextureLoader.Instance;
            _background = await loader.PrepareAndGetTexture("Interface/GFx/party_bg.ozd");
            _selected = await loader.PrepareAndGetTexture("Interface/GFx/party_bg01.ozd");
            _hp = await loader.PrepareAndGetTexture("Interface/GFx/party_hpbar.ozd");
            _mp = await loader.PrepareAndGetTexture("Interface/GFx/party_mpbar.ozd");
            _crown = await loader.PrepareAndGetTexture("Interface/GFx/party_crown.ozd");
            await base.Load();
        }

        public void UpdateData(PartyMemberInfo memberInfo, bool isCurrentPlayer = false)
        {
            MemberInfo = memberInfo;
            IsCurrentPlayer = isCurrentPlayer;
            _healthText = $"{Math.Clamp((int)(memberInfo.HealthPercentage * 100), 0, 100)}%";
            _location = $"{MapDatabase.GetMapName(memberInfo.MapId)} ({memberInfo.PositionX}, {memberInfo.PositionY})";
            _leaveButton.Visible = isCurrentPlayer;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (MemberInfo == null) return;
            _refreshTime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_refreshTime > 0) return;
            _refreshTime = 0.5;
            var state = MuGame.Network?.GetCharacterState();
            var scope = MuGame.Network?.GetScopeManager();
            ushort? id = IsCurrentPlayer ? state?.Id : null;
            if (!IsCurrentPlayer && scope != null)
            {
                // PartyList identifies members by name, not by a reliable remote object ID.
                foreach (var entry in scope.GetScopeItems(ScopeObjectType.Player))
                    if (entry is PlayerScopeObject player && string.Equals(player.Name, MemberInfo.Name, StringComparison.OrdinalIgnoreCase))
                    { id = player.Id; break; }
            }
            _inScope = id.HasValue;
            var active = id.HasValue && state != null
                ? state.GetActiveBuffsForPlayer(id.Value).Where(b => BuffIconAtlas.ShouldRender(b.EffectId))
                    .OrderBy(b => BuffIconAtlas.IsDebuff(b.EffectId)).ThenBy(b => b.EffectId).Take(_buffs.Length).ToArray()
                : Array.Empty<ActiveBuffState>();
            for (int i = 0; i < _buffs.Length; i++)
            {
                _buffs[i].Buff = i < active.Length ? active[i] : null;
                _buffs[i].Visible = i < active.Length;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || MemberInfo == null) return;
            var sb = GraphicsManager.Instance.Sprite;
            using var scope = new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                transform: UiScaler.SpriteTransform);
            var rect = DisplayRectangle;
            var background = IsCurrentPlayer ? _selected ?? _background : _background;
            // These are complete standalone GFx sprites; no atlas gutters or generated frame.
            if (background != null) sb.Draw(background, new Rectangle(rect.X, rect.Y, RowWidth, 52), Color.White);
            if (MemberInfo.Index == 0 && _crown != null)
                sb.Draw(_crown, new Rectangle(rect.X + 7, rect.Y + 25, 24, 12), Color.White);
            float health = Math.Clamp(MemberInfo.HealthPercentage, 0f, 1f);
            if (_hp != null && health > 0)
                sb.Draw(_hp, new Rectangle(rect.X + 34, rect.Y + 28, (int)(190 * health), 6), Color.White);
            var state = MuGame.Network?.GetCharacterState();
            if (IsCurrentPlayer && state != null && _mp != null && state.MaximumMana > 0)
            {
                float mana = Math.Clamp((float)state.CurrentMana / state.MaximumMana, 0f, 1f);
                if (mana > 0) sb.Draw(_mp, new Rectangle(rect.X + 34, rect.Y + 38, (int)(190 * mana), 4), Color.White);
            }
            if (!IsCurrentPlayer) Text(sb, "MP --", rect.X + 35, rect.Y + 35, 7, Color.Gray);
            Text(sb, MemberInfo.Name, rect.X + 35, rect.Y + 8, 11, _inScope ? Color.White : Color.Gray);
            Text(sb, _healthText, rect.X + 5, rect.Y + 9, 8, Color.Gold);
            Text(sb, _location, rect.X + 5, rect.Y + 49, 9, new Color(190, 190, 180));
            // Remote mana is not part of the Season 6 party packets. Never invent it.
            for (int i = 0; i < Controls.Count; i++) Controls[i].Draw(gameTime);
        }

        private static void Text(SpriteBatch sb, string text, int x, int y, float size, Color color)
        {
            var font = GraphicsManager.Instance.Font;
            if (font != null) sb.DrawString(font, text, new Vector2(x, y), color, 0,
                Vector2.Zero, size / Constants.BASE_FONT_SIZE, SpriteEffects.None, 0);
        }
    }
}
