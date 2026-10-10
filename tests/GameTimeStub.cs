using System;

// El reloj real del proyecto solo utiliza estas dos propiedades de GameTime.
// No se modifica ClassicFxClock.cs ni se necesita descargar MonoGame/NuGet.
namespace Microsoft.Xna.Framework
{
    public sealed class GameTime
    {
        public TimeSpan TotalGameTime { get; }
        public TimeSpan ElapsedGameTime { get; }

        public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
        {
            TotalGameTime = totalGameTime;
            ElapsedGameTime = elapsedGameTime;
        }
    }
}
