namespace Client.Main.Core.Input
{
    /// <summary>Keep a covered HUD button suppressed through the covering click's release.</summary>
    public sealed class UiOcclusionGate
    {
        private bool _waitForRelease;
        public bool IsSuppressed { get; private set; }

        public void Update(bool covered, bool pressed, bool previouslyPressed)
        {
            if (covered && (pressed || previouslyPressed))
                _waitForRelease = true;
            else if (!previouslyPressed)
                _waitForRelease = false;

            IsSuppressed = covered || _waitForRelease;
        }
    }
}
