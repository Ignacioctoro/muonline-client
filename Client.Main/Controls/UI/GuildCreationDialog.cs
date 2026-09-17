using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Client.Main.Controls.UI
{
    public class GuildCreationDialog : PopupFieldDialog
    {
        private readonly TextFieldControl _guildNameInput;
        private readonly GuildEmblemEditorControl _emblemEditor;

        private readonly LabelButtonSmall _createButton;
        private readonly LabelButtonSmall _cancelButton;
        private readonly LabelButtonSmall _clearButton;

        public event EventHandler CreateRequested;
        public event EventHandler CancelRequested;

        public string GuildName =>
            _guildNameInput.Value?.Trim() ?? string.Empty;

        public byte[] GuildEmblem =>
            _emblemEditor.GetEmblemBytes();

        public GuildCreationDialog()
        {
            Interactive = true;

            // Más alto para dejar espacio al editor 8x8.
            ControlSize = new Point(360, 430);

            // ─────────────────────────────────────────────
            // TÍTULO
            // ─────────────────────────────────────────────

            Controls.Add(new LabelControl
            {
                Text = "Create Guild",
                Align = ControlAlign.HorizontalCenter,
                Y = 20,
                FontSize = 14
            });

            // ─────────────────────────────────────────────
            // NOMBRE DE GUILD
            // ─────────────────────────────────────────────

            Controls.Add(new LabelControl
            {
                Text = "Guild Name",
                X = 45,
                Y = 67,
                AutoViewSize = false,
                ViewSize = new Point(90, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12
            });

            _guildNameInput = TextFieldControl.Create();

            _guildNameInput.X = 145;
            _guildNameInput.Y = 63;
            _guildNameInput.Skin = TextFieldSkin.NineSlice;

            Controls.Add(_guildNameInput);

            Controls.Add(new LabelControl
            {
                Text = "Maximum 8 characters",
                Align = ControlAlign.HorizontalCenter,
                Y = 95,
                FontSize = 10,
                TextColor = new Color(180, 180, 180)
            });

            // ─────────────────────────────────────────────
            // EMBLEMA
            // ─────────────────────────────────────────────

            Controls.Add(new LabelControl
            {
                Text = "Guild Emblem",
                Align = ControlAlign.HorizontalCenter,
                Y = 120,
                FontSize = 12
            });

            _emblemEditor = new GuildEmblemEditorControl
            {
                X = 108,
                Y = 148
            };

            Controls.Add(_emblemEditor);

            // ─────────────────────────────────────────────
            // BOTÓN CLEAR
            // ─────────────────────────────────────────────

            _clearButton = new LabelButtonSmall
            {
                X = 148,
                Y = 355,

                Label = new LabelControl
                {
                    Text = "Clear",
                    Align =
                        ControlAlign.HorizontalCenter |
                        ControlAlign.VerticalCenter,
                    FontSize = 11
                }
            };

            _clearButton.Click += ClearButton_Click;

            Controls.Add(_clearButton);

            // ─────────────────────────────────────────────
            // CREATE
            // ─────────────────────────────────────────────

            _createButton = new LabelButtonSmall
            {
                X = 108,
                Y = 392,

                Label = new LabelControl
                {
                    Text = "Create",
                    Align =
                        ControlAlign.HorizontalCenter |
                        ControlAlign.VerticalCenter,
                    FontSize = 11
                }
            };

            _createButton.Click += CreateButton_Click;

            Controls.Add(_createButton);

            // ─────────────────────────────────────────────
            // CANCEL
            // ─────────────────────────────────────────────

            _cancelButton = new LabelButtonSmall
            {
                X = 188,
                Y = 392,

                Label = new LabelControl
                {
                    Text = "Cancel",
                    Align =
                        ControlAlign.HorizontalCenter |
                        ControlAlign.VerticalCenter,
                    FontSize = 11
                }
            };

            _cancelButton.Click += CancelButton_Click;

            Controls.Add(_cancelButton);

            // ─────────────────────────────────────────────
            // FOCUS DEL INPUT
            // ─────────────────────────────────────────────

            _guildNameInput.Click += (sender, args) =>
            {
                _guildNameInput.OnFocus();

                if (Scene != null)
                {
                    Scene.FocusControl = _guildNameInput;
                }
            };
        }

        // ─────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────

        public override void Update(GameTime gameTime)
        {
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Enter) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Enter))
            {
                RequestCreate();
            }

            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                RequestCancel();
            }

            base.Update(gameTime);
        }

        // ─────────────────────────────────────────────
        // FOCUS
        // ─────────────────────────────────────────────

        public void FocusGuildName()
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                _guildNameInput.OnFocus();

                if (Scene != null)
                {
                    Scene.FocusControl = _guildNameInput;
                }
            });
        }

        // ─────────────────────────────────────────────
        // BOTONES
        // ─────────────────────────────────────────────

        private void CreateButton_Click(
            object sender,
            EventArgs e)
        {
            RequestCreate();
        }

        private void CancelButton_Click(
            object sender,
            EventArgs e)
        {
            RequestCancel();
        }

        private void ClearButton_Click(
            object sender,
            EventArgs e)
        {
            _emblemEditor.Clear();
        }

        // ─────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────

        private void RequestCreate()
        {
            string guildName = GuildName;

            if (string.IsNullOrWhiteSpace(guildName))
            {
                return;
            }

            if (guildName.Length > 8)
            {
                return;
            }

            ClearInputFocus();

            CreateRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        // ─────────────────────────────────────────────
        // CANCEL
        // ─────────────────────────────────────────────

        private void RequestCancel()
        {
            ClearInputFocus();

            CancelRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        // ─────────────────────────────────────────────
        // LIMPIAR FOCUS
        // ─────────────────────────────────────────────

        private void ClearInputFocus()
        {
            _guildNameInput.OnBlur();

            if (Scene != null &&
                Scene.FocusControl == _guildNameInput)
            {
                Scene.FocusControl = null;
            }
        }
    }
}