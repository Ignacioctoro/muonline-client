using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(275, "Kundun")]
    public class Kundun : MonsterObject
    {
        public Kundun()
        {
            Scale = 2.0f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster65.bmd");
            await base.Load();
        }
    }
}
