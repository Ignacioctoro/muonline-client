using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(422, "Cursed Lich")]
    public class CursedLich : MonsterObject
    {
        public CursedLich()
        {
            Scale = 0.95f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster133.bmd");
            await base.Load();
        }
    }
}
