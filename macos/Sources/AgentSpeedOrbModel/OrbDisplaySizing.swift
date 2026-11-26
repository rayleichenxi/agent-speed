import CoreGraphics
import Foundation

/// Windows-compatible compact-display sizing in logical screen points, without changing the saved base size.
public enum OrbDisplaySizing {
    public static func factor(frame: CGRect?) -> Double {
        guard let frame, frame.width.isFinite, frame.height.isFinite, frame.width > 0, frame.height > 0 else { return 1 }
        return min(1, max(0.7, Double(min(frame.width, frame.height)) / 1080))
    }

    public static func diameter(size: Double, style: OrbStyleKind, factor: Double) -> Double {
        let baseline = OrbSizes.clamp(size)
        let minimum = (style == .minimal || style == .character) ? OrbSizes.range.lowerBound : min(baseline, 96)
        let scale = factor.isFinite ? min(1, max(0.7, factor)) : 1
        return max(minimum, OrbSizes.clamp(baseline * scale))
    }

    public static func footprint(size: Double, style: OrbStyleKind, frame: CGRect?) -> Double {
        diameter(size: size, style: style, factor: factor(frame: frame)) * style.footprint
    }
}
