using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(424, "Grizzly")]
    public class Grizzly : MonsterObject
    {
        public Grizzly()
        {
            Scale = 1.14f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster135.bmd");
            await base.Load();
        }
    }
}
