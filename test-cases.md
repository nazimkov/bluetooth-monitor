# E2E test cases

This list describes the product cases that are missing from the current E2E
suite. Tests use the fake Bluetooth host and must not depend on real devices,
audio output, tray state, or an external browser.

## Critical battery

- `CriticalBattery_ShowsBannerAndNotification`
  - Open Devices with the fake earbuds selected.
  - Enable the critical alert setting.
  - Set the fake battery to 4% and refresh the selected device.
  - Verify that the critical battery banner is visible.
  - Verify that the notification log contains a `Critical battery` notification
    for 4%.

- `CriticalBattery_DisabledAlert_DoesNotShowCriticalBanner`
  - Disable the critical alert setting.
  - Set the fake battery to 4% and refresh the selected device.
  - Verify that the critical battery banner is not visible.

## General settings

- `StartAtSignIn_PersistsToSettingsJson`
  - Toggle Start at sign-in.
  - Verify that `startAtSignIn` in `settings.json` has the selected value.

- `ThemeSelection_PersistsToSettingsJson`
  - Select Light or Dark.
  - Verify that `theme` in `settings.json` has the selected value.

## Additional cases for later implementation

- Pair New opens the Windows Bluetooth pairing settings URI.
- Privacy Policy opens the configured privacy URI.
- Silent notification style does not create a notification log entry.
- Low-battery hysteresis permits a new notification after the battery rises
  above the threshold plus five points and then falls below the threshold.
- Settings load after an app restart restores the saved controls.
- The empty-device view shows its Pair New action when the fake catalog is
  empty.
