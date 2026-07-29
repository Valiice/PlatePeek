namespace PlatePeek.Core;

/// <summary>
/// Decides which captured plate the inspector shows. Captured players always
/// keep the screen: the local player's own plate (opened e.g. to reach the
/// portrait editor) is stashed behind a switch action instead of replacing
/// them. While pinned, any different character goes to the stash.
/// Same-character updates (unlock-cache re-resolves) always apply in place.
/// </summary>
public sealed class CaptureRouter
{
    public ResolvedPlateView? View { get; private set; }
    public ResolvedPlateView? Stashed { get; private set; }
    public bool Pinned { get; set; }

    /// <summary>True when a different character is waiting in the stash.</summary>
    public bool CanSwitch =>
        View != null && Stashed != null && Stashed.Snapshot.ContentId != View.Snapshot.ContentId;

    public void Accept(ResolvedPlateView incoming)
    {
        if (View != null && View.Snapshot.ContentId == incoming.Snapshot.ContentId)
        {
            View = incoming;
            return;
        }

        if (View != null && (Pinned || (incoming.Snapshot.IsSelf && !View.Snapshot.IsSelf)))
        {
            Stashed = incoming;
            return;
        }

        // Incoming takes the screen; the displaced capture stays one switch away.
        if (View != null)
            Stashed = View;
        View = incoming;
    }

    public void Switch()
    {
        if (CanSwitch)
            (View, Stashed) = (Stashed, View);
    }
}
