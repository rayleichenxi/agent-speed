using AgentSpeed.Presentation.Layout;
using AgentSpeed.Presentation.Orb;
using AgentSpeed.Presentation.Settings.Orbs;

namespace AgentSpeed.Presentation.Tests;

public class OrbSizingTests
{
    [Theory]
    [InlineData(1920, 1080, 1, 148)]
    [InlineData(1920, 1080, 1.5, 104)]
    [InlineData(2560, 1600, 2, 110)]
    [InlineData(3840, 2160, 2, 148)]
    [InlineData(3840, 2160, 1.5, 148)]
    [InlineData(1080, 1920, 1, 148)]
    public void DefaultsShrinkOnCompactLogicalDesktopsButNeverGrow(double width, double height, double scale, double expected)
    {
        var factor = OrbSizing.DisplayFactor(new RectD(0, 0, width, height), scale);
        Assert.Equal(expected, OrbSizing.Diameter(148, OrbStyle.Led, factor));
    }

    [Fact]
    public void ManualSizeAndEachStylesReadabilityFloorAreRespected()
    {
        var factor = OrbSizing.DisplayFactor(new RectD(0, 0, 1920, 1080), 1.5);
        Assert.Equal(96, OrbSizing.Diameter(112, OrbStyle.Led, factor));
        Assert.Equal(104, OrbSizing.Diameter(148, OrbStyle.Led, factor));
        Assert.Equal(137, OrbSizing.Diameter(196, OrbStyle.Led, factor));
        Assert.Equal(64, OrbSizing.Diameter(64, OrbStyle.Minimal, factor));
        Assert.Equal(96, OrbSizing.Diameter(96, OrbStyle.Needle, factor));
    }

    [Fact]
    public void InvalidDisplayMetricsUseTheBaseline()
    {
        foreach (var bounds in new[] { new RectD(0, 0, 0, 1080), new RectD(0, 0, double.NaN, 1080) })
        {
            Assert.Equal(1, OrbSizing.DisplayFactor(bounds, 1));
        }

        Assert.Equal(148, OrbSizing.Diameter(148, OrbStyle.Led, double.NaN));
    }

    [Fact]
    public void UnpluggingAnExternalScreenKeepsDockingAndTheSavedBaseSize()
    {
        var external = new OrbDisplay("external", new RectD(1920, 0, 2560, 1400), 1, false);
        var laptop = Laptop();
        var orb = new OrbDefinition("x");
        var original = OrbArrangement.Commit(new OrbsDocument([orb]), [external], orb.Id,
            new PointD(external.Work.Right - 12 - 74, external.Work.Bottom - 12 - 74));

        var compact = OrbArrangement.Resolve(original, [laptop]);
        var location = compact.Locations[orb.Id];
        Assert.Equal(78, location.Radius);
        Assert.Equal(new PointD(1920 - 18 - 78, 1040 - 18 - 78), location.Center);
        Assert.Equal(148, compact.Document.Find(orb.Id)!.Size);

        var restored = OrbArrangement.Resolve(compact.Document, [external]);
        Assert.Equal(74, restored.Locations[orb.Id].Radius);
        Assert.Equal(new PointD(external.Work.Right - 12 - 74, external.Work.Bottom - 12 - 74), restored.Locations[orb.Id].Center);
    }

    [Fact]
    public void CrossScreenReleaseAndPositionRoundTripUseTheDestinationSizeEvenAtTheSameDpi()
    {
        var laptop = Laptop();
        var external = new OrbDisplay("external", new RectD(1920, 0, 3840, 2120), 1.5, false);
        var orb = new OrbDefinition("x");
        var document = new OrbsDocument([orb]);
        var centers = new Dictionary<string, PointD> { [orb.Id] = new(2500, 800) };

        var target = OrbArrangement.Release(document, [laptop, external], centers, orb.Id, new PointD(1919, 500))!.Value;
        Assert.Equal(78, target.Radius);
        Assert.Equal(1920 - 18 - 78, target.Center.X);
        var saved = OrbArrangement.Commit(document, [laptop, external], orb.Id, target.Center);
        var restored = OrbArrangement.Resolve(saved, [laptop, external]).Locations[orb.Id];
        Assert.Equal(target.Display, restored.Display);
        Assert.Equal(target.Radius, restored.Radius);
        Assert.Equal(target.Center.X, restored.Center.X, 0.01);
        Assert.Equal(target.Center.Y, restored.Center.Y, 0.01);

        var back = OrbArrangement.Release(saved, [laptop, external], centers, orb.Id, new PointD(5759, 500))!.Value;
        Assert.Equal(111, back.Radius);
        Assert.Equal(5760 - 18 - 111, back.Center.X);
        Assert.Equal(148, saved.Find(orb.Id)!.Size);
    }

    [Fact]
    public void CreatingMultipleOrbsOnTheLaptopUsesAdaptedCollisionRadii()
    {
        var display = Laptop();
        var orbs = new[] { new OrbDefinition("a"), new OrbDefinition("b"), new OrbDefinition("c") };
        var result = OrbArrangement.Resolve(new OrbsDocument(orbs), [display]);
        var placed = new List<OrbDisc>();
        foreach (var orb in orbs)
        {
            var location = result.Locations[orb.Id];
            Assert.Equal(78, location.Radius);
            Assert.True(OrbGeometry.Fits(location.Center, location.Radius, display.Work, display.Scale));
            Assert.True(OrbGeometry.IsFree(location.Center, location.Radius, placed, display.Scale));
            placed.Add(new OrbDisc(location.Center, location.Radius));
        }
    }

    private static OrbDisplay Laptop() => new("laptop", new RectD(0, 0, 1920, 1040), 1.5, true,
        OrbSizing.DisplayFactor(new RectD(0, 0, 1920, 1080), 1.5));
}
