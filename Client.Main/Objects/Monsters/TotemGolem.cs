using Client.Main.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(423, "Totem Golem")]
    public class TotemGolem : MonsterObject
    {
        public TotemGolem()
        {
            Scale = 0.1615f;
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster134.bmd");
            await base.Load();
        }
    }
}
