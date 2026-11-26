import CoreGraphics
import Testing
@testable import AgentSpeedOrbModel

@Suite("Display adaptive orb sizing")
struct OrbDisplaySizingTests {
    @Test func factorsUseLogicalDisplayBoundsAndClamp() {
        #expect(OrbDisplaySizing.factor(frame: CGRect(x: 0, y: 0, width: 1920, height: 1080)) == 1)
        #expect(OrbDisplaySizing.factor(frame: CGRect(x: 0, y: 0, width: 1440, height: 810)) == 0.75)
        #expect(OrbDisplaySizing.factor(frame: CGRect(x: 0, y: 0, width: 800, height: 600)) == 0.7)
        #expect(OrbDisplaySizing.factor(frame: .zero) == 1)
        #expect(OrbDisplaySizing.factor(frame: nil) == 1)
    }

    @Test func diameterRespectsStyleMinimumAndSavedBase() {
        #expect(OrbDisplaySizing.diameter(size: 148, style: .led, factor: 0.75) == 111)
        #expect(OrbDisplaySizing.diameter(size: 112, style: .led, factor: 0.7) == 96)
        #expect(OrbDisplaySizing.diameter(size: 80, style: .led, factor: 0.7) == 80)
        #expect(abs(OrbDisplaySizing.diameter(size: 112, style: .minimal, factor: 0.7) - 78.4) < 0.000001)
        #expect(OrbDisplaySizing.footprint(size: 112, style: .character,
            frame: CGRect(x: 0, y: 0, width: 1440, height: 810)) == 168)
        #expect(OrbDisplaySizing.diameter(size: 148, style: .led, factor: .nan) == 148)
    }

    @Test func launchRadiusFollowsTheResolvedDisplay() {
        let orb = OrbConfig(id: "test", size: 148)
        let compact = OrbDisplayInfo(key: "compact", visible: CGRect(x: 0, y: 0, width: 1440, height: 780),
                                     frame: CGRect(x: 0, y: 0, width: 1440, height: 810))
        let wide = OrbDisplayInfo(key: "wide", visible: CGRect(x: 1440, y: 0, width: 1920, height: 1050),
                                  frame: CGRect(x: 1440, y: 0, width: 1920, height: 1080))
        #expect(compact.radius(for: orb) == 55.5)
        #expect(wide.radius(for: orb) == 74)
        let record = OrbPositionRecord(x: .high(12), y: .high(12))
        let doc = OrbsDocument(orbs: [orb], layouts: ["pair": [orb.id: OrbLayoutEntry(display: compact.key, record: record)]])
        let center = OrbLaunchPlacement.center(of: orb, document: doc, arrangement: "pair", displays: [compact, wide],
                                               main: wide.key, primary: wide)
        #expect(center == CGPoint(x: compact.visible.maxX - 12 - 55.5, y: compact.visible.maxY - 12 - 55.5))
        #expect(orb.size == 148)
    }
}
