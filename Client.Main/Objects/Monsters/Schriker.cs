using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(160, "Schriker")]
    public class Schriker : MonsterObject
    {
        public Schriker()
        {
            Scale = 1.2f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster70.bmd");
            await base.Load();
        }
    }
}
