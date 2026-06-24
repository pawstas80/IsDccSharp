# Changelog

## 4.0.0 - 2026-06-24

- Initial C# port of the public `isDcc` InstallShield INX decoder.
- Added a reusable `.NET Framework 4.8` core library.
- Added command-line decoder.
- Added WPF viewer with MVVM, output options, application icon, and logging.
- Added automatic `aLuZ` detection to avoid double-unscrambling.
- Fixed `goto2` alignment for newer INX files.
- Corrected type `13` handling from `TYPE_AUTOSTRING` to `TYPE_UNDEF5`.
- Added Stirling Technologies info string support as a warning.
