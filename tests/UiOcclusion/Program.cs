using Client.Main.Core.Input;

var gate = new UiOcclusionGate();
void Tick(bool covered, bool pressed, bool previous, bool expected, string scenario)
{
    gate.Update(covered, pressed, previous);
    if (gate.IsSuppressed != expected) throw new Exception(scenario);
}

Tick(false, false, false, false, "Uncovered idle button is available");
Tick(false, true, false, false, "Fresh click on an uncovered button is allowed");
Tick(false, false, true, false, "Uncovered click may complete normally");
Tick(true, false, false, true, "Any overlapping menu suppresses the button");
Tick(true, true, false, true, "Inventory close press belongs to the inventory");
Tick(false, true, true, true, "Closing on press cannot expose the underlying button mid-hold");
Tick(false, false, true, true, "Closing click release cannot open Commands");
Tick(false, true, false, false, "The next fresh press works even without an intervening idle frame");
Tick(false, false, true, false, "That intentional click completes normally");
Tick(false, false, false, false, "Button returns after the closing gesture ends");
Tick(false, true, false, false, "A subsequent intentional click is allowed");
Tick(true, true, true, true, "A menu appearing during a hold cancels that gesture");
Tick(false, false, true, true, "Release remains blocked if that menu disappears");
Tick(false, false, false, false, "Hidden menu does not permanently disable the launcher");
Tick(true, false, true, true, "A window closed on release retains ownership for the whole frame");
Tick(false, false, false, false, "Release-only closure also recovers");
Tick(true, false, false, true, "Keyboard-opened window covers the launcher without a mouse hold");
Tick(false, false, false, false, "Keyboard closure with no active click restores the launcher");
Console.WriteLine("UI occlusion gesture checks passed.");
