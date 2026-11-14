namespace PlatePeek.Core;

public sealed class CaptureRouter
{
    private const int MaxHistory = 10;
    private readonly List<ResolvedPlateView> history = new();

    public ResolvedPlateView? View { get; private set; }
    public ResolvedPlateView? Stashed { get; private set; }
    public bool Pinned { get; set; }

    public IReadOnlyList<ResolvedPlateView> History => history;

    public bool CanSwitch =>
        View != null && Stashed != null && Stashed.Snapshot.ContentId != View.Snapshot.ContentId;

    public void Accept(ResolvedPlateView incoming)
    {
        history.RemoveAll(h => h.Snapshot.ContentId == incoming.Snapshot.ContentId);
        history.Insert(0, incoming);
        if (history.Count > MaxHistory)
            history.RemoveAt(history.Count - 1);

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

        if (View != null)
            Stashed = View;
        View = incoming;
    }

    public void Switch()
    {
        if (CanSwitch)
            (View, Stashed) = (Stashed, View);
    }

    public void Show(ResolvedPlateView entry)
    {
        if (View == entry)
            return;
        if (View != null)
            Stashed = View;
        View = entry;
    }
}
