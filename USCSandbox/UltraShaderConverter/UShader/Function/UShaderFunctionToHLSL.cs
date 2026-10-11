using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL;
using AssetRipper.Export.Modules.Shaders.UltraShaderConverter.USIL.Fixers;
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

        private delegate void InstHandler(USILInstruction inst);
        private Dictionary<USILInstructionType, InstHandler> _instructionHandlers;

        public UShaderFunctionToHLSL(UShaderProgram shader, int indentDepth)
        {
            _shader = shader;

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

        public string WriteFunction()
        {
            _stringBuilder.Clear();

            WriteFunctionDefinition();
            {
                WriteLocals();
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
            int samplerTypeIdx = inst.instructionType == USILInstructionType.Sample ? 3 : 4;
            bool samplerType = srcOps[samplerTypeIdx].immValueInt[0] == 1;
            string args = $"{srcOps[2]}, {srcOps[0]}";
            string value;
            if (!samplerType)
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
            else
            {
                // args is "texture, rest": the macros take (texture, the texture whose sampler is used, rest).
                args = $"{srcOps[2]}, {SamplerArgument(textureOperand)}, {args.Substring(srcOps[2].ToString().Length + 2)}";
                value = textureOperand.operandType switch
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
            bool samplerType = srcOps[4].immValueInt[0] == 1;
            string args;
            if (srcOps[0].mask.Length == 2) // texture2d
            {
                args = $"{srcOps[2]}, float4({srcOps[0]}, 0, {srcOps[3]})";
            }
            else
            {
                args = $"{srcOps[2]}, float4({srcOps[0]}, {srcOps[3]})";
            }

            string value;
            if (!samplerType)
            {
                value = textureOperand.operandType switch
                {
                    USILOperandType.Sampler2D => $"tex2Dlod({args})",
                    USILOperandType.Sampler3D => $"tex3Dlod({args})",
                    USILOperandType.SamplerCube => $"texCUBElod({args})",
                    USILOperandType.Sampler2DArray => $"UNITY_SAMPLE_TEX2DARRAY_LOD({args})",
                    USILOperandType.SamplerCubeArray => $"UNITY_SAMPLE_TEXCUBEARRAY_LOD({args})",
                    _ => $"texNDlod({args})" // unknown real type
                };
            }
            else
            {
                // args is "texture, rest": the macros take (texture, the texture whose sampler is used, rest).
                args = $"{srcOps[2]}, {SamplerArgument(textureOperand)}, {args.Substring(srcOps[2].ToString().Length + 2)}";
                value = textureOperand.operandType switch
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

        private void HandleSampleDerivative(USILInstruction inst)
        {
            List<USILOperand> srcOps = inst.srcOperands;
            USILOperand textureOperand = srcOps[2];
            string value;
            string args = $"{srcOps[2]}, {srcOps[0]}, {srcOps[3]}, {srcOps[4]}";
            value = textureOperand.operandType switch
            {
                USILOperandType.Sampler2D => $"tex2Dgrad({args})",
                USILOperandType.Sampler3D => $"tex3Dgrad({args})",
                USILOperandType.SamplerCube => $"texCUBEgrad({args})",
                _ => $"texNDgrad({args})" // unknown real type
            };
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

        /// <summary>Named textures this program reads texel by texel (ld) or asks the size of, and never samples: they are Texture objects, not sampler2D.</summary>
        public HashSet<string> GetTextureObjectNames()
        {
            HashSet<string> objects = new();
            foreach (var (instruction, operand) in AllInstructionOperands())
            {
                if (!operand.metadataNameAssigned || string.IsNullOrEmpty(operand.metadataName) ||
                    operand.operandType != USILOperandType.ResourceRegister || operand.bufferResource)
                {
                    continue;
                }

                if (instruction.instructionType == USILInstructionType.LoadResource || instruction.instructionType == USILInstructionType.LoadResourceMultisampled ||
                    IsDimensionInfo(instruction))
                {
                    objects.Add(operand.metadataName!);
                }
            }

            objects.ExceptWith(SampledTextureNames());
            return objects;
        }

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
        public string WriteUnnamedDeclarations(HashSet<string> declared, int depth)
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
                    int size = operand.arrayRelative != null ? 4096 : operand.arrayIndex + 1;
                    constantBuffers[operand.registerIndex] = (Math.Max(known.Size, size), known.Relative || operand.arrayRelative != null);
                }
            }

            // The size of a texture that is also sampled comes from its _TexelSize vector (see HandleResourceDimensionInfo).
            HashSet<string> texelSizes = new();
            foreach (var (instruction, operand) in AllInstructionOperands())
            {
                if (IsDimensionInfo(instruction) && operand.operandType == USILOperandType.ResourceRegister && operand.metadataNameAssigned &&
                    !operand.bufferResource && operand.metadataName != null && SampledTextureNames().Contains(operand.metadataName))
                {
                    texelSizes.Add(operand.metadataName);
                }
            }

            StringBuilder sb = new();
            string indent = new string(' ', depth * 4);
            foreach (string name in texelSizes.Order(StringComparer.Ordinal))
            {
                if (declared.Add(name + "_TexelSize"))
                {
                    sb.AppendLine($"{indent}float4 {name}_TexelSize; // the size of {name}, which is also sampled");
                }
            }

            foreach ((string name, string type) in resources.OrderBy(r => r.Key, StringComparer.Ordinal))
            {
                if (declared.Add(name))
                {
                    sb.AppendLine($"{indent}{type} {name}; // no name in the shader's reflection data");
                }
            }

            foreach ((int register, (int size, bool _)) in constantBuffers.OrderBy(c => c.Key))
            {
                if (declared.Add($"cb{register}"))
                {
                    sb.AppendLine($"{indent}float4 cb{register}[{size}]; // constant buffer {register}, no names in the shader's reflection data");
                }
            }

            return sb.ToString();
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

            // A sampled texture is a sampler2D, which has no GetDimensions; its _TexelSize vector holds (1/w, 1/h, w, h).
            if (usilResource.metadataNameAssigned && !usilResource.bufferResource && usilResource.metadataName != null &&
                SampledTextureNames().Contains(usilResource.metadataName))
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
            string comment = CommentString(inst);
            AppendLine($"{comment}while (true) {{");
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
                $"{comment}for ({numberTypeName} {iterName} = {iterRegOp}; " +
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
