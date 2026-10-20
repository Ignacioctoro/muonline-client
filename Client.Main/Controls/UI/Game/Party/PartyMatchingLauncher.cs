using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Party;

/// <summary>Always-visible touch target, including when the player has no party.</summary>
public sealed class PartyMatchingLauncher : TextureControl
{
    private readonly LabelControl _label;
    private int _requests = -1;
    public override bool BlocksPointerInput => true;
    public PartyMatchingLauncher()
    {
        TexturePath = "Interface/GFx/btn_l01.ozd";
        AutoViewSize = false; ViewSize = ControlSize = new Point(110, 28);
        Align = ControlAlign.Top | ControlAlign.Right;
        Margin = new Margin { Top = 12, Right = 10 }; Interactive = true;
        _label = new LabelControl { Text = "Party [P]", X = 14, Y = 6, FontSize = 11, TextColor = Color.Gold, Interactive = false };
        Controls.Add(_label);
        Click += (_, _) => { Scene?.SetMouseInputConsumed(); (Scene as GameScene)?.PartyMatchingWindow?.Toggle(); };
    }
    public override void Update(GameTime time)
    {
        base.Update(time);
        int count = MuGame.Network.GetCharacterService().PartyMatching.Snapshot.Requests.Length;
        if (_requests != count) { _requests = count; _label.Text = count > 0 ? $"Party ({count})" : "Party [P]"; }
    }
}
