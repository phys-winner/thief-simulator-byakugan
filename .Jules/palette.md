## 2025-01-24 - Destructive Action Confirmation
**Learning:** Destructive actions (like unloading a mod) should never be a single-click interaction, especially in a compact UI where accidental clicks are likely. A "two-stage" confirmation or a "Confirm/Cancel" state prevents frustration.
**Action:** Implement a confirmation state for the "Unload & Cleanup" button to ensure user intent.

## 2025-01-24 - Unity IMGUI Redundancy & Real-time Feedback
**Learning:** Unity's `GUILayout.Toggle` renders a checkbox by default; adding manual `[X]` indicators to labels creates visual noise. Additionally, disabling visualization overlays (like ESP) when the menu is open (during a 'pause' state) prevents users from seeing the immediate impact of their settings.
**Action:** Remove manual state indicators from toggle labels and ensure overlays remain visible while the menu is active for real-time feedback.
