using System;
using Client.Main.ClassicFX.Core;
using Microsoft.Xna.Framework;

internal static class Program
{
    private static int Main()
    {
        try
        {
            foreach (int fps in new[] { 25, 30, 40, 60, 75, 90, 120, 144, 240 })
                TestClock(fps, durationSeconds: 10);
            TestClock(20, durationSeconds: 10); // Main intencionalmente limita factor en FPS < 25
            Console.WriteLine("PASS: reloj real, 25/30/40/60/75/90/120/144/240/20 FPS; 10s.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex.Message);
            return 1;
        }
    }

    private static void TestClock(int fps, int durationSeconds)
    {
        var clock = new ClassicFxClock();
        long previous = 0;
        int periodic30 = 0;
        int advances = 0;
        int samples = fps * durationSeconds;
        for (int frame = 1; frame <= samples; frame++)
        {
            // Dividimos la duracion completa en intervalos medidos en ticks.
            // Así no acumulamos artificialmente error de redondeo en el test.
            long current = (long)Math.Round(
                frame * (double)TimeSpan.TicksPerSecond / fps,
                MidpointRounding.AwayFromZero);
            var delta = TimeSpan.FromTicks(current - previous);
            clock.Update(new GameTime(TimeSpan.FromTicks(current), delta));
            if (clock.AdvancedReferenceFrame) advances++;
            if (clock.IsReferenceFrameInterval(30)) periodic30++;
            if (clock.ReferenceFrame != advances)
                throw new Exception($"FPS {fps}: inconsistencia entre ReferenceFrame y advances");
            if (clock.FrameFactor <= 0f || clock.FrameFactor > 1f)
                throw new Exception($"FPS {fps}: FrameFactor fuera de [0,1]: {clock.FrameFactor}");
            previous = current;
        }
        long expected = Math.Min(fps, 25) * durationSeconds;
        if (clock.ReferenceFrame != expected)
            throw new Exception($"FPS {fps}: ReferenceFrame={clock.ReferenceFrame} (esperado={expected}).");
        if (periodic30 != expected / 30)
            throw new Exception($"FPS {fps}: pulsos intervalo30={periodic30} (esperado={expected / 30}).");
        Console.WriteLine($"PASS {fps,3} FPS: frames logicos={clock.ReferenceFrame}, pulsos30={periodic30}");
    }
}
