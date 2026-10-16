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

Release `vrcw-5` fixes the `$Globals` row of the table below:

18. `ShaderProcessor.cs`: a cbuffer that Unity's includes do not declare is written as a real `CBUFFER_START`/`CBUFFER_END` around all of its variables. `vrcw-4` commented the markers out and closed the block after the first variable, so the variables landed in `$Globals`. AudioLink's eight `float[1023]` sample arrays made it 131776 bytes, and UnityShaderCompiler.exe failed on the import (`Internal error communicating with the shader compiler process`). The vertex and fragment programs of a pass share one file, so only the first program to use a cbuffer writes the wrapper; the other's variables not yet declared stay plain globals. An instancing cbuffer whose name is taken gets `_Instanced` appended. On for-my-love's `AudioLink/Internal/AudioLink`, 11 of 12 passes now compile, against 10 with `vrcw-4`. The one left fails on `array index out of bounds` in its `STEREO_INSTANCING_ON` variant.

Release `vrcw-6` fixes the loop, LOD and separate-sampler rows of the table below (design D1–D3 of VRCW's `recover-more-from-seven-worlds` change):

19. `UShaderFunctionToHLSL.cs`: `HandleLoop` and `HandleForLoop` write real loops with `[loop]`, so the compiler keeps them loops instead of trying to unroll them. Popcorn's `Lit` no longer fails with `unable to unroll loop (26 iterations)`; its ForwardBase pass compiles in the scenario variant. No gradient-in-loop error appeared, so there is no explicit-gradient fallback.
20. `UShaderFunctionToHLSL.cs`, `ShaderProcessor.cs`: a sample whose resource and sampler name different textures reads the resource with the sampler's state. 2D-array LOD passes coordinate and LOD separately (`UNITY_SAMPLE_TEX2DARRAY_LOD(tex, coord.xyz, lod)`), and so does cube-array LOD. LOD sampling through a separate sampler uses the `_SAMPLER_LOD` macros with coordinate and LOD split the same way (a 2D coordinate on a 3D or cube texture reads direction (x, y, 0), as the packed `texCUBE lod` form did). Non-LOD sampling promotes the same 2D coordinates to `float3` in the new `UNITY_SAMPLE_*` and `_SAMPLER` emissions (the old `tex2D`/`texCUBE` forms keep their coordinates). The read texture is declared as a texture object (`Texture2D<float4>` and the like, or `UNITY_DECLARE_TEX2D` with its own-sampler samples rewritten through `UNITY_SAMPLE_*` when its own sampler samples it too); the borrowed sampler is paired with its texture through `UNITY_DECLARE_*`, since a bare `SamplerState` without its texture does not compile. The built-in shared-sampler textures keep today's path. A derivative sample of a texture object uses `SampleGrad` with its sampler. A texture whose reflection names no sampler (only loaded from, never bound to one) gets its declaration appended when the pass has none: `Texture2D` (or `Texture2DArray` when an `ld` address is four-wide) for load-only names, the reflection dimension otherwise, `UNITY_DECLARE_*` when its own sampler samples it too. Built-ins are never declared or upgraded (Unity's includes own them). Upgrades stay inside the program's own section: a global replace would upgrade other programs' branches, whose uses may still need the sampler form. "Is it declared" checks stay inside the program's own section, since declarations live in per-variant branches, and names held as cbuffer members are never redeclared as textures. Textures with a known reflected dimension use native dimension queries with writable unsigned scratch outputs, retaining depth, array length and mip count. The legacy texel-size fallback is used only when the dimension is unknown. Unity binds a `sampler<name>` only when the compiled program still binds the texture `<name>`: the compiler drops an unused declaration from the reflection, and Unity's import then rejects the sampler (`Unrecognized sampler 'sampler_udonlightvolume'`, Silent/Foliage's ForwardBase, which borrows the volume's sampler while reading nothing of the volume itself). A program that borrows a 3D volume sampler whose texture has no surviving read of its own keeps the texture bound with a `GetDimensions` and a use of its width that never fires: `clip(asfloat(width))` in a fragment program and a width-below-zero early return in a vertex program (a real texture width is below 2^31, so its bit pattern is a nonnegative float).
21. `ShaderProcessor.cs`: a pass whose sub-programs include geometry (types 19/20), hull or domain programs is not decompiled. Its body is an empty `// VRCRecovery: not decompiled: <stage> stage` marker, and VRCW's splice records that reason without a compile check, in place of a misleading compile error such as `invalid subscript 'texcoord1'`.
22. `ShaderProcessor.cs`: collect shader-owned constant buffers across the pass's programs and variants before emitting declarations. Each buffer has one wrapper, the union of its members in reflected offset order, and the largest reflected array and instance counts. `$Globals` and Unity-declared buffers keep their previous handling. `DirectXProgramToUSIL` also carries `dcl_constantbuffer` lengths into the program: unnamed `cbN` arrays use the largest declared length across the pass in separate buffers, instead of retaining the first program's shorter observed usage or overflowing `$Globals`. This addresses the array bounds errors revealed by the LTCGI loop fix. A register the compiled program stores to (AudioLink's self-copy pass writes its previous frame into `cb0`/`cb1`, past their declared lengths) keeps a writable global array — a cbuffer member cannot be assigned — sized to the larger of the declared length and the largest index used.

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

Release `vrcw-6` was measured on a paired harness over seven worlds' shader sets (prepared form, one Unity session per world, `vrcw-5` against `vrcw-6`): 58 fallen-back passes become 42, with no pass lost. `unable to unroll loop` (Lit), the `Too few arguments to a macro call` row (ten of the twenty fallen-back `5ka/*` ForwardBase passes now decompile; the rest stop on `cannot use casts on l-values`, a limitation below), `sampler2D object does not have methods` (Graphlit/Lit decompiles all three passes; the SPS passes now stop on `redefinition` pairs instead — a texture loaded by one program and sampled through its own sampler by another needs pass-level declaration unification, future work), and the `array index out of bounds` readings of `cb0` (Lit's LTCGI loop, AudioLink's self-copy) are the rows this release clears. Known texture dimensions use native queries with unsigned scratch outputs; the legacy texel-size fallback remains for unknown dimensions (fix 20). The remaining `cannot use casts on l-values` failures (ten `5ka/*` ForwardBase passes) come from a USIL pattern the emitter writes as a cast on an assignable destination; fixing it is future work.

The full Popcorn recovery also exposes a deliberate consequence of fix 21 outside that paired selection: `RED_SIM/Fire` previously compiled two vertex/fragment-only passes even though its programs include hull and domain stages. Those passes now keep generic bodies with an explicit unsupported-stage reason, and one material moves from decompiled to reconstructed. The selected-set no-loss result does not cover this Fire loss; it must be considered separately when approving the release.

DEV's full recovery report gains all twelve AudioLink passes against the October 14 baseline, but loses `Hidden/ltspass_opaque` pass 0.4 on an unrecognized `sampler_udonpointlightvolumetexture`. That shader has zero world materials. It is outside the selected paired harness; this comparison does not establish whether the diagnostic first appeared in vrcw-5 or vrcw-6. DEV's material tiers and 1397 attached Udon records remain unchanged.

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

Release `vrcw-5` is a rebuild with fix 18. It was built the same way as `vrcw-4`, from a fresh clone of tag [`vrcw-5`](https://github.com/kirin-3/USCSandbox/tree/vrcw-5), with `license` and `readme.md` copied into `out\` and a `SOURCE.txt` naming the tag. Its zip is `USCSandbox-vrcw-5-win-x64.zip`.

Release `vrcw-6` uses fixes 19–22, built with the same self-contained Windows x64 publish command from the modified `vrcw` source tree. Its archive preserves the exact binaries used by the seven-set paired Unity comparison and VRCW recoveries, rather than substituting a later rebuild. `USCSandbox.dll` has SHA-256 `00d3baa66b5bf9e678e5bad6f68f2cd4095041a725d38cacf5d47c061c2fc967`. The archive adds `classdata.tpk`, `license`, `readme.md`, this file and a `SOURCE.txt` identifying tag `vrcw-6`. Its zip is `USCSandbox-vrcw-6-win-x64.zip`.
