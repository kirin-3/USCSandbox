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

Release `vrcw-3` adds these fixes for shaders that failed in every VRCW run on 2022.3 world bundles:

3. A failed shader also writes its whole exception (`ToString()`, with the stack) to stderr, between `--- VRCW stack begin: <name>` and `--- VRCW stack end`. VRCW attaches it to the shader's result row. The one-line reason on stdout is unchanged.
4. `Processor/ShaderProcessor.cs`, `Processor/BlobManager.cs`: since Unity 2019.3, a platform's programs can span several separately compressed segments, and each blob entry names its segment. Only the first segment was decompressed, so every entry in a later segment read past its end with `EndOfStreamException` (`Lit`, `Hidden/PostProcessing/Uber`, `Bakery/Standard`, Light Volume PBR, orels1 Fish and Standard, Warren's Fast Fur, TsunaMoo Party FX). Each segment is now decompressed from its own offset with `ReadExactly`, and entries are read from their segment.
5. `DirectXProgramToUSIL.cs`, `UShaderFunctionToHLSL.cs`, `USILGetDimensionsFixer.cs`: `sampleinfo dest, rasterizer` threw "operand type Rasterizer not supported" (`Hidden/VRCM4/Line`, Fast Fur Lite). It now becomes `dest = GetRenderTargetSampleCount()`. The output had named the source operand instead of the destination. The fixer that merges `resinfo` with `sampleinfo` read instructions 0 and 1 instead of `i` and `i + 1`, and it now skips a rasterizer `sampleinfo`.
6. `USILForLoopOptimizer.cs`: a loop whose increment is a register, not a constant, threw `IndexOutOfRangeException` (`UniTeiLab/CA_Distorsion`). Such a loop now stays a plain `loop` instead of becoming a `for` loop.

With these, the shaders of the Popcorn Palace, jbc4 and rainydaze bundles all decompile: 85 (Popcorn, without the 6 that VRCW skips because a real source replaces them), 123 and 108, with no failures. Before, they failed on 7, 6 and 7 shaders.

Release `vrcw-4` fixes the emitter for the shaders that still fell back to a generic body after `vrcw-3`. Each fix was checked by decompiling the saved bundles of eleven worlds and compiling every pass in Unity 2022.3.22f1 (D3D11, batch mode):

7. `ShaderProcessor.cs`: a program built for shader model 5.0 gets `#pragma target 5.0` (it can use instructions and outputs such as `SV_Coverage` that `ps_4_0` does not have).
8. `ShaderProcessor.cs`, `UnityShaderConstants.cs`: variables that `UnityShaderVariables.cginc` declares in every program (`unity_OrthoParams`, `unity_CameraProjection`, `unity_WorldToCamera`, `unity_StereoWorldToCamera` and the other camera variables) are no longer declared again.
9. `UShaderFunctionToHLSL.cs`, `DirectXProgramToUSIL.cs`, `SHDR.cs`: a resource register (`rsc<n>`) or constant buffer register (`cb<n>`) that reflection does not name is declared with the type its use implies. `ld` and `ld_structured` become `Load` calls, and a raw buffer is read as `asfloat(buffer.Load4(index * stride + byteOffset))`, with the stride taken from `dcl_resource_structured`.
10. `UShaderFunctionToHLSL.cs`: a texture the program only loads from or asks the size of is a `Texture2D`, not a `sampler2D` (which has no methods). The size of a texture that is also sampled comes from its `<name>_TexelSize` vector, which is declared.
11. `UShaderFunctionToHLSL.cs`, `USILSamplerTypeFixer.cs`, `USILSamplerMetadder.cs`: the `UNITY_DECLARE_TEX*_NOSAMPLER` textures of `UnityShaderVariables.cginc` (`unity_SpecCube1`, `unity_DynamicLightmap`, `unity_ShadowMask` and the like) sample with the sampler of the texture that owns it. The `*_SAMPLER` macros are called with their arguments in the order Unity defines (texture, sampler owner, rest).
12. `USILOperand.cs`: infinity and NaN literals are written as `asfloat(...)`; `float.ToString` wrote `Infinity` and `NaN`, which HLSL does not parse.
13. `UShaderFunctionToHLSL.cs`: `udiv` with a discarded quotient or remainder no longer writes `null`.
14. `ShaderProcessor.cs`: the struct parameters of `UnityInstancing.cginc`'s instanced buffers (`unity_Builtins0Array` and the like) are declared the way `UNITY_INSTANCING_BUFFER_START` declares them: an instancing cbuffer holding an anonymous struct array, with the code's `name.member` reaching the first instance. A cbuffer that Unity's includes do not declare (`UnityDrawCallInfo`, `UnityPerMaterial`, a custom `CBUFFER`) is declared as written, not commented out.
15. `USILForLoopOptimizer.cs`: a loop that could not become a `for` loop no longer leaves its counter name (`i`) behind on the operands after it.
16. `ShaderProcessor.cs`: a ShaderLab `Int` property's default is written as the integer. Unity stores it as float bits, and `1` was written as `1E-45`.
17. `UShaderFunctionToHLSL.cs`: in a vertex input or output struct, members with a system-generated semantic (`SV_InstanceID`, `SV_VertexID`, `SV_IsFrontFace` and the like) are written last. HLSL rejects any parameter after a system-generated one.

