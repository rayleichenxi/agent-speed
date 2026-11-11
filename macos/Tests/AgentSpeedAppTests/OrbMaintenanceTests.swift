import AgentSpeedCore
import Foundation
import Testing
@testable import AgentSpeed

@Suite("Maintenance behavior")
@MainActor
struct OrbMaintenanceTests {
    @Test func perThreadAverageIncludesDuplicateSourcesAndHandlesIdle() {
        let feed = OrbFeed()
        feed.gauge.snap(to: 120, now: 0)
        #expect(feed.activeThreads == 0)
        #expect(feed.averagePerThread == nil)
        feed.set(generating: [.codex, .codex, .claudeCode], todayTokens: 0, agentCount: 3)
        #expect(feed.activeThreads == 3)
        #expect(feed.averagePerThread == 40)
        feed.gauge.snap(to: 0, now: 1)
        #expect(feed.averagePerThread == 0)
    }

    @Test func automaticUpdateDefaultDoesNotOverrideSavedPreference() throws {
        let suite = "AgentSpeedMaintenanceTests.\(UUID().uuidString)"
        let defaults = try #require(UserDefaults(suiteName: suite))
        defer { defaults.removePersistentDomain(forName: suite) }
        #expect(!AppSettings(defaults: defaults).autoCheckUpdates)
        let settings = AppSettings(defaults: defaults)
        settings.autoCheckUpdates = true
        #expect(AppSettings(defaults: defaults).autoCheckUpdates)
        settings.autoCheckUpdates = false
        #expect(!AppSettings(defaults: defaults).autoCheckUpdates)
    }
}
