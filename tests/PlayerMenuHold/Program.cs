using Client.Main.Core.Input;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
var hold = new PlayerMenuHoldGesture();
bool Tick(int? pointer, bool press, bool down, ushort? target, bool blocked, int x, double time, out bool opened)
    => hold.Update(pointer, press, down, target, blocked, x, 100, time, out opened);

Check(Tick(1, true, true, 42, false, 100, .1, out var open) && !open, "press captures without opening");
Check(Tick(1, false, false, 42, false, 100, .1, out open) && !open, "short tap releases without opening");
Check(!Tick(null, false, false, null, false, 100, 1, out open), "release clears capture");
Tick(1, true, true, 42, false, 100, .3, out open);
Check(Tick(1, false, true, 42, false, 110, .36, out open) && open, "stable hold opens after threshold");
Check(Tick(1, false, true, null, true, 110, 1, out open) && !open, "open only once while menu covers target");
Check(Tick(1, false, false, null, true, 110, .1, out open) && !open, "opening finger release is consumed");
Tick(1, true, true, 42, false, 100, .2, out open);
Tick(1, false, true, 42, false, 130, .2, out open);
Check(Tick(1, false, true, 42, false, 100, 1, out open) && !open, "drag cannot resume when returning to origin");
hold.Reset();
Check(!Tick(1, false, true, 42, false, 100, 1, out open) && !open, "dragging onto a player cannot start a hold");
Tick(1, true, true, 42, false, 100, .4, out open);
Tick(1, false, true, 43, false, 100, .1, out open);
Check(Tick(1, false, true, 42, false, 100, 1, out open) && !open, "changing targets cancels for entire gesture");
hold.Reset();
Check(!Tick(1, true, true, 42, true, 100, 1, out open), "UI-blocked press cannot capture");
Tick(1, true, true, 42, false, 100, .4, out open);
Check(Tick(null, false, false, null, false, 100, .1, out open) && !open, "lost touch releases capture");
Check(Tick(2, true, true, 43, false, 100, .3, out open) && !open, "replacement finger starts a new timer");
Tick(2, false, true, null, false, 100, .1, out open);
Check(Tick(2, false, true, 43, false, 100, 1, out open) && !open, "despawn or lost hit test cancels hold");
hold.Reset();
Tick(1, true, true, 42, false, 100, .3, out open);
Tick(1, false, true, 42, true, 100, .1, out open);
Check(Tick(1, false, true, 42, false, 100, 1, out open) && !open, "UI appearing mid-hold cancels");
Console.WriteLine($"{checks} player-menu gesture checks passed.");
