import CoreGraphics

/// A connected display: its key (display UUID) and usable area in global screen coordinates (y up).
public struct OrbDisplayInfo: Equatable, Sendable {
    public var key: String
    public var visible: CGRect
    /// Full logical display bounds in points; nil retains the unscaled baseline.
    public var frame: CGRect?

    public init(key: String, visible: CGRect, frame: CGRect? = nil) {
        self.key = key
        self.visible = visible
        self.frame = frame
    }

    public func radius(for orb: OrbConfig) -> CGFloat {
        CGFloat(OrbDisplaySizing.footprint(size: orb.size, style: orb.style, frame: frame)) / 2
    }
}

/// Where an orb's window goes when it is created at launch or restored by undo (DESIGN 2b): its entry for the
/// arrangement (or the arrangement resolver's choice) resolved on that display, else the default corner of the
/// primary display; an orb overlapping one placed before it slides to the nearest free touching position.
public enum OrbLaunchPlacement {
    public static func center(
        of orb: OrbConfig,
        document: OrbsDocument,
        arrangement: String,
        displays: [OrbDisplayInfo],
        main: String,
        primary: OrbDisplayInfo?,
        placed: [OrbDisc] = []
    ) -> CGPoint? {
        let entries = OrbLayoutResolver.entries(document: document, arrangement: arrangement, previousArrangement: nil,
                                                connected: displays.map(\.key), main: main)
        var center: CGPoint
        let display: OrbDisplayInfo
        if let entry = entries[orb.id] ?? nil, let resolved = displays.first(where: { $0.key == entry.display }) ?? primary {
            display = resolved
            center = entry.record.center(radius: display.radius(for: orb), visible: display.visible)
        } else if let primary {
            display = primary
            center = OrbSnap.defaultCenter(radius: display.radius(for: orb), in: display.visible)
        } else {
            return nil
        }
        let radius = display.radius(for: orb)
        let visible = display.visible
        if !OrbPlacement.isFree(center, radius: radius, others: placed) {
            center = OrbPlacement.resolveOverlap(center, radius: radius, others: placed, visible: visible)
        }
        return center
    }
}
