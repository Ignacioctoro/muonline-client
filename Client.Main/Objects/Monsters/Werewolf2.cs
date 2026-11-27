using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(421, "Werewolf")]
    public class Werewolf2 : MonsterObject
    {
        public Werewolf2()
        {
            Scale = 0.88f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster132.bmd");
            await base.Load();
        }
    }
}
