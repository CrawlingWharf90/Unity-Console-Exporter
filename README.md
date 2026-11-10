# Console Exporter for Unity

An Editor-only Unity package that adds an enhanced Console Exporter with filtering, optional entry selection, reusable presets, and TXT, JSON, CSV, and XML export formats.

**Free to use in personal and commercial Unity projects. Proprietary source code; modification and redistribution are not permitted.** See [LICENSE.md](LICENSE.md) before installing.

## Requirements

- Unity 2021.2 or newer (the version-specific backend is selected with Unity compilation symbols; compatibility must be verified against each Editor release).
- Git installed and available on the system `PATH` when installing through a Git URL.

> **Compatibility note:** Console Exporter reads Unity Console entries and hooks into the Console toolbar using reflection against Unity Editor implementation details. These are not stable public APIs. Unity can change them without notice, so compatibility is not guaranteed across Editor releases. If the Console changes internally, the toolbar integration or log retrieval may need an update.

## Install through Unity Package Manager

### Test locally

1. Download or clone this repository.
2. In Unity, open **Window → Package Management → Package Manager**.
3. Select **+ → Add package from disk...**.
4. Select this package's `package.json`.

### Install from GitHub

1. Open **Window → Package Management → Package Manager**.
2. Select **+ → Add package from git URL...**.
3. Paste the repository URL, pinned to a release tag:

   ```text
   https://github.com/CrawlingWharf90/Unity-Console-Exporter.git#v1.0.0
   ```

4. Select **Add**.

If you publish under a different GitHub account or organization, replace `CrawlingWharf90` with that owner. The repository must contain `package.json` at its root. Unity's Package Manager retrieves Git dependencies directly from the repository; Git must be installed locally.

## Use

After installation, open the Unity Console. The **Console Exporter** toolbar control should appear if the current Unity version exposes the expected Console implementation hook. The package also provides menu items under **Tools → Console Exporter**.

Available export formats: TXT, JSON, CSV, and XML. Settings include log filters and the optional **Select On Export** workflow. Saved presets are stored in Editor preferences.

## Package contents

- `Editor/`: all seven Editor scripts plus the Editor-only assembly definition.
- `Documentation~/`: package documentation (the tilde suffix keeps it out of the imported project content).
- `package.json`: UPM package metadata.
- `LICENSE.md`: proprietary usage terms.

All C# types live in the `CrawlingWharf90.ConsoleExporter` namespace.

## Update

For reproducible project setups, pin the dependency to a release tag. To update, select the package in Package Manager and use its update action, or enter the Git URL with the desired tag. Then commit the updated `Packages/manifest.json` and `Packages/packages-lock.json` in the consuming Unity project.

## Uninstall

Open Package Manager, select **Console Exporter**, open its package details, and choose **Remove**. Unity removes the package dependency and its package files from the project. Preferences saved through `EditorPrefs` are separate from package files and may remain after removal.

## Development and releases

- Package manifest: `package.json`
- Editor assembly: `Editor/CrawlingWharf90.ConsoleExporter.Editor.asmdef`
- Editor scripts: `Editor/*.cs`
- Changelog: `CHANGELOG.md`
- License: `LICENSE.md`

For a release:

1. Update `version` in `package.json`.
2. Add the changes to `CHANGELOG.md`.
3. Commit the changes to `main`.
4. Create and push a matching annotated Git tag, for example `v1.0.1`.
5. Test installation from that tag in a clean Unity project before announcing it.

## License and source availability

Console Exporter is **proprietary** and is not open-source software. The repository is public to support Git-based UPM installation and source inspection. The license permits installation and use of the unmodified package in personal and commercial Unity projects, but does not permit modification or redistribution of the package or its source code. Read [LICENSE.md](LICENSE.md) for the complete terms.

A public GitHub repository can be viewed and forked under GitHub's Terms of Service. A public repository is not, by itself, an open-source license or a grant of unrestricted rights to use the code.
