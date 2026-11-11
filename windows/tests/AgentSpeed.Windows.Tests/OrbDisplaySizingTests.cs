using AgentSpeed.Core;
using AgentSpeed.Interop;
using AgentSpeed.Orb.Render;
using AgentSpeed.Presentation.Colors;
using AgentSpeed.Presentation.Motion;
using AgentSpeed.Presentation.Orb;
using AgentSpeed.Presentation.Settings;

namespace AgentSpeed.Windows.Tests;

public class OrbDisplaySizingTests
{
    [Fact]
    public void RendererAndLayoutUseTheSameSizeWithoutMutatingTheBaseline()
    {
        var laptop = new MonitorInfo(0, "laptop", new RECT(0, 0, 1920, 1080), new RECT(0, 0, 1920, 1040), true, 144);
        var external = new MonitorInfo(0, "external", new RECT(1920, 0, 5760, 2160), new RECT(1920, 0, 5760, 2120), false, 144);
        var config = new OrbSurfaceConfig(OrbStyle.Led, 148, OrbAppearance.Dark, new EffectsPolicy(EffectsIntensity.Standard),
            false, false, 1, true, true, 100, new ReadoutTexts("tok/s", "PEAK", ["IDLE"], true), null, SourceSet.Empty);

        for (var i = 0; i < 3; i++)
        {
            var adapted = config.ForDisplay(laptop);
            Assert.Equal(104, adapted.Size);
            Assert.Equal(laptop.OrbDisplay.DiameterFor(config.Size, config.Style), adapted.Size);
            Assert.Equal(config with { Size = 104 }, adapted);
            Assert.Same(config, config.ForDisplay(external));
        }

        Assert.Equal(148, config.Size);
        Assert.Same(config, config.ForDisplay(null));
        Assert.Equal(laptop.OrbDisplay.OrbSizeFactor,
            (laptop with { WorkArea = new RECT(0, 0, 1920, 900) }).OrbDisplay.OrbSizeFactor);
    }
}
