namespace Client.Main.Core.Input;

/// <summary>Captures a single pointer from press through release, excluding drags.</summary>
public sealed class PlayerMenuHoldGesture
{
    private int? _pointer;
    private ushort _target;
    private int _x, _y;
    private double _seconds;
    private bool _cancelled, _opened;

    public void Reset()
    {
        _pointer = null;
        _seconds = 0;
        _cancelled = _opened = false;
    }

    // Return true while consuming a captured gesture, including its final release.
    public bool Update(int? pointer, bool pressed, bool down, ushort? target,
        bool blocked, int x, int y, double elapsedSeconds, out bool openMenu)
    {
        openMenu = false;
        if (!pointer.HasValue || !down)
        {
            bool captured = _pointer.HasValue;
            Reset();
            return captured;
        }
        if (_pointer != pointer)
        {
            Reset();
            if (!pressed || blocked || !target.HasValue) return false;
            _pointer = pointer;
            _target = target.Value;
            _x = x;
            _y = y;
        }
        // Opening the menu itself blocks world picking; still capture the release.
        if (_opened) return true;
        long dx = x - _x, dy = y - _y;
        if (blocked || target != _target || dx * dx + dy * dy > 16 * 16)
            _cancelled = true;
        if (_cancelled) return true;
        _seconds += System.Math.Max(0, elapsedSeconds);
        if (_seconds >= 0.65)
            openMenu = _opened = true;
        return true;
    }
}
