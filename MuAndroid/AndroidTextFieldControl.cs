using Client.Main.Controls.UI;
using Microsoft.Xna.Framework.Input;

namespace MuAndroid
{
    public class AndroidTextFieldControl : TextFieldControl
    {
        public override async void OnFocus()
        {
            // Evita intentar abrir dos KeyboardInput
            // para el mismo campo.
            if (IsFocused)
                return;

            base.OnFocus();

            var result = await KeyboardInput.Show(
                title: string.IsNullOrEmpty(Label)
                    ? "Text Input"
                    : Label,

                description: string.IsNullOrEmpty(Placeholder)
                    ? "Enter value"
                    : Placeholder,

                defaultText: Value,
                usePasswordMode: MaskValue
            );

            if (result != null)
            {
                Value = result;
                OnValueChanged();
            }
        }

        public override void OnBlur()
        {
            base.OnBlur();
        }
    }
}