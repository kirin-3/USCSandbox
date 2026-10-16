using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Fixers;
using USCSandbox.Processor;
using System.Globalization;
using System.Text;

namespace AssetRipper.Export.Modules.Shaders.UltraShaderConverter.UShader.Function
{
    public class UShaderFunctionToHLSL
    {
        private UShaderProgram _shader;
        private readonly StringBuilder _stringBuilder = new();
        private string _baseIndent;
        private string _indent;
        private int _indentLevel;
        private int _scratchCount;
        private readonly Dictionary<string, int> _textureDims = new(StringComparer.Ordinal);

        private delegate void InstHandler(USILInstruction inst);
        private Dictionary<USILInstructionType, InstHandler> _instructionHandlers;

        public UShaderFunctionToHLSL(UShaderProgram shader, int indentDepth, List<TextureParameter>? textureParams = null)
        {
            _shader = shader;
            if (textureParams != null)
                foreach (TextureParameter param in textureParams)
                    _textureDims.TryAdd(param.Name, param.Dim);

            _baseIndent = new string(' ', indentDepth * 4);
            _indent = new string(' ', 4);

            _instructionHandlers = new()
            {
                { USILInstructionType.Move, new InstHandler(HandleMove) },
                { USILInstructionType.MoveConditional, new InstHandler(HandleMoveConditional) },
                { USILInstructionType.Add, new InstHandler(HandleAdd) },
                { USILInstructionType.Subtract, new InstHandler(HandleSubtract) },
                { USILInstructionType.Multiply, new InstHandler(HandleMultiply) },
                { USILInstructionType.Divide, new InstHandler(HandleDivide) },
                { USILInstructionType.Modulus, new InstHandler(HandleModulus) },
                { USILInstructionType.MultiplyAdd, new InstHandler(HandleMultiplyAdd) },
                { USILInstructionType.And, new InstHandler(HandleAnd) },
                { USILInstructionType.Or, new InstHandler(HandleOr) },
                { USILInstructionType.Xor, new InstHandler(HandleXor) },
                { USILInstructionType.Not, new InstHandler(HandleNot) },
                { USILInstructionType.Minimum, new InstHandler(HandleMinimum) },
                { USILInstructionType.Maximum, new InstHandler(HandleMaximum) },
                { USILInstructionType.SquareRoot, new InstHandler(HandleSquareRoot) },
                { USILInstructionType.SquareRootReciprocal, new InstHandler(HandleSquareRootReciprocal) },
                { USILInstructionType.Logarithm2, new InstHandler(HandleLogarithm2) },
                { USILInstructionType.ToThePower, new InstHandler(HandleToThePower) },
                { USILInstructionType.Reciprocal, new InstHandler(HandleReciprocal) },
                { USILInstructionType.Fractional, new InstHandler(HandleFractional) },
                { USILInstructionType.Floor, new InstHandler(HandleFloor) },
                { USILInstructionType.Ceiling, new InstHandler(HandleCeiling) },
                { USILInstructionType.Round, new InstHandler(HandleRound) },
                { USILInstructionType.Truncate, new InstHandler(HandleTruncate) },
                { USILInstructionType.IntToFloat, new InstHandler(HandleIntToFloat) },
                { USILInstructionType.FloatToInt, new InstHandler(HandleFloatToInt) },
                { USILInstructionType.Sine, new InstHandler(HandleSine) },
                { USILInstructionType.Cosine, new InstHandler(HandleCosine) },
                { USILInstructionType.ShiftLeft, new InstHandler(HandleShiftLeft) },
                { USILInstructionType.ShiftRight, new InstHandler(HandleShiftRight) },
                { USILInstructionType.DotProduct2, new InstHandler(HandleDotProduct) },
                { USILInstructionType.DotProduct3, new InstHandler(HandleDotProduct) },
                { USILInstructionType.DotProduct4, new InstHandler(HandleDotProduct) },
                { USILInstructionType.Sample, new InstHandler(HandleSample) },
                { USILInstructionType.SampleComparison, new InstHandler(HandleSample) },
                { USILInstructionType.SampleComparisonLODZero, new InstHandler(HandleSample) },
                { USILInstructionType.SampleLOD, new InstHandler(HandleSampleLOD) },
                { USILInstructionType.SampleDerivative, new InstHandler(HandleSampleDerivative) },
                { USILInstructionType.LoadResource, new InstHandler(HandleLoadResource) },
                { USILInstructionType.LoadResourceMultisampled, new InstHandler(HandleLoadResource) },
                { USILInstructionType.LoadResourceStructured, new InstHandler(HandleLoadResourceStructured) },
                { USILInstructionType.Discard, new InstHandler(HandleDiscard) },
                { USILInstructionType.ResourceDimensionInfo, new InstHandler(HandleResourceDimensionInfo) },
                { USILInstructionType.SampleCountInfo, new InstHandler(HandleSampleCountInfo) },
                { USILInstructionType.GetDimensions, new InstHandler(HandleResourceDimensionInfo) },
                { USILInstructionType.DerivativeRenderTargetX, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.DerivativeRenderTargetY, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.DerivativeRenderTargetXCoarse, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.DerivativeRenderTargetYCoarse, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.DerivativeRenderTargetXFine, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.DerivativeRenderTargetYFine, new InstHandler(HandleDerivativeRenderTarget) },
                { USILInstructionType.IfFalse, new InstHandler(HandleIf) },
                { USILInstructionType.IfTrue, new InstHandler(HandleIf) },
                { USILInstructionType.Else, new InstHandler(HandleElse) },
                { USILInstructionType.EndIf, new InstHandler(HandleEndIf) },
                { USILInstructionType.Loop, new InstHandler(HandleLoop) },
                { USILInstructionType.EndLoop, new InstHandler(HandleEndLoop) },
                { USILInstructionType.Break, new InstHandler(HandleBreak) },
                { USILInstructionType.Continue, new InstHandler(HandleContinue) },
                { USILInstructionType.ForLoop, new InstHandler(HandleForLoop) },
                { USILInstructionType.Switch, new InstHandler(HandleSwitch) },
                { USILInstructionType.Case, new InstHandler(HandleCase) },
                { USILInstructionType.Default, new InstHandler(HandleDefault) },
                { USILInstructionType.EndSwitch, new InstHandler(HandleEndSwitch) },
                { USILInstructionType.Equal, new InstHandler(HandleEqual) },
                { USILInstructionType.NotEqual, new InstHandler(HandleNotEqual) },
                { USILInstructionType.LessThan, new InstHandler(HandleLessThan) },
                { USILInstructionType.LessThanOrEqual, new InstHandler(HandleLessThanOrEqual) },
                { USILInstructionType.GreaterThan, new InstHandler(HandleGreaterThan) },
                { USILInstructionType.GreaterThanOrEqual, new InstHandler(HandleGreaterThanOrEqual) },
                { USILInstructionType.Return, new InstHandler(HandleReturn) },
                // extra
                { USILInstructionType.MultiplyMatrixByVector, new InstHandler(MultiplyMatrixByVector) },
                { USILInstructionType.Comment, new InstHandler(HandleComment) }
            };
        }

