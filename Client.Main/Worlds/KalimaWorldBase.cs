using System.Threading.Tasks;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Worlds
{
    public abstract class KalimaWorldBase : S6EventWorldBase
    {
        private readonly KalimaWaterRenderer _water = new();

        protected KalimaWorldBase(string name) : base(worldIndex: 25, name: name) { }

        public override async Task Load()
        {
            await base.Load();
            await _water.LoadAsync();
        }

        public override void AfterLoad()
        {
            base.AfterLoad();
            // Original IsWaterTerrain bypasses RenderTerrain, including ordinary grass.
            // Keep its loaded ATT/height data for gameplay, pathfinding and ray picking.
            Terrain.Visible = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status == GameControlStatus.Ready && Walker != null) _water.Update(gameTime, Walker.Position);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            // Native: composite water after world geometry/effects, before labels and HUD.
            // DrawAfter runs both inside BaseScene.Draw and again through the scene's
            // final control pass. Putting water there blends it twice, including over UI.
            // Draw runs once, while the world depth buffer is still intact.
            if (Visible && Status == GameControlStatus.Ready && Walker != null) _water.Draw(Walker.Position);
        }

        public override void Dispose()
        {
            _water.Dispose();
            base.Dispose();
        }
    }
}
