using Client.Main.Models;
using Client.Main.Controls.UI.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Client.Main.Controls.UI.Login
{
    public class LoginDialog : PopupFieldDialog
    {
        private readonly TextureControl _line1;
        private readonly TextureControl _line2;

        private readonly TextFieldControl _userInput;
        private readonly TextFieldControl _passwordInput;

        private readonly LabelControl _serverNameLabel;

        private readonly OkButton _okButton;
        private readonly ButtonControl _registerButton;
        public string ServerName
        {
            get => _serverNameLabel.Text;
            set => _serverNameLabel.Text = value;
        }

        public string Username => _userInput.Value
        ;
        public string Password => _passwordInput.Value;

        public event EventHandler LoginAttempt;
        public event EventHandler RegisterRequested;

        public LoginDialog()
        {
            ControlSize = new Point(300, 245);

            Controls.Add(new LabelControl
            {
                Text = "B Royal MU",
                Align = ControlAlign.HorizontalCenter,
                Y = 15,
                FontSize = 12
            });

            Controls.Add(_line1 = new TextureControl
            {
                TexturePath =
                    "Interface/GFx/popup_line_m.ozd",

                X = 10,
                Y = 40,
                AutoViewSize = false
            });

            Controls.Add(_serverNameLabel =
                new LabelControl
                {
                    Text = "OpenMU Server 1",

                    Align =
                        ControlAlign.HorizontalCenter,

                    Y = 55,
                    FontSize = 12,

                    TextColor =
                        new Color(241, 188, 37)
                });

            Controls.Add(new LabelControl
            {
                Text = "Usuario",
                Y = 90,
                X = 20,
                AutoViewSize = false,
                ViewSize = new Point(70, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12f
            });

            Controls.Add(new LabelControl
            {
                Text = "Contraseña",
                Y = 120,
                X = 20,
                AutoViewSize = false,
                ViewSize = new Point(70, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12f
            });

            Controls.Add(_line2 =
                new TextureControl
                {
                    TexturePath =
                        "Interface/GFx/popup_line_m.ozd",

                    X = 10,
                    Y = 150,

                    AutoViewSize = false,
                    Alpha = 0.7f
                });

            _userInput =
                TextFieldControl.Create();

            _userInput.X = 100;
            _userInput.Y = 87;
            _userInput.Skin =
                TextFieldSkin.NineSlice;

            _userInput.MaxLength = 10;

            _passwordInput =
                TextFieldControl.Create();

            _passwordInput.X = 100;
            _passwordInput.Y = 117;

            _passwordInput.MaskValue = true;

            _passwordInput.Skin =
                TextFieldSkin.NineSlice;

            _passwordInput.MaxLength = 72;

            _passwordInput.EnterKeyPressed +=
                PasswordInput_EnterPressed;

            Controls.Add(_userInput);
            Controls.Add(_passwordInput);

            _userInput.Click += (s, e) =>
            {
                _userInput.OnFocus();
                _passwordInput.OnBlur();
            };

            _passwordInput.Click += (s, e) =>
            {
                _passwordInput.OnFocus();
                _userInput.OnBlur();
            };

            _okButton = new OkButton
            {
                Y = 160,

                Align =
                    ControlAlign.HorizontalCenter
            };

            _okButton.Click += OkButton_Click;

            Controls.Add(_okButton);

            Controls.Add(new LabelControl
            {
                Text = "¿No tienes una cuenta?",

                Align =
                    ControlAlign.HorizontalCenter,

                Y = 198,

                FontSize = 11f,

                TextColor =
                    new Color(190, 190, 190)
            });

            _registerButton =
                new ButtonControl
                {
                    Text = "Crear cuenta",

                    X = 96,
                    Y = 215,

                    AutoViewSize = false,

                    ViewSize =
                        new Point(108, 25),

                    ControlSize =
                        new Point(108, 25),

                    FontSize = 11f,

                    BackgroundColor =
                        new Color(20, 24, 30, 240),

                    HoverBackgroundColor =
                        new Color(55, 60, 70, 240),

                    PressedBackgroundColor =
                        new Color(90, 75, 35, 240),

                    BorderColor =
                        new Color(110, 95, 55),

                    BorderThickness = 1,

                    TextColor = Color.White,

                    HoverTextColor =
                        new Color(255, 220, 130)
                };

            _registerButton.Click +=
                RegisterButton_Click;

            Controls.Add(_registerButton);
        }

        public void FocusUsername()
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                _userInput?.OnFocus();
                _passwordInput?.OnBlur();
            });
        }

        public override void Update(GameTime gameTime)
        {
            if (
                MuGame.Instance.Keyboard.IsKeyDown(Keys.Tab)
                &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Tab))
            {
                if (_userInput.IsFocused)
                {
                    _userInput.OnBlur();
                    _passwordInput.OnFocus();
                }
                else if (_passwordInput.IsFocused)
                {
                    _passwordInput.OnBlur();
                    _userInput.OnFocus();
                }
            }

            base.Update(gameTime);
        }

        protected override void OnScreenSizeChanged()
        {
            if (_line1 != null)
            {
                _line1.ViewSize =
                    new Point(DisplaySize.X - 20, 8);
            }

            if (_line2 != null)
            {
                _line2.ViewSize =
                    new Point(DisplaySize.X - 20, 5);
            }

            base.OnScreenSizeChanged();
        }

        private void OkButton_Click(
            object sender,
            EventArgs e)
        {
            AttemptLogin();
        }

        private void PasswordInput_EnterPressed(
            object sender,
            EventArgs e)
        {
            AttemptLogin();
        }

        private void RegisterButton_Click(
            object sender,
            EventArgs e)
        {
            _userInput.OnBlur();
            _passwordInput.OnBlur();

            if (Scene != null)
            {
                Scene.FocusControl = null;
            }

            RegisterRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void AttemptLogin()
        {
            _userInput.OnBlur();
            _passwordInput.OnBlur();

            if (
                Scene != null
                &&
                (
                    Scene.FocusControl == _userInput
                    ||
                    Scene.FocusControl == _passwordInput
                ))
            {
                Scene.FocusControl = null;
            }

            LoginAttempt?.Invoke(
                this,
                EventArgs.Empty);
        }
    }
}