        private static readonly HashSet<string> SystemGeneratedSemantics = new(StringComparer.OrdinalIgnoreCase)
        {
            "SV_InstanceID", "SV_PrimitiveID", "SV_VertexID", "SV_IsFrontFace", "SV_SampleIndex", "SV_RenderTargetArrayIndex", "SV_ViewportArrayIndex"
        };

        // HLSL does not accept a parameter after a system-generated value in a signature, so those members go last.
        private static IEnumerable<USILInputOutput> SystemGeneratedLast(IEnumerable<USILInputOutput> members)
        {
            List<USILInputOutput> all = members.ToList();
            return all.Where(m => !SystemGeneratedSemantics.Contains(m.type ?? "")).Concat(all.Where(m => SystemGeneratedSemantics.Contains(m.type ?? "")));
        }

        public string WriteStruct()
        {
            _stringBuilder.Clear();

            if (_shader.shaderFunctionType == UShaderFunctionType.Vertex)
            {
                AppendLine("struct appdata");
                AppendLine("{");
                _indentLevel++;
                foreach (USILInputOutput input in SystemGeneratedLast(_shader.inputs))
                {
                    AppendLine($"{input.format} {input.name} : {input.type};");
                }
                _indentLevel--;
                AppendLine("};");

                AppendLine("struct v2f");
                AppendLine("{");
                _indentLevel++;
                foreach (USILInputOutput output in SystemGeneratedLast(_shader.outputs))
                {
                    AppendLine($"{output.format} {output.name} : {output.type};");
                }
                _indentLevel--;
                AppendLine("};");
            }
            else if (_shader.shaderFunctionType == UShaderFunctionType.Fragment)
            {
                AppendLine("struct fout");
                AppendLine("{");
                _indentLevel++;
                foreach (USILInputOutput output in _shader.outputs)
                {
                    AppendLine($"{output.format} {output.name} : {output.type};");
                }
                _indentLevel--;
                AppendLine("};");
            }

            return _stringBuilder.ToString();
        }

        public string WriteFunction(bool retainVolumeSamplers = false)
        {
            _stringBuilder.Clear();

            WriteFunctionDefinition();
            {
                WriteLocals();
                if (retainVolumeSamplers) WriteVolumeSamplerBindings();
                foreach (USILInstruction inst in _shader.instructions)
                {
                    if (_instructionHandlers.ContainsKey(inst.instructionType))
                    {
                        _instructionHandlers[inst.instructionType](inst);
                    }
                }
            }
            _indentLevel--;
            AppendLine("}");

            return _stringBuilder.ToString();
        }

        private void WriteVolumeSamplerBindings()
        {
            foreach (string owner in GetSamplerOwners().Where(n => _textureDims.TryGetValue(n, out int dim) && dim == 3).Order(StringComparer.Ordinal))
            {
                string prefix = $"rscSamplerBinding{_scratchCount++}";
                AppendLine($"uint {prefix}Width, {prefix}Height, {prefix}Depth, {prefix}Levels;");
                AppendLine($"{owner}.GetDimensions(0, {prefix}Width, {prefix}Height, {prefix}Depth, {prefix}Levels);");
                // Unity cannot bind a borrowed sampler after FXC removes its owning texture. A valid
                // hardware texture width is below 2^31, so its bit pattern is a nonnegative float.
                // This never discards/returns, but retains that resource without changing the sampler.
                if (_shader.shaderFunctionType == UShaderFunctionType.Fragment)
                    AppendLine($"clip(asfloat({prefix}Width));");
                else if (_shader.shaderFunctionType == UShaderFunctionType.Vertex)
                    AppendLine($"if (asfloat({prefix}Width) < 0) return ({USILConstants.VERT_TO_FRAG_STRUCT_NAME})0;");
            }
        }

        private void WriteFunctionDefinition()
        {
            if (_shader.shaderFunctionType == UShaderFunctionType.Vertex)
            {
                AppendLine($"{USILConstants.VERT_TO_FRAG_STRUCT_NAME} vert(appdata {USILConstants.VERT_INPUT_NAME})");
            }
            else
            {
                var frontFace = _shader.inputs.FirstOrDefault(i => i.type == "SV_IsFrontFace");
                string args = $"{USILConstants.VERT_TO_FRAG_STRUCT_NAME} {USILConstants.FRAG_INPUT_NAME}";
                if (frontFace is not null)
                {
                    // not part of v2f
                    args += $", {frontFace.format} {frontFace.name}: VFACE";
                }
                AppendLine($"{USILConstants.FRAG_OUTPUT_STRUCT_NAME} frag({args})");
            }
            AppendLine("{");
            _indentLevel++;
        }

        private void WriteLocals()
        {
            foreach (USILLocal local in _shader.locals)
            {
                if (local.defaultValues.Count > 0 && local.isArray)
                {
                    AppendLine($"{local.type} {local.name}[{local.defaultValues.Count}] = {{");
                    if (local.defaultValues.Count > 0)
                    {
                        _indentLevel++;
                        for (int i = 0; i < local.defaultValues.Count; i++)
                        {
                            USILOperand operand = local.defaultValues[i];
                            string comma = i != local.defaultValues.Count - 1 ? "," : "";
                            AppendLine($"{operand}{comma}");
                        }
                        _indentLevel--;
                    }
                    AppendLine("};");
                }
                else
                {
                    AppendLine($"{local.type} {local.name};");
                }
            }
        }