### Known limitations of `vrcw-4`

Passes that still fall back to the generic body, grouped by the first error Unity reports. Counts are passes over the eleven worlds, and the causes are not investigated unless the line says so.

| Error | Passes | Example |
| --- | --- | --- |
| `invalid subscript 'x'` | 26 | `Hidden/VRCFury/SpsResolver`, `Hidden/VRCM4/Line Subdivided` (`data`) |
| `$Globals` is over 65536 bytes | 27 | `AudioLink/Internal/AudioLink`: the shader's own material block, once every array is declared, is 131776 bytes |
| a parameter after a system-generated value | 22 | `Mochie/Water`: the line Unity reports is `frag(v2f inp, uint facing : VFACE)` |
| `tex2D` or `Sample` with no matching overload | 18 | `Warren's Fast Fur`, `TsunaMoo/Party FX`: a texture of another dimension is typed `sampler2D` |
| `cannot implicitly convert from 'const float2'` | 15 | `.poiyomi/Poiyomi Toon` |
| `GetDimensions` with five arguments | 13 | `Hidden/ltspass_opaque` |
| `sampler2D object does not have methods` | 9 | `Hidden/SPSPatched/...`: a texture that is both sampled and loaded from |
| `Too few arguments to a macro call` | 10 | `Hidden/ltspass_opaque`, `Mochie/Water` |
| `undeclared identifier 'unity_DynamicLightmap_HDR'` | 7 | `.poiyomi/Poiyomi Toon` |
| `array index out of bounds` | 6 | `Mochie/Standard`, `Mochie/Standard (Lite)` |
| `redefinition of 'unity_WorldToLight'` | 13 | `orels1/Standard`, `orels1/Particles/Fish`: the shader's material block lists it, and `AutoLight.cginc` declares it too in the `POINT`, `SPOT` and cookie variants. Adding it to the included names, or declaring it only when those keywords are off, leaves it undeclared in other variants (65 passes), because `AutoLight.cginc` is not in every program |
| `redefinition of 'unity_BaseInstanceID'` | 8 | `orels1/Particles/Fish`, `Silent/Volumetric Box`: the same trade-off with `UnityInstancing.cginc` |
| `redefinition` of a material property | 4 | `JLChnToZ/VideoRoundCorner` (`_StereoExtend`), `orels1/VFX/Ghost Lines` (`_BakedNoiseTex`) |
| `undeclared identifier '_fallspeed'` | 1 | `sunny/Choco_fountain` |
| `unable to unroll loop` | 1 | `Lit`: 26 iterations without `[loop]` |
| ShaderLab parse error `TVAL_ID` | 4 | `orels1/Toon/Cutout` |

The harness cannot check some passes and reports `Unity did not import this pass` for them: the second `FORWARD` pass of `Rain/RainScroller`, `VRCCoffee/Cafe Liquid`, `VRCCoffee/Ice`, `DummyReflectionWater`, `Thad/Crystal` and `Hidden/SPSPatched/...`. The `vrcw-3` output of the same shaders gets the same result, so it is the check, not the emitter.

## Build

Release `vrcw-1` was built on Windows with the .NET SDK 10.0.302, from a fresh clone of tag `vrcw-1`:

```
git clone --branch vrcw-1 https://github.com/kirin-3/USCSandbox.git
cd USCSandbox
dotnet publish USCSandbox/USCSandbox.csproj -c Release -r win-x64 --self-contained -o out
copy Files\classdata.tpk out\
copy readme.md out\
```

`out\` then got a `SOURCE.txt` naming the source tag, and its contents were zipped (files at the zip root) as `USCSandbox-vrcw-1-win-x64.zip`. `USCSandbox.exe` reads `classdata.tpk` from its working directory.

Release `vrcw-2` changes no code. Tag `vrcw-2` adds upstream's GPL-3.0 `license` (merged from upstream `044bc319`) and this file. Its zip is the `vrcw-1` zip with `license` and the tag's `readme.md` added and `SOURCE.txt` rewritten; every other file is byte-identical to `vrcw-1`, so the binaries are the ones VRCW validated. (A rebuild gives the same code but a different `USCSandbox.dll` hash, because the build path is embedded.)

The source of release `vrcw-2` is tag [`vrcw-2`](https://github.com/kirin-3/USCSandbox/tree/vrcw-2).

Release `vrcw-3` is a rebuild with the fixes above. It was built the same way as `vrcw-1`, from a fresh clone of tag [`vrcw-3`](https://github.com/kirin-3/USCSandbox/tree/vrcw-3), and `license` was also copied into `out\`. Its zip is `USCSandbox-vrcw-3-win-x64.zip`.

Release `vrcw-4` is a rebuild with the fixes above (7 to 17). It was built the same way as `vrcw-3`, from a fresh clone of tag [`vrcw-4`](https://github.com/kirin-3/USCSandbox/tree/vrcw-4), with `license` and `readme.md` copied into `out\` and a `SOURCE.txt` naming the tag. Its zip is `USCSandbox-vrcw-4-win-x64.zip`.
