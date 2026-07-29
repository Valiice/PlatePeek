using PlatePeek.Core;
using Xunit;

namespace PlatePeek.Tests;

public class CaptureRouterTests
{
    private static ResolvedPlateView Player(ulong contentId, bool isSelf, string name = "Someone") => new()
    {
        Snapshot = new PlateSnapshot { ContentId = contentId, IsSelf = isSelf, Name = name },
    };

    [Fact]
    public void OtherPlayerCapture_TakesTheScreen()
    {
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        Assert.Equal("Lisana", router.View?.Snapshot.Name);
    }

    [Fact]
    public void OwnPlate_DoesNotReplaceCapturedPlayer_GoesToStash()
    {
        // The regression that broke import: walking to your editor opens your
        // own plate, which must not clobber the captured target.
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Accept(Player(2, isSelf: true, "Va Li"));

        Assert.Equal("Lisana", router.View?.Snapshot.Name);
        Assert.Equal("Va Li", router.Stashed?.Snapshot.Name);
        Assert.True(router.CanSwitch);
    }

    [Fact]
    public void Switch_SwapsViewAndStash_BothWays()
    {
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Accept(Player(2, isSelf: true, "Va Li"));

        router.Switch();
        Assert.Equal("Va Li", router.View?.Snapshot.Name);
        Assert.Equal("Lisana", router.Stashed?.Snapshot.Name);

        router.Switch();
        Assert.Equal("Lisana", router.View?.Snapshot.Name);
    }

    [Fact]
    public void NewerOtherPlayer_ReplacesOlderOtherPlayer_AndStashesIt()
    {
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Accept(Player(3, isSelf: false, "Adalmont"));
        Assert.Equal("Adalmont", router.View?.Snapshot.Name);
        Assert.Equal("Lisana", router.Stashed?.Snapshot.Name);
        Assert.True(router.CanSwitch);
    }

    [Fact]
    public void UnpinThenReacceptStashedCharacter_KeepsPreviousViewReachable()
    {
        // Regression: pin, capture a second player (stashed), unpin, then open
        // that second player's plate again. The first player must not be lost.
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Pinned = true;
        router.Accept(Player(3, isSelf: false, "Adalmont"));
        router.Pinned = false;
        router.Accept(Player(3, isSelf: false, "Adalmont"));

        Assert.Equal("Adalmont", router.View?.Snapshot.Name);
        Assert.Equal("Lisana", router.Stashed?.Snapshot.Name);
        Assert.True(router.CanSwitch);
    }

    [Fact]
    public void Pinned_SendsDifferentCharactersToStash()
    {
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Pinned = true;
        router.Accept(Player(3, isSelf: false, "Adalmont"));

        Assert.Equal("Lisana", router.View?.Snapshot.Name);
        Assert.Equal("Adalmont", router.Stashed?.Snapshot.Name);
    }

    [Fact]
    public void SameCharacterUpdate_AppliesInPlace_EvenWhenPinned()
    {
        // Unlock-cache harvests re-resolve the same snapshot; the refresh must
        // land even while pinned.
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Pinned = true;

        var refreshed = Player(1, isSelf: false, "Lisana") with { JobName = "white mage" };
        router.Accept(refreshed);

        Assert.Equal("white mage", router.View?.JobName);
    }

    [Fact]
    public void OwnPlateFirst_ThenOtherPlayer_StashesOwnPlate()
    {
        var router = new CaptureRouter();
        router.Accept(Player(2, isSelf: true, "Va Li"));
        router.Accept(Player(1, isSelf: false, "Lisana"));

        Assert.Equal("Lisana", router.View?.Snapshot.Name);
        Assert.Equal("Va Li", router.Stashed?.Snapshot.Name);
    }

    [Fact]
    public void StashedCharacterUpdate_RefreshesStash()
    {
        var router = new CaptureRouter();
        router.Accept(Player(1, isSelf: false, "Lisana"));
        router.Accept(Player(2, isSelf: true, "Va Li"));

        var refreshedSelf = Player(2, isSelf: true, "Va Li") with { JobName = "dark knight" };
        router.Accept(refreshedSelf);

        Assert.Equal("Lisana", router.View?.Snapshot.Name);
        Assert.Equal("dark knight", router.Stashed?.JobName);
    }
}