        private void HandleMove(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleMoveConditional(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} ? {srcOps[1]} : {srcOps[2]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleAdd(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} + {srcOps[1]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleSubtract(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} - {srcOps[1]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleMultiply(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} * {srcOps[1]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleDivide(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} / {srcOps[1]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleModulus(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {srcOps[0]} % {srcOps[1]};");
        }

        private void HandleMultiplyAdd(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"{srcOps[0]} * {srcOps[1]} + {srcOps[2]}");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleAnd(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            int op0UintSize = srcOps[0].GetValueCount();
            int op1UintSize = srcOps[1].GetValueCount();
            string value = $"uint{op0UintSize}({srcOps[0]}) & uint{op1UintSize}({srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleOr(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            int op0UintSize = srcOps[0].GetValueCount();
            int op1UintSize = srcOps[1].GetValueCount();
            string value = $"uint{op0UintSize}({srcOps[0]}) | uint{op1UintSize}({srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleXor(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            int op0UintSize = srcOps[0].GetValueCount();
            int op1UintSize = srcOps[1].GetValueCount();
            string value = $"uint{op0UintSize}({srcOps[0]}) ^ uint{op1UintSize}({srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleNot(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            int op0UintSize = srcOps[0].GetValueCount();
            string value = $"~uint{op0UintSize}({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleMinimum(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"min({srcOps[0]}, {srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleMaximum(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"max({srcOps[0]}, {srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleSquareRoot(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"sqrt({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleSquareRootReciprocal(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"rsqrt({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleLogarithm2(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"log({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleToThePower(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"pow({srcOps[0]}, {srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleReciprocal(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"rcp({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleFractional(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"frac({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleFloor(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"floor({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleCeiling(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"ceil({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleRound(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"round({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleTruncate(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"trunc({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleIntToFloat(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"floor({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleFloatToInt(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"asint({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleSine(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"sin({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleCosine(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"cos({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleShiftLeft(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand srcOp0 = srcOps[0];
            USILOperand srcOp1 = srcOps[1];

            // temp fix to prevent compile errors, still innacurate
            int op0IntSize = srcOp0.GetValueCount();
            int op1IntSize = srcOp1.GetValueCount();

            string op0Text, op1Text;

            if (srcOp0.operandType == USILOperandType.ImmediateInt)
            {
                op0Text = $"{srcOp0}";
            }
            else
            {
                op0Text = $"int{op0IntSize}({srcOp0})";
            }

            if (srcOp1.operandType == USILOperandType.ImmediateInt)
            {
                op1Text = $"{srcOp1}";
            }
            else
            {
                op1Text = $"int{op1IntSize}({srcOp1})";
            }

            string value = $"float{op0IntSize}({op0Text} << {op1Text})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleShiftRight(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand srcOp0 = srcOps[0];
            USILOperand srcOp1 = srcOps[1];

            // temp fix to prevent compile errors, still innacurate
            int op0IntSize = srcOp0.GetValueCount();
            int op1IntSize = srcOp1.GetValueCount();

            string op0Text, op1Text;

            if (srcOp0.operandType == USILOperandType.ImmediateInt)
            {
                op0Text = $"{srcOp0}";
            }
            else
            {
                op0Text = $"int{op0IntSize}({srcOp0})";
            }

            if (srcOp1.operandType == USILOperandType.ImmediateInt)
            {
                op1Text = $"{srcOp1}";
            }
            else
            {
                op1Text = $"int{op1IntSize}({srcOp1})";
            }

            string value = $"float{op0IntSize}({op0Text} >> {op1Text})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleDotProduct(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = WrapSaturate(inst, $"dot({srcOps[0]}, {srcOps[1]})");
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        // The sampler a *_SAMPLER macro is given: the texture itself, or for a NOSAMPLER built-in the texture that owns its sampler.
        private static string SamplerArgument(USILOperand texture) =>
            texture.metadataName != null && USILSamplerTypeFixer.SHARED_SAMPLER_TEXTURE_NAMES.TryGetValue(texture.metadataName, out string? owner)
                ? owner
                : texture.ToString();

        private void HandleSample(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand textureOperand = srcOps[2];
            SampleRef reference = ResolveSample(inst);
            // A separate sample names the read texture; otherwise the sampler names it, as before.
            string tex = reference.FromResource ? reference.Tex : $"{srcOps[2]}";
            string coord = $"{srcOps[0]}";
            // A 3D or cube sample addressed with a 2D coordinate reads direction (x, y, 0), as the packed
            // texCUBE form did (D2, as in HandleSampleLOD): a combined sampler converted implicitly.
            string sampleCoord = reference.TexType is USILOperandType.Sampler3D or USILOperandType.SamplerCube &&
                srcOps[0].mask.Length == 2 ? $"float3({coord}, 0)" : coord;
            string args = $"{tex}, {coord}";
            string value;
            if (!reference.Separate)
            {
                // VRCW vrcw-6 (D2): a texture object has no tex2D; its own-sampler samples go through UNITY_SAMPLE_*.
                if (GetRewrittenNames().Contains(textureOperand.metadataName ?? ""))
                {
                    value = reference.TexType switch
                    {
                        USILOperandType.Sampler2D => $"UNITY_SAMPLE_TEX2D({args})",
                        USILOperandType.Sampler3D => $"UNITY_SAMPLE_TEX3D({tex}, {sampleCoord})",
                        USILOperandType.SamplerCube => $"UNITY_SAMPLE_TEXCUBE({tex}, {sampleCoord})",
                        USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY({args})",
                        USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY({args})",
                        _ => $"texND({args})" // unknown real type
                    };
                }
                else
                {
                    value = textureOperand.operandType switch
                    {
                        USILOperandType.Sampler2D => $"tex2D({args})",
                        USILOperandType.Sampler3D => $"tex3D({args})",
                        USILOperandType.SamplerCube => $"texCUBE({args})",
                        USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY({args})",
                        USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY({args})",
                        _ => $"texND({args})" // unknown real type
                    };
                }
            }
            else
            {
                // args is "texture, rest": the macros take (texture, the texture whose sampler is used, rest).
                args = $"{tex}, {reference.Owner}, {sampleCoord}";
                value = reference.TexType switch
                {
                    USILOperandType.Sampler2D => $"UNITY_SAMPLE_TEX2D_SAMPLER({args})",
                    USILOperandType.Sampler3D => $"UNITY_SAMPLE_TEX3D_SAMPLER({args})",
                    USILOperandType.SamplerCube => $"UNITY_SAMPLE_TEXCUBE_SAMPLER({args})",
                    USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_SAMPLER({args})",
                    USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER({args})",
                    _ => $"texND({args})" // unknown real type
                };
            }
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }


        private void HandleSampleLOD(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand textureOperand = srcOps[2];
            SampleRef reference = ResolveSample(inst);
            string tex = reference.FromResource ? reference.Tex : $"{srcOps[2]}";
            string coord = $"{srcOps[0]}", lod = $"{srcOps[3]}";
            string args;
            if (srcOps[0].mask.Length == 2) // texture2d
            {
                args = $"{tex}, float4({coord}, 0, {lod})";
            }
            else
            {
                args = $"{tex}, float4({coord}, {lod})";
            }
            // A 2D-array coordinate is float3 (uv plus the slice): passed as coord.xyz, never packed with the level.
            string arrayCoord = srcOps[0].mask.Length == 3 ? coord : $"({coord}).xyz";
            // A 3D or cube sample addressed with a 2D coordinate reads direction (x, y, 0), as the packed
            // texCUBE lod form did.
            string volumeCoord = srcOps[0].mask.Length == 3 ? coord : srcOps[0].mask.Length == 2 ? $"float3({coord}, 0)" : $"({coord}).xyz";

            string value;
            if (!reference.Separate)
            {
                // VRCW vrcw-6 (D2): Unity's array LOD macros take coordinate and level as separate arguments;
                // packing them into one float4 is "Too few arguments to a macro call". A texture object has no
                // tex2Dlod; it samples with its own sampler through UNITY_SAMPLE_*_LOD.
                bool rewrite = GetRewrittenNames().Contains(textureOperand.metadataName ?? "");
                value = reference.TexType switch
                {
                    USILOperandType.Sampler2D => rewrite ? $"UNITY_SAMPLE_TEX2D_LOD({tex}, {coord}, {lod})" : $"tex2Dlod({args})",
                    USILOperandType.Sampler3D => rewrite ? $"UNITY_SAMPLE_TEX3D_LOD({tex}, {volumeCoord}, {lod})" : $"tex3Dlod({args})",
                    USILOperandType.SamplerCube => rewrite ? $"UNITY_SAMPLE_TEXCUBE_LOD({tex}, {volumeCoord}, {lod})" : $"texCUBElod({args})",
                    USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_LOD({tex}, {arrayCoord}, {lod})",
                    USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_LOD({tex}, {coord}, {lod})",
                    _ => $"texNDlod({args})" // unknown real type
                };
            }
            else
            {
                // VRCW vrcw-6 (D2): LOD sampling through a separate sampler keeps its level with the _SAMPLER_LOD
                // macros. The non-LOD macros sample the wrong mip (and, for arrays, do not compile).
                value = reference.TexType switch
                {
                    USILOperandType.Sampler2D => $"UNITY_SAMPLE_TEX2D_SAMPLER_LOD({tex}, {reference.Owner}, {coord}, {lod})",
                    USILOperandType.Sampler3D => $"UNITY_SAMPLE_TEX3D_SAMPLER_LOD({tex}, {reference.Owner}, {volumeCoord}, {lod})",
                    USILOperandType.SamplerCube => $"UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD({tex}, {reference.Owner}, {volumeCoord}, {lod})",
                    USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_SAMPLER_LOD({tex}, {reference.Owner}, {arrayCoord}, {lod})",
                    USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD({tex}, {reference.Owner}, {coord}, {lod})",
                    _ => $"texND({tex}, {reference.Owner}, {args.Substring(tex.Length + 2)})" // unknown real type
                };
            }
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }


        private void HandleSampleDerivative(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand textureOperand = srcOps[2];
            SampleRef reference = ResolveSample(inst);
            string tex = reference.FromResource ? reference.Tex : $"{srcOps[2]}";
            string value;
            string args = $"{tex}, {srcOps[0]}, {srcOps[3]}, {srcOps[4]}";
            // VRCW vrcw-6 (D2): a texture object has no tex2Dgrad; it samples with explicit gradients through
            // its own sampler, or through the borrowed one. Derivative-sampled textures always keep a sampler.
            if (reference.Separate)
            {
                value = $"{tex}.SampleGrad(sampler{reference.Owner}, {srcOps[0]}, {srcOps[3]}, {srcOps[4]})";
            }
            else if (GetRewrittenNames().Contains(textureOperand.metadataName ?? "") &&
                reference.TexType is USILOperandType.Sampler2D or USILOperandType.Sampler3D or USILOperandType.SamplerCube)
            {
                value = $"{tex}.SampleGrad(sampler{tex}, {srcOps[0]}, {srcOps[3]}, {srcOps[4]})";
            }
            else
            {
                value = textureOperand.operandType switch
                {
                    USILOperandType.Sampler2D => $"tex2Dgrad({args})",
                    USILOperandType.Sampler3D => $"tex3Dgrad({args})",
                    USILOperandType.SamplerCube => $"texCUBEgrad({args})",
                    _ => $"texNDgrad({args})" // unknown real type
                };
            }
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }


        // The components of a loaded value the destination takes: the resource's swizzle, as many as the destination has.
        private static string Swizzle(USILOperand operand, USILOperand? destination)
        {
            if (operand.mask == null || operand.mask.Length == 0)
            {
                return "";
            }

            IEnumerable<int> mask = operand.mask;
            if (destination != null && destination.mask.Length > 0 && destination.mask.Length < operand.mask.Length)
            {
                mask = mask.Take(destination.mask.Length);
            }

            string swizzle = string.Concat(mask.Select(m => USILConstants.MASK_CHARS[m]));
            return swizzle == "xyzw" ? "" : "." + swizzle;
        }

        private void HandleLoadResource(USILInstruction inst)
        {
            // dx: ld dest, address, resource. HLSL reads texels through the resource object: resource.Load(int3(x, y, mip)).
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand address = srcOps[0];
            USILOperand resource = srcOps[1];
            int components = Math.Max(1, address.GetValueCount());
            string coordinate = components == 1 ? $"int({address})" : $"int{components}({address})";
            string value = $"{resource.ToString(true)}.Load({coordinate}){Swizzle(resource, inst.destOperand)}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleLoadResourceStructured(USILInstruction inst)
        {
            // dx: ld_structured dest, index, byteOffset, buffer. The element layout is not in the program, so the buffer
            // is read as raw dwords: asfloat(buffer.Load4(index * stride + byteOffset)).
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand address = srcOps[0];
            USILOperand offset = srcOps[1];
            USILOperand resource = srcOps[2];
            int stride = srcOps.Count > 3 && srcOps[3].immValueInt is { Length: > 0 } && srcOps[3].immValueInt[0] > 0 ? srcOps[3].immValueInt[0] : 16;
            string byteOffset = offset.immValueInt is { Length: > 0 } ? offset.immValueInt[0].ToString(CultureInfo.InvariantCulture) : $"uint({offset})";
            string value = $"asfloat({resource.ToString(true)}.Load4(uint({address}) * {stride} + {byteOffset})){Swizzle(resource, inst.destOperand)}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private IEnumerable<USILOperand> AllOperands(USILOperand? operand)
        {
            if (operand == null)
            {
                yield break;
            }

            yield return operand;
            foreach (USILOperand child in AllOperands(operand.arrayRelative))
            {
                yield return child;
            }

            if (operand.children != null)
            {
                foreach (USILOperand childOperand in operand.children)
                {
                    foreach (USILOperand child in AllOperands(childOperand))
                    {
                        yield return child;
                    }
                }
            }
        }

        private IEnumerable<(USILInstruction Instruction, USILOperand Operand)> AllInstructionOperands()
        {
            foreach (USILInstruction instruction in _shader.instructions)
            {
                foreach (USILOperand operand in AllOperands(instruction.destOperand))
                {
                    yield return (instruction, operand);
                }

                foreach (USILOperand source in instruction.srcOperands ?? new List<USILOperand>())
                {
                    foreach (USILOperand operand in AllOperands(source))
                    {
                        yield return (instruction, operand);
                    }
                }
            }
        }

        private static bool IsDimensionInfo(USILInstruction instruction) =>
            instruction.instructionType == USILInstructionType.ResourceDimensionInfo || instruction.instructionType == USILInstructionType.GetDimensions;

        private HashSet<string>? _sampledNames;

        /// <summary>Named textures the program samples (tex2D and the like).</summary>
        private HashSet<string> SampledTextureNames()
        {
            if (_sampledNames == null)
            {
                _sampledNames = new();
                foreach (var (_, operand) in AllInstructionOperands())
                {
                    if (operand.metadataNameAssigned && !string.IsNullOrEmpty(operand.metadataName) &&
                        operand.operandType is USILOperandType.Sampler2D or USILOperandType.Sampler3D or USILOperandType.SamplerCube
                            or USILOperandType.Sampler2DArray or USILOperandType.SamplerCubeArray)
                    {
                        _sampledNames.Add(operand.metadataName!);
                    }
                }
            }

            return _sampledNames;
        }

        private static bool IsNamedSampler(USILOperand operand) =>
            operand.metadataNameAssigned && !string.IsNullOrEmpty(operand.metadataName) &&
            operand.operandType is USILOperandType.Sampler2D or USILOperandType.Sampler3D or USILOperandType.SamplerCube
                or USILOperandType.Sampler2DArray or USILOperandType.SamplerCubeArray;

        // A sample instruction's sampler flag: Sample carries it at 3, the others at 4. SampleDerivative has none.
        private static int SamplerFlagIndex(USILInstructionType type) => type switch
        {
            USILInstructionType.Sample => 3,
            USILInstructionType.SampleComparison or USILInstructionType.SampleComparisonLODZero or USILInstructionType.SampleLOD => 4,
            _ => -1,
        };

        private static bool IsNamedResource(USILOperand operand) =>
            operand.metadataNameAssigned && !string.IsNullOrEmpty(operand.metadataName) &&
            operand.operandType == USILOperandType.ResourceRegister && !operand.bufferResource;

        private static bool IsSampleInstruction(USILInstruction instruction) =>
            instruction.instructionType == USILInstructionType.Sample ||
            instruction.instructionType == USILInstructionType.SampleComparison ||
            instruction.instructionType == USILInstructionType.SampleComparisonLODZero ||
            instruction.instructionType == USILInstructionType.SampleLOD ||
            instruction.instructionType == USILInstructionType.SampleDerivative;

        private static USILOperandType DimOperandType(int dim) => dim switch
        {
            3 => USILOperandType.Sampler3D,
            4 => USILOperandType.SamplerCube,
            5 => USILOperandType.Sampler2DArray,
            6 => USILOperandType.SamplerCubeArray,
            _ => USILOperandType.Sampler2D,
        };

        /// <summary>A sample's read texture and sampler (VRCW vrcw-6, D2).</summary>
        private readonly record struct SampleRef(string Tex, string Owner, USILOperandType TexType, bool Separate, bool FromResource);

        /// <summary>
        /// Which texture a sample reads and whose sampler it uses (VRCW vrcw-6, D2). A sample whose resource and
        /// sampler name different textures reads the resource with the sampler's state; otherwise the sampler names
        /// the texture, as before. Falls back to the sampler when the resource is unnamed or its dimension unknown.
        /// </summary>
        private SampleRef ResolveSample(USILInstruction instruction)
        {
            USILOperand resource = instruction.srcOperands[1], sampler = instruction.srcOperands[2];
            int flagIndex = SamplerFlagIndex(instruction.instructionType);
            bool flag = flagIndex >= 0 && instruction.srcOperands.Count > flagIndex &&
                instruction.srcOperands[flagIndex].immValueInt is { Length: > 0 } ints && ints[0] == 1;
            if (!flag && IsNamedSampler(sampler) && IsNamedResource(resource) && resource.metadataName != sampler.metadataName
                && _textureDims.TryGetValue(resource.metadataName!, out int dim))
            {
                return new SampleRef(resource.metadataName!, SamplerArgument(sampler), DimOperandType(dim), true, true);
            }
            return new SampleRef(sampler.metadataName ?? "", SamplerArgument(sampler), sampler.operandType, flag, false);
        }

        private Dictionary<string, USILOperandType>? _textureObjects;
        private HashSet<string>? _loadedNames;
        private HashSet<string>? _ownSampledNames;
        private HashSet<string>? _samplerOwners;
        private HashSet<string>? _rewrittenNames;

        /// <summary>
        /// Textures this program reads through a separate sampler (VRCW vrcw-6, D2), by name and read dimension:
        /// declared as texture objects, not sampler2D.
        /// </summary>
        public Dictionary<string, USILOperandType> GetTextureObjects()
        {
            if (_textureObjects == null)
            {
                _textureObjects = new();
                foreach (USILInstruction instruction in _shader.instructions)
                {
                    if (!IsSampleInstruction(instruction) || instruction.srcOperands.Count < 3) continue;
                    SampleRef reference = ResolveSample(instruction);
                    if (reference.Separate && !USILSamplerTypeFixer.SHARED_SAMPLER_TEXTURE_NAMES.ContainsKey(reference.Tex)
                        && !IsBuiltinTexture(reference.Tex))
                        _textureObjects.TryAdd(reference.Tex, reference.TexType);
                }
            }

            return _textureObjects;
        }

        /// <summary>Textures this program reads texel by texel (ld) or asks the size of.</summary>
        public HashSet<string> GetLoadedNames()
        {
            if (_loadedNames == null)
            {
                _loadedNames = new();
                foreach (var (instruction, operand) in AllInstructionOperands())
                {
                    if (!IsNamedResource(operand)) continue;
                    if (instruction.instructionType == USILInstructionType.LoadResource || instruction.instructionType == USILInstructionType.LoadResourceMultisampled ||
                        IsDimensionInfo(instruction))
                    {
                        _loadedNames.Add(operand.metadataName!);
                    }
                }
            }

            return _loadedNames;
        }
        private Dictionary<string, USILOperandType>? _loadedTypes;

        /// <summary>Load-only textures by read dimension (VRCW vrcw-6, D2): the ld address width tells
        /// 2D (int3) from 2D-array (int4) apart when reflection names no sampler.</summary>
        public Dictionary<string, USILOperandType> GetLoadedTypes()
        {
            if (_loadedTypes == null)
            {
                _loadedTypes = new(StringComparer.Ordinal);
                foreach (USILInstruction instruction in _shader.instructions)
                {
                    if (instruction.instructionType != USILInstructionType.LoadResource &&
                        instruction.instructionType != USILInstructionType.LoadResourceMultisampled) continue;
                    if (instruction.srcOperands.Count < 2) continue;
                    USILOperand resource = instruction.srcOperands[1];
                    if (!IsNamedResource(resource)) continue;
                    int width = 3;
                    try { width = Math.Max(1, instruction.srcOperands[0].GetValueCount()); } catch { width = 3; }
                    USILOperandType type = width >= 4 ? USILOperandType.Sampler2DArray : USILOperandType.Sampler2D;
                    if (type == USILOperandType.Sampler2DArray) _loadedTypes[resource.metadataName!] = type;
                    else _loadedTypes.TryAdd(resource.metadataName!, type);
                }
            }

            return _loadedTypes;
        }

        /// <summary>
        /// Textures this program also samples through their own sampler (VRCW vrcw-6, D2): they keep a sampler
        /// declaration (through UNITY_DECLARE_TEX2D and the like) beside their texture object.
        /// </summary>
        public HashSet<string> GetOwnSampledNames()
        {
            if (_ownSampledNames == null)
            {
                _ownSampledNames = new();
                foreach (USILInstruction instruction in _shader.instructions)
                {
                    if (!IsSampleInstruction(instruction) || instruction.srcOperands.Count < 3) continue;
                    if (instruction.instructionType == USILInstructionType.SampleDerivative)
                    {
                        if (IsNamedSampler(instruction.srcOperands[2])) _ownSampledNames.Add(instruction.srcOperands[2].metadataName!);
                        continue;
                    }
                    SampleRef reference = ResolveSample(instruction);
                    if (!reference.Separate && IsNamedSampler(instruction.srcOperands[2])) _ownSampledNames.Add(instruction.srcOperands[2].metadataName!);
                }
            }

            return _ownSampledNames;
        }

        /// <summary>
        /// Sampler owners this program borrows (VRCW vrcw-6, D2): textures whose sampler state a separate sample
        /// uses. Each keeps a sampler declaration so the borrowed sampler exists.
        /// </summary>
        public HashSet<string> GetSamplerOwners()
        {
            if (_samplerOwners == null)
            {
                _samplerOwners = new(StringComparer.Ordinal);
                foreach (USILInstruction instruction in _shader.instructions)
                {
                    if (!IsSampleInstruction(instruction) || instruction.srcOperands.Count < 3) continue;
                    SampleRef reference = ResolveSample(instruction);
                    if (reference.Separate && reference.Owner != reference.Tex
                        && !USILSamplerTypeFixer.SHARED_SAMPLER_TEXTURE_NAMES.ContainsKey(reference.Owner)
                        && !IsBuiltinTexture(reference.Owner))
                        _samplerOwners.Add(reference.Owner);
                }
            }

            return _samplerOwners;
        }

        /// <summary>Textures whose own-sampler samples are written through UNITY_SAMPLE_* (D2).</summary>
        public HashSet<string> GetRewrittenNames()
        {
            if (_rewrittenNames == null)
            {
                // Owners upgraded to UNITY_DECLARE_* need their own samples rewritten too (a texture object
                // has no tex2D).
                _rewrittenNames = new(GetOwnSampledNames(), StringComparer.Ordinal);
                _rewrittenNames.IntersectWith(GetTextureObjects().Keys.Concat(GetLoadedNames()).Concat(GetSamplerOwners()));
            }

            return _rewrittenNames;
        }

        private static bool IsBuiltinTexture(string name) =>
            UnityShaderConstants.BUILTIN_TEXTURE_NAMES.Contains(name);



        private static string TextureObjectType(int dimension) => dimension switch
        {
            1 => "Buffer<float4>",
            2 => "Texture1D<float4>",
            4 => "Texture2DMS<float4>",
            5 => "Texture3D<float4>",
            6 => "TextureCube<float4>",
            7 => "Texture1DArray<float4>",
            8 => "Texture2DArray<float4>",
            9 => "Texture2DMSArray<float4>",
            10 => "TextureCubeArray<float4>",
            _ => "Texture2D<float4>",
        };

        /// <summary>
        /// Declarations for what the program reads and reflection does not name: a resource register (rsc0) and a constant buffer
        /// register (cb0), and the buffers reflection names as buffers. Names already in <paramref name="declared"/> are skipped.
        /// </summary>
        /// <summary>
        /// Unnamed constant-buffer registers this program stores to (VRCW vrcw-6, fix 22): the compiled program
        /// writes them (AudioLink's self-copy pass stores its previous frame into cb0/cb1). A cbuffer member
        /// cannot be assigned, so the pass emits those registers as writable global arrays instead.
        /// </summary>
        public HashSet<int> GetUnnamedBufferWrites()
        {
            HashSet<int> writes = new();
            foreach (USILInstruction instruction in _shader.instructions)
            {
                foreach (USILOperand operand in AllOperands(instruction.destOperand))
                {
                    if (operand.operandType == USILOperandType.ConstantBuffer && !operand.metadataNameAssigned)
                        writes.Add(operand.registerIndex);
                }
            }
            return writes;
        }

        public string WriteUnnamedDeclarations(HashSet<string> declared, int depth, Dictionary<int, int>? passConstantBuffers = null)
        {
            Dictionary<string, string> resources = new();
            Dictionary<int, (int Size, bool Relative)> constantBuffers = new();
            foreach (var (instruction, operand) in AllInstructionOperands())
            {
                if (operand.operandType == USILOperandType.ResourceRegister)
                {
                    bool structured = instruction.instructionType == USILInstructionType.LoadResourceStructured;
                    bool loaded = instruction.instructionType == USILInstructionType.LoadResource || instruction.instructionType == USILInstructionType.LoadResourceMultisampled;
                    string name = operand.metadataNameAssigned ? operand.metadataName ?? "" : $"rsc{operand.registerIndex}";
                    // Named texture parameters are declared with the program's textures; a name from the buffer bindings is not.
                    if (name.Length == 0 || operand.metadataNameAssigned && !operand.bufferResource)
                    {
                        continue;
                    }

                    if (structured)
                    {
                        resources[name] = "ByteAddressBuffer";
                    }
                    else if (loaded && !resources.ContainsKey(name))
                    {
                        resources[name] = TextureObjectType(instruction.srcOperands.Count > 2 && instruction.srcOperands[2].immValueInt is { Length: > 0 } ? instruction.srcOperands[2].immValueInt[0] : 3);
                    }
                    else if (!resources.ContainsKey(name))
                    {
                        resources[name] = operand.bufferResource ? "ByteAddressBuffer" : "Texture2D<float4>";
                    }
                }
                else if (operand.operandType == USILOperandType.ConstantBuffer && !operand.metadataNameAssigned)
                {
                    constantBuffers.TryGetValue(operand.registerIndex, out var known);
                    int observed = operand.arrayRelative != null ? 4096 : operand.arrayIndex + 1;
                    // The declared length wins only when it is the larger: a program can index past a shorter
                    // declaration (AudioLink's self-copy stores into cb1 beyond its dcl_constantbuffer length).
                    int size = _shader.constantBufferSizes.TryGetValue(operand.registerIndex, out int reflectedSize)
                        ? Math.Max(reflectedSize, observed) : observed;
                    constantBuffers[operand.registerIndex] = (Math.Max(known.Size, size), known.Relative || operand.arrayRelative != null);
                }
            }

            StringBuilder sb = new();
            string indent = new string(' ', depth * 4);

            foreach ((string name, string type) in resources.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                if (declared.Add(name))
                {
                    sb.AppendLine($"{indent}{type} {name}; // no name in the shader's reflection data");
                }
            }

            foreach ((int register, (int size, bool _)) in constantBuffers.OrderBy(c => c.Key))
            {
                if (passConstantBuffers != null)
                {
                    passConstantBuffers.TryGetValue(register, out int known);
                    passConstantBuffers[register] = Math.Max(known, size);
                }
                else if (declared.Add($"cb{register}"))
                {
                    sb.AppendLine($"{indent}float4 cb{register}[{size}]; // constant buffer {register}, no names in the shader's reflection data");
                }
            }

            return sb.ToString();
        }

        /// <summary>Textures whose size is read and which are also sampled (VRCW): their _TexelSize
        /// vector is ensured beside their declaration (see HandleResourceDimensionInfo).</summary>
        private HashSet<string>? _texelSizeNames;
        public HashSet<string> GetTexelSizeNames()
        {
            if (_texelSizeNames == null)
            {
                // The size of a texture that is also sampled comes from its _TexelSize vector (see HandleResourceDimensionInfo).
                _texelSizeNames = new();
                foreach (var (instruction, operand) in AllInstructionOperands())
                {
                    if (IsDimensionInfo(instruction) && operand.operandType == USILOperandType.ResourceRegister && operand.metadataNameAssigned &&
                        !operand.bufferResource && operand.metadataName != null && SampledTextureNames().Contains(operand.metadataName))
                    {
                        if (!_textureDims.TryGetValue(operand.metadataName, out int dim) || dim is < 2 or > 6)
                            _texelSizeNames.Add(operand.metadataName);
                    }
                }
            }

            return _texelSizeNames;
        }

        private void HandleDiscard(USILInstruction inst)
        {
            string comment = CommentString(inst);
            AppendLine($"{comment}discard;");
        }

        private void HandleResourceDimensionInfo(USILInstruction inst)
        {
            // assumes resinfo_extra exists
            List<USILOperand> srcOps = inst.srcOperands;

            USILOperand usilResource = srcOps[0];
            USILOperand usilMipLevel = srcOps[1];
            USILOperand usilWidth = srcOps[2];
            USILOperand usilHeight = srcOps[3];
            USILOperand usilDepthOrArraySize = srcOps[4];
            USILOperand usilMipCount = srcOps[5];

            // _TexelSize cannot provide array length, volume depth or mip count. Keep the native
            // resource query, with integer scratch outputs rather than casts used as out l-values.
            if (!usilResource.bufferResource && usilResource.metadataName is string resourceName &&
                _textureDims.TryGetValue(resourceName, out int dimension) && dimension is >= 2 and <= 6)
            {
                bool depth = dimension is 3 or 5 or 6;
                string[] scratch = Enumerable.Range(0, depth ? 4 : 3).Select(_ => $"rscDimension{_scratchCount++}").ToArray();
                AppendLine($"uint {string.Join(", ", scratch)};");
                AppendLine($"{CommentString(inst)}{resourceName}.GetDimensions((uint)({usilMipLevel}), {string.Join(", ", scratch)});");
                var outputs = depth ? new[] { usilWidth, usilHeight, usilDepthOrArraySize, usilMipCount }
                    : new[] { usilWidth, usilHeight, usilMipCount };
                for (int i = 0; i < outputs.Length; i++)
                    if (outputs[i].operandType != USILOperandType.Null) AppendLine($"{outputs[i]} = {scratch[i]};");
                return;
            }

            // A sampled texture is a sampler2D, which has no GetDimensions; its _TexelSize vector holds (1/w, 1/h, w, h).
            // A texture only loaded from has no sampler either; its _TexelSize vector (ensured beside its
            // declaration) serves the same way.
            if (usilResource.metadataNameAssigned && !usilResource.bufferResource && usilResource.metadataName != null &&
                (SampledTextureNames().Contains(usilResource.metadataName) || GetLoadedNames().Contains(usilResource.metadataName)))
            {
                string texelSize = $"{usilResource.metadataName}_TexelSize";
                string commentLine = CommentString(inst);
                if (usilWidth.operandType != USILOperandType.Null)
                {
                    AppendLine($"{commentLine}{usilWidth} = {texelSize}.z;");
                }

                if (usilHeight.operandType != USILOperandType.Null)
                {
                    AppendLine($"{commentLine}{usilHeight} = {texelSize}.w;");
                }

                if (usilDepthOrArraySize.operandType != USILOperandType.Null)
                {
                    AppendLine($"{commentLine}{usilDepthOrArraySize} = 1.0;");
                }

                if (usilMipCount.operandType != USILOperandType.Null)
                {
                    AppendLine($"{commentLine}{usilMipCount} = 1.0;");
                }

                return;
            }

            List<string> args = new List<string>();

            // GetDimensions needs a variable for every output it is given, so an unused output gets a scratch one.
            string Output(USILOperand operand)
            {
                if (operand.operandType != USILOperandType.Null)
                {
                    return operand.ToString();
                }

                string scratch = $"rscUnused{_scratchCount++}";
                AppendLine($"float {scratch};");
                return scratch;
            }

            if (usilMipLevel.immValueFloat[0] == 0 && usilMipCount.operandType == USILOperandType.Null)
            {
                // shorter version (not checking the compiler did this correctly!)
                args.Add(Output(usilWidth));

                if (usilHeight.operandType != USILOperandType.Null)
                {
                    args.Add(usilHeight.ToString());
                }

                if (usilDepthOrArraySize.operandType != USILOperandType.Null)
                {
                    args.Add(usilDepthOrArraySize.ToString());
                }
            }
            else
            {
                args.Add(usilMipLevel.ToString());
                args.Add(Output(usilWidth));

                if (usilHeight.operandType != USILOperandType.Null)
                {
                    args.Add(usilHeight.ToString());
                }

                if (usilDepthOrArraySize.operandType != USILOperandType.Null)
                {
                    args.Add(usilDepthOrArraySize.ToString());
                }

                if (usilMipCount.operandType != USILOperandType.Null)
                {
                    args.Add(usilMipCount.ToString());
                }
                else
                {
                    args.Add(Output(usilMipCount));
                }
            }

            string call = $"GetDimensions({string.Join(", ", args)})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{usilResource}.{call};");
        }

        private void HandleSampleCountInfo(USILInstruction inst)
        {
            string value = $"{inst.destOperand} = GetRenderTargetSampleCount()";
            string comment = CommentString(inst);
            AppendLine($"{comment}{value};");
        }

        private void HandleDerivativeRenderTarget(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string fun = inst.instructionType switch
            {
                USILInstructionType.DerivativeRenderTargetX => "ddx",
                USILInstructionType.DerivativeRenderTargetY => "ddy",
                USILInstructionType.DerivativeRenderTargetXCoarse => "ddx_coarse",
                USILInstructionType.DerivativeRenderTargetYCoarse => "ddy_coarse",
                USILInstructionType.DerivativeRenderTargetXFine => "ddx_fine",
                USILInstructionType.DerivativeRenderTargetYFine => "ddy_fine",
                _ => "dd?"
            };
            string value = $"{fun}({srcOps[0]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleIf(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string comment = CommentString(inst);
            if (inst.instructionType == USILInstructionType.IfTrue)
            {
                AppendLine($"{comment}if ({srcOps[0]}) {{");
            }
            else
            {
                AppendLine($"{comment}if (!({srcOps[0]})) {{");
            }

            _indentLevel++;
        }

        private void HandleElse(USILInstruction inst)
        {
            _indentLevel--;
            string comment = CommentString(inst);
            AppendLine($"{comment}}} else {{");
            _indentLevel++;
        }

        private void HandleEndIf(USILInstruction inst)
        {
            _indentLevel--;
            string comment = CommentString(inst);
            AppendLine($"{comment}}}");
        }

        private void HandleLoop(USILInstruction inst)
        {
            // this can create bad optos and should be
            // replaced with USILXXXLoopOptimizer if possible.
            // VRCW vrcw-6 (D1): a DXBC loop block means the original compiler kept a real loop, so [loop]
            // restates what it did. Without it the shader compiler tries to unroll the loop and gives up
            // (Popcorn's Lit: "unable to unroll loop, loop does not appear to terminate in a timely manner").
            string comment = CommentString(inst);
            AppendLine($"{comment}[loop] while (true) {{");
            _indentLevel++;
        }

        private void HandleEndLoop(USILInstruction inst)
        {
            _indentLevel--;
            string comment = CommentString(inst);
            AppendLine($"{comment}}}");
        }

        private void HandleBreak(USILInstruction inst)
        {
            string comment = CommentString(inst);
            AppendLine($"{comment}break;");
        }

        private void HandleContinue(USILInstruction inst)
        {
            string comment = CommentString(inst);
            AppendLine($"{comment}continue;");
        }

        private void HandleForLoop(USILInstruction inst)
        {
            string comment = CommentString(inst);

            USILOperand iterRegOp = inst.srcOperands[0];
            USILOperand compOp = inst.srcOperands[1];
            USILInstructionType compType = (USILInstructionType)inst.srcOperands[2].immValueInt[0];
            USILNumberType numberType = (USILNumberType)inst.srcOperands[3].immValueInt[0];
            float addCount = inst.srcOperands[4].immValueFloat[0]; // todo use an int instead of float when int incremented?
            int depth = inst.srcOperands[5].immValueInt[0];

            string numberTypeName = numberType switch
            {
                USILNumberType.Float => "float",
                USILNumberType.Int => "int",
                USILNumberType.UnsignedInt => "unsigned int",
                _ => "?"
            };
            string iterName = USILConstants.ITER_CHARS[depth].ToString(); // better hope someone's not crazy enough to go over
            string compText = compType switch
            {
                USILInstructionType.Equal => "==",
                USILInstructionType.NotEqual => "!=",
                USILInstructionType.GreaterThan => ">",
                USILInstructionType.GreaterThanOrEqual => ">=",
                USILInstructionType.LessThan => "<",
                USILInstructionType.LessThanOrEqual => "<=",
                _ => "?"
            };

            AppendLine(
                // VRCW vrcw-6 (D1): as in HandleLoop, a real loop keeps [loop] so the compiler does not try to unroll it.
                $"{comment}[loop] for ({numberTypeName} {iterName} = {iterRegOp}; " +
                $"{iterName} {compText} {compOp}; " +
                $"{iterName} += {addCount.ToString(CultureInfo.InvariantCulture)}) {{"
            );

            _indentLevel++;
        }

        private void HandleSwitch(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string comment = CommentString(inst);
            AppendLine($"{comment}switch ({srcOps[0]}) {{");

            _indentLevel++;
        }

        private void HandleCase(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string comment = CommentString(inst);
            AppendLine($"{comment}case {srcOps[0]}:");
        }

        private void HandleDefault(USILInstruction inst)
        {
            string comment = CommentString(inst);
            AppendLine($"{comment}default:");
        }

        private void HandleEndSwitch(USILInstruction inst)
        {
            _indentLevel--;
            string comment = CommentString(inst);
            AppendLine($"{comment}}}");
        }

        private void HandleEqual(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} == {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleNotEqual(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} != {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleLessThan(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} < {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleLessThanOrEqual(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} <= {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleGreaterThan(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} > {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleGreaterThanOrEqual(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"{srcOps[0]} >= {srcOps[1]}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleReturn(USILInstruction inst)
        {
            string outputName = _shader.shaderFunctionType switch
            {
                UShaderFunctionType.Vertex => USILConstants.VERT_OUTPUT_LOCAL_NAME,
                UShaderFunctionType.Fragment => USILConstants.FRAG_OUTPUT_LOCAL_NAME,
                _ => "o" // ?
            };

            string value = $"return {outputName}";
            string comment = CommentString(inst);
            AppendLine($"{comment}{value};");
        }

        private void MultiplyMatrixByVector(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            string value = $"mul({srcOps[0]}, {srcOps[1]})";
            string comment = CommentString(inst);
            AppendLine($"{comment}{inst.destOperand} = {value};");
        }

        private void HandleComment(USILInstruction inst)
        {
            AppendLine($"//{inst.destOperand.comment};");
        }

        private string WrapSaturate(USILInstruction inst, string str)
        {
            if (inst.saturate)
            {
                str = $"saturate({str})";
            }
            return str;
        }

        private void AppendLine(string line)
        {
            _stringBuilder.Append(_baseIndent);

            for (int i = 0; i < _indentLevel; i++)
            {
                _stringBuilder.Append(_indent);
            }

            _stringBuilder.AppendLine(line);
        }

        // this is awful
        private string CommentString(USILInstruction inst)
        {
            return inst.commented ? "//" : "";
        }
    }
}
