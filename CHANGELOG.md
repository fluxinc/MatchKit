# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - YYYY-MM-DD

### Added

- **Application-Driven Configuration**:
  - `MatchKit.Core.ConfigurationService` to manage settings in the Windows Registry (`HKEY_LOCAL_MACHINE` via `RegistryView.Registry32`).
  - `--config` command-line flag for `MatchKit.exe` (console) to interactively set and save registry defaults via CLI prompts.
  - `--config` command-line flag for `MatchKit.Tray.exe` to launch a GUI form (`ConfigurationForm.cs`) for managing registry defaults.
  - `--save` command-line flag for `MatchKit.exe` (console) to silently save provided operational arguments (window, regex, URL, JSON key, hotkey) to the registry.
- **Memory Bank**: Systematically updated to reflect new features and installer strategies for the core MatchKit application.

### Changed

- **Configuration Storage**: Moved from command-line only or installer-wizard based configuration to persistent registry-based storage for `MatchKit.Tray.exe` and `MatchKit.exe`.
- **Installer `installer/MatchKitSetup.iss` (Generic "MatchKit" version)**:
  - Removed Inno Setup wizard pages for argument collection.
  - Now runs `MatchKit.Tray.exe --config` post-installation for user setup.
  - Desktop shortcut and HKLM startup entry for `MatchKit.Tray.exe` launch without arguments, relying on registry configuration.
  - No Start Menu shortcuts are created (`DisableProgramGroupPage=yes`).
  - "All Users" desktop shortcut (`{commondesktop}`) created for `MatchKit.Tray.exe` (launches without arguments).
  - Installer now requires admin privileges.
- **Application Behavior**:
  - `MatchKit.Tray.exe` and `MatchKit.exe` now load their primary operational settings from the registry if no overriding command-line arguments are provided.
  - Clarified that `MatchKit.Tray.exe` does not handle the `--save` parameter; this is handled by `MatchKit.exe`.

### Removed

- Inno Setup wizard pages for argument collection from `installer/MatchKitSetup.iss`.
- Start Menu shortcut creation from the installer.

### Fixed

- (Implicitly) Previous installer flows that relied on wizard-based argument collection.
- Addressed potential inconsistencies in how application defaults were managed by centralizing to registry via `--config` and `--save` (for `MatchKit.exe`).
