using Client.Main.Content;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Objects.Monsters
{
    [NpcInfo(459, "Selupan")]
    public class SelupanBoss : MonsterObject
    {
        public SelupanBoss()
        {
            Scale = 2.0f;
            BlendMesh = 5; // Original Selupan's additive blast layer.
        }

        public override async Task Load()
        {
            Model = await BMDLoader.Instance.Prepare($"Monster/Monster151.bmd");
            await base.Load();
        }

        // S6 uses Monster151; the Monster415 mesh-4 override belongs to another model.
    }
}
