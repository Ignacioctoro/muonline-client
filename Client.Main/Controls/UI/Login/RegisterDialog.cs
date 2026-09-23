using Client.Main.Models;
using Client.Main.Controls.UI.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;


namespace Client.Main.Controls.UI.Login
{
    public class RegisterDialog : PopupFieldDialog
    {
        private readonly TextFieldControl _usernameInput;
        private readonly TextFieldControl _emailInput;
        private readonly TextFieldControl _passwordInput;
        private readonly TextFieldControl _confirmPasswordInput;

        private readonly LabelControl _captchaStatusLabel;

        private readonly ButtonControl _verifyButton;
        private readonly ButtonControl _registerButton;
        private readonly ButtonControl _backButton;

        public string Username => _usernameInput.Value;
        public string Email => _emailInput.Value;
        public string Password => _passwordInput.Value;
        public string ConfirmPassword => _confirmPasswordInput.Value;

        public event EventHandler VerifyCaptchaRequested;
        public event EventHandler RegisterRequested;
        public event EventHandler BackRequested;

        public RegisterDialog()
        {
            ControlSize = new Point(390, 360);
            ViewSize = ControlSize;

            Controls.Add(new LabelControl
            {
                Text = "B Royal MU",
                Align = ControlAlign.HorizontalCenter,
                Y = 15,
                FontSize = 14f,
                TextColor = new Color(241, 188, 37)
            });

            Controls.Add(new LabelControl
            {
                Text = "Crear cuenta",
                Align = ControlAlign.HorizontalCenter,
                Y = 40,
                FontSize = 13f,
                TextColor = Color.White
            });

            AddLabel("Usuario", 75);
            AddLabel("Correo", 115);
            AddLabel("Contraseña", 155);
            AddLabel("Confirmar", 195);

            _usernameInput = CreateTextField(
                x: 145,
                y: 72,
                maxLength: 10);

            _emailInput = CreateTextField(
                x: 145,
                y: 112,
                maxLength: 100);

            _passwordInput = CreateTextField(
                x: 145,
                y: 152,
                maxLength: 72,
                masked: true);

            _confirmPasswordInput = CreateTextField(
                x: 145,
                y: 192,
                maxLength: 72,
                masked: true);

            Controls.Add(_usernameInput);
            Controls.Add(_emailInput);
            Controls.Add(_passwordInput);
            Controls.Add(_confirmPasswordInput);

            _verifyButton = CreateButton(
                "Verificar CAPTCHA",
                x: 105,
                y: 230,
                width: 180);

            _verifyButton.Click += VerifyButton_Click;
            Controls.Add(_verifyButton);

            _captchaStatusLabel = new LabelControl
            {
                Text = "No verificado",
                Align = ControlAlign.HorizontalCenter,
                Y = 270,
                FontSize = 11f,
                TextColor = Color.Gray
            };

            Controls.Add(_captchaStatusLabel);

            _registerButton = CreateButton(
                "Crear cuenta",
                x: 75,
                y: 305,
                width: 115);

            _registerButton.Click += RegisterButton_Click;
            Controls.Add(_registerButton);

            _backButton = CreateButton(
                "Volver",
                x: 200,
                y: 305,
                width: 115);

            _backButton.Click += BackButton_Click;
            Controls.Add(_backButton);

            SetupFocusHandlers();
        }

        public void SetCaptchaPending()
        {
            _captchaStatusLabel.Text = "Esperando verificación...";
            _captchaStatusLabel.TextColor =
                new Color(241, 188, 37);
        }

        public void SetCaptchaVerified()
        {
            _captchaStatusLabel.Text = "CAPTCHA verificado";
            _captchaStatusLabel.TextColor =
                new Color(90, 220, 120);
        }

        public void SetCaptchaFailed()
        {
            _captchaStatusLabel.Text =
                "No fue posible verificar el CAPTCHA";

            _captchaStatusLabel.TextColor =
                new Color(220, 90, 90);
        }

        public void ResetCaptcha()
        {
            _captchaStatusLabel.Text = "No verificado";
            _captchaStatusLabel.TextColor = Color.Gray;
        }

        public void FocusUsername()
        {
            _usernameInput.OnFocus();
            _emailInput.OnBlur();
            _passwordInput.OnBlur();
            _confirmPasswordInput.OnBlur();
        }

        public override void Update(GameTime gameTime)
        {
            HandleTabNavigation();

            base.Update(gameTime);
        }

        private void AddLabel(
            string text,
            int y)
        {
            Controls.Add(new LabelControl
            {
                Text = text,
                X = 30,
                Y = y,
                AutoViewSize = false,
                ViewSize = new Point(100, 20),
                TextAlign = HorizontalAlign.Right,
                FontSize = 12f
            });
        }

        private static TextFieldControl CreateTextField(
            int x,
            int y,
            int maxLength,
            bool masked = false)
        {
            var input = TextFieldControl.Create();

            input.X = x;
            input.Y = y;
            input.Skin = TextFieldSkin.NineSlice;
            input.MaxLength = maxLength;
            input.MaskValue = masked;

            return input;
        }

        private static ButtonControl CreateButton(
            string text,
            int x,
            int y,
            int width)
        {
            return new ButtonControl
            {
                Text = text,
                X = x,
                Y = y,

                AutoViewSize = false,
                ViewSize = new Point(width, 30),
                ControlSize = new Point(width, 30),

                FontSize = 12f,

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
        }

        private void SetupFocusHandlers()
        {
            _usernameInput.Click += (s, e) =>
            {
                FocusField(_usernameInput);
            };

            _emailInput.Click += (s, e) =>
            {
                FocusField(_emailInput);
            };

            _passwordInput.Click += (s, e) =>
            {
                FocusField(_passwordInput);
            };

            _confirmPasswordInput.Click += (s, e) =>
            {
                FocusField(_confirmPasswordInput);
            };
        }

        private void FocusField(TextFieldControl target)
        {
            _usernameInput.OnBlur();
            _emailInput.OnBlur();
            _passwordInput.OnBlur();
            _confirmPasswordInput.OnBlur();

            target.OnFocus();
        }

        private void HandleTabNavigation()
        {
            bool tabPressed =
                MuGame.Instance.Keyboard.IsKeyDown(Keys.Tab)
                &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Tab);

            if (!tabPressed)
            {
                return;
            }

            if (_usernameInput.IsFocused)
            {
                FocusField(_emailInput);
            }
            else if (_emailInput.IsFocused)
            {
                FocusField(_passwordInput);
            }
            else if (_passwordInput.IsFocused)
            {
                FocusField(_confirmPasswordInput);
            }
            else
            {
                FocusField(_usernameInput);
            }
        }

        private void VerifyButton_Click(
            object sender,
            EventArgs e)
        {
            BlurAllFields();

            VerifyCaptchaRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void RegisterButton_Click(
            object sender,
            EventArgs e)
        {
            BlurAllFields();

            RegisterRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void BackButton_Click(
            object sender,
            EventArgs e)
        {
            BlurAllFields();

            BackRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        private void BlurAllFields()
        {
            _usernameInput.OnBlur();
            _emailInput.OnBlur();
            _passwordInput.OnBlur();
            _confirmPasswordInput.OnBlur();

            if (Scene != null)
            {
                Scene.FocusControl = null;
            }
        }
    }
}