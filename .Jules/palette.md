## 2025-01-24 - Destructive Action Confirmation
**Learning:** Destructive actions (like unloading a mod) should never be a single-click interaction, especially in a compact UI where accidental clicks are likely. A "two-stage" confirmation or a "Confirm/Cancel" state prevents frustration.
**Action:** Implement a confirmation state for the "Unload & Cleanup" button to ensure user intent.

## 2025-01-24 - Unity IMGUI Redundancy & Real-time Feedback
**Learning:** Unity's `GUILayout.Toggle` renders a checkbox by default; adding manual `[X]` indicators to labels creates visual noise. Additionally, disabling visualization overlays (like ESP) when the menu is open (during a 'pause' state) prevents users from seeing the immediate impact of their settings.
**Action:** Remove manual state indicators from toggle labels and ensure overlays remain visible while the menu is active for real-time feedback.

## 2025-01-24 - Reactive UI Hints for Feature Conflicts
**Learning:** Disabling a UI element without explanation (silent failure) is frustrating for users. When features conflict (e.g., Transparent Walls vs. White Walls), the dependent feature should be visually disabled, and a status hint should explain why it is unavailable.
**Action:** Use 'GUI.enabled' to prevent invalid states and 'GUI.contentColor' to provide grayed-out "N/A" hints that guide the user.
