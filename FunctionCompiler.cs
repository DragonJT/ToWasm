
class ExpressionInfo(ILInstruction[] instructions, string type){
    public ILInstruction[] instructions = instructions;
    public string type = type;
}

class FunctionCompiler(string returnType){
    public string returnType = returnType;
    public Dictionary<string, string> locals = [];

    static int FindSplit(Token[] tokens, string[] ops){
        for(var i=tokens.Length-1;i>=0;i--){
            if(tokens[i].type == TokenType.Punctuation && ops.Contains(tokens[i].value)){
                return i;
            }
        }
        return -1;
    }

    static Opcode GetOpcode(string op, string type){
        if(type == "float"){
            return op switch
            {
                "+" => Opcode.f32_add,
                "-" => Opcode.f32_sub,
                "*" => Opcode.f32_mul,
                "/" => Opcode.f32_div,
                _ => throw new Exception("Unexpected op:" + op),
            };
        }
        else if(type == "int"){
            return op switch
            {
                "+" => Opcode.i32_add,
                "-" => Opcode.i32_sub,
                "*" => Opcode.i32_mul,
                "/" => Opcode.i32_div_s,
                _ => throw new Exception("Unexpected op:" + op),
            };
        }
        else{
            throw new Exception("Unexpected type");
        }
    }

    ExpressionInfo CompileExpression(Token[] tokens){
        string[][] operators = [["+", "-"], ["*", "/"]];

        if(tokens.Length == 1){
            if(tokens[0].type == TokenType.Number){
                if(tokens[0].value.Contains('.')){
                    return new ExpressionInfo([new ILInstruction(Opcode.f32_const, float.Parse(tokens[0].value))], "float");
                }
                return new ExpressionInfo([new ILInstruction(Opcode.i32_const, int.Parse(tokens[0].value))], "int");
            }
            else if(tokens[0].type == TokenType.Varname){
                return new ExpressionInfo([new ILInstruction(Opcode.get_local, tokens[0].value)], locals[tokens[0].value]);
            }
            else{
                throw new Exception("Unexpected tokentype: "+tokens[0].type);
            }
        }

        foreach(var ops in operators){
            var split = FindSplit(tokens, ops);
            if(split>=0){
                var left = CompileExpression(tokens[0..split]);
                var right = CompileExpression(tokens[(split+1)..]);
                var type = TypeCompiler.CalcType(left.type, right.type);
                left.instructions = TypeCompiler.AddConvertInstructions(left.instructions, left.type, type);
                right.instructions = TypeCompiler.AddConvertInstructions(right.instructions, right.type, type);

                ILInstruction[] instructions = [..left.instructions, ..right.instructions, new ILInstruction(GetOpcode(tokens[split].value, type))];
                return new ExpressionInfo(instructions, type);
            }
        }
        throw new Exception("Unexpected tokens");
    }

    ILInstruction[] CompileStatement(Token[] tokens){
        if(tokens[0].type == TokenType.Return){
            var expressionInfo = CompileExpression(tokens[1..]);
            if(TypeCompiler.ValidFromToType(expressionInfo.type, returnType!)){
                return [
                    ..TypeCompiler.AddConvertInstructions(expressionInfo.instructions, expressionInfo.type, returnType!),
                    new ILInstruction(Opcode.ret)
                ];
            }
            else{
                throw new Exception("Return type mismatch: "+expressionInfo.type+" - "+returnType!);
            }
        }
        else if(tokens[0].type == TokenType.Var){
            var varname = tokens[1].value;
            var expressionInfo = CompileExpression(tokens[3..]);
            locals.Add(varname, expressionInfo.type);
            return [
                ..expressionInfo.instructions,
                new ILInstruction(Opcode.set_local, varname),
            ];
        }
        throw new Exception("Unexpected statement");
    }

    public ILFunction Compile(List<Token> tokens){
        List<Token> statementTokens = [];
        List<ILInstruction> instructions = [];
        foreach(var t in tokens){
            if(t.type == TokenType.Punctuation && t.value == ";"){
                instructions.AddRange(CompileStatement([..statementTokens]));
                statementTokens.Clear();
            }
            else{
                statementTokens.Add(t);
            }
        }
        var l = locals.Select(t=>new ILVariable(TypeCompiler.StringToValtype(t.Value), t.Key)).ToArray();
        return new ILFunction(true, "Run", Valtype.F32, [], l, [..instructions]);
    }
}