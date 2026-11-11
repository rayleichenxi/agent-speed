using AgentSpeed.Presentation.Orb;

namespace AgentSpeed.Presentation.Layout;

/// <summary>Floating-orb sizes on compact desktops; settings remain the user's unscaled baseline.</summary>
public static class OrbSizing
{
    private const double ReferenceShortSide = 1080;
    private const double MinimumFactor = 0.7;

    /// <summary>
    /// A full-HD desktop at 100% keeps its size. Smaller logical desktops shrink by at most 30%; larger ones do
    /// not enlarge it. Use display bounds, so taskbar changes do not change the size. DPI is applied once, later.
    /// </summary>
    public static double DisplayFactor(RectD bounds, double scale)
    {
        if (!double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return 1;
        }

        var shortSide = Math.Min(bounds.Width, bounds.Height) / OrbGeometry.ValidScale(scale);
        return Math.Clamp(shortSide / ReferenceShortSide, MinimumFactor, 1);
    }

    /// <summary>The adapted diameter in DIP, bounded by the style's existing readability limits.</summary>
    public static double Diameter(double size, OrbStyle style, double factor) =>
        OrbStyles.ClampSize(style, size * (double.IsFinite(factor) ? Math.Clamp(factor, MinimumFactor, 1) : 1));
}
