# VRCW build of USCSandbox

This branch is a modified version of [USCSandbox](https://github.com/nesrak1/USCSandbox) by nesrak1, built for the VRCW world recovery tools.

## License

USCSandbox is licensed under the GNU General Public License v3.0 (see `license`), which upstream added in [`044bc319`](https://github.com/nesrak1/USCSandbox/commit/044bc319866ca2f7a570942cb55d74458de7e2c0). This fork and its releases are distributed under the same license. The upstream repository is https://github.com/nesrak1/USCSandbox.

## Base

Upstream commit [`563391b849116de5d470420cc0e3f1bc6a4b6157`](https://github.com/nesrak1/USCSandbox/commit/563391b849116de5d470420cc0e3f1bc6a4b6157) ("Merge pull request #3 from ShiyumeMeguri/AddInfo").

## Changes

Two commits on top of the base:

1. `USCSandbox/Processor/ShaderProcessor.cs` and `USCSandbox/Program.cs`:
   - Shaders built with `#pragma target 5.0` (DX11 SM5.0 programs only) are decompiled through the SM4.0 path instead of coming out as empty passes.
   - `--skip <file>`: shader names, one per line, that are not decompiled.
   - `--only <file>`: shader path IDs, one per line; with `--all`, only these are decompiled.
   - A shader that fails prints `<name> failed: <type>: <message>` and the run continues.
   - No `dbg_entry_*.bin` debug dumps are written to the working directory.
2. `USCSandbox/USCSandbox.csproj`: `TargetFramework` is `net10.0` instead of `net8.0`.

## Build

Release `vrcw-1` was built on Windows with the .NET SDK 10.0.302, from a fresh clone of tag `vrcw-1`:

```
git clone --branch vrcw-1 https://github.com/kirin-3/USCSandbox.git
cd USCSandbox
dotnet publish USCSandbox/USCSandbox.csproj -c Release -r win-x64 --self-contained -o out
copy Files\classdata.tpk out\
copy readme.md out\
```

`out\` then gets a `SOURCE.txt` naming the source tag and stating that upstream publishes no license, and its contents are zipped (files at the zip root) as `USCSandbox-vrcw-1-win-x64.zip`. `USCSandbox.exe` reads `classdata.tpk` from its working directory.

The source of release `vrcw-1` is tag [`vrcw-1`](https://github.com/kirin-3/USCSandbox/tree/vrcw-1).
