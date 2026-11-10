# Console Exporter

## Overview

Console Exporter provides an enhanced workflow for exporting Unity Editor Console logs.

## Features

- Export as TXT, JSON, CSV, or XML.
- Filter by information, warnings, and errors.
- Select individual entries before export.
- Save and manage export presets.
- Configure the default export format and selection behavior.

## Troubleshooting

If the toolbar control is missing or exporting fails after a Unity upgrade, the Unity Console implementation may have changed. The package currently relies on reflection against internal Unity Editor Console types and members. Check the package README and report the Unity Editor version together with the full exception/log output.
