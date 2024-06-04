
class ExpressionInfo(ILInstruction[] instructions, string type){
    public ILInstruction[] instructions = instructions;
    public string type = type;
}

class Variable(string type, string name){
    public string type = type;
    public string name = name;
}

class FunctionCompiler{
    readonly Compiler compiler;
    public readonly string returnType;
    public readonly string name;
    readonly Variable[] parameters;
    readonly List<Variable> locals = [];
    readonly List<Token> bodyTokens;

    public bool MatchingParameterTypes(string[] parameterTypes){
        if(parameters.Length != parameterTypes.Length){
            return false;
        }
        for(var i=0;i<parameters.Length;i++){
            if(!TypeCompiler.ValidFromToType(parameterTypes[i], parameters[i].type)){
                return false;
            }
        }
        return true;
    }

    static Token[][] SplitByComma(Token[] tokens){
        List<Token[]> result = [];
        List<Token> splitResult = [];
        foreach(var token in tokens){
            if(token.type == TokenType.Punctuation && token.value == ","){
                result.Add([..splitResult]);
                splitResult.Clear();
            }
            else{
                splitResult.Add(token);
            }
        }
        if(splitResult.Count > 0){
            result.Add([..splitResult]);
        }
        return [..result];
    }

    Variable GetVariable(string name){
        foreach(var p in parameters){
            if(p.name == name){
                return p;
            }
        }
        foreach(var l in locals){
            if(l.name == name){
                return l;
            }
        }
        throw new Exception("Can't find local or parameter with name: "+name);
    }

    public FunctionCompiler(Compiler compiler, Token[] tokens){
        this.compiler = compiler;
        returnType = tokens[0].GetVarnameValue();
        name = tokens[1].GetVarnameValue();
        var parametersTokens = tokens[2].GetParenthesesTokens();
        var splitParametersTokens = SplitByComma([..parametersTokens]);
        parameters = splitParametersTokens.Select(p=>new Variable(p[0].value, p[1].value)).ToArray();
        bodyTokens = tokens[3].GetCurlyTokens();
    }

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
                return new ExpressionInfo([new ILInstruction(Opcode.get_local, tokens[0].value)], GetVariable(tokens[0].value).type);
            }
            else{
                throw new Exception("Unexpected tokentype: "+tokens[0].type);
            }
        }
        else if(tokens.Length == 2){
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parentheses){
                var argsTokens = tokens[1].GetParenthesesTokens();
                var splitArgsTokens = SplitByComma([..argsTokens]);
                var argExpressions = splitArgsTokens.Select(CompileExpression).ToArray();
                var function = compiler.FindFunction(tokens[0].value, argExpressions.Select(e=>e.type).ToArray());
                var instructions = new List<ILInstruction>();
                for(var i=0;i<function.parameters.Length;i++){
                    instructions.AddRange(TypeCompiler.AddConvertInstructions(argExpressions[i].instructions, argExpressions[i].type, function.parameters[i].type));
                }
                return new ExpressionInfo([..instructions, new ILInstruction(Opcode.call, function.name)], function.returnType);
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
            locals.Add(new Variable(expressionInfo.type, varname));
            return [
                ..expressionInfo.instructions,
                new ILInstruction(Opcode.set_local, varname),
            ];
        }
        throw new Exception("Unexpected statement");
    }

    public ILFunction Compile(){
        List<Token> statementTokens = [];
        List<ILInstruction> instructions = [];
        foreach(var t in bodyTokens){
            if(t.type == TokenType.Punctuation && t.value == ";"){
                instructions.AddRange(CompileStatement([..statementTokens]));
                statementTokens.Clear();
            }
            else{
                statementTokens.Add(t);
            }
        }
        var ilLocals = locals.Select(l=>new ILVariable(TypeCompiler.StringToValtype(l.type), l.name)).ToArray();
        var ilParameters = parameters.Select(p=>new ILVariable(TypeCompiler.StringToValtype(p.type), p.name)).ToArray();
        return new ILFunction(true, name, TypeCompiler.StringToValtype(returnType), ilParameters, ilLocals, [..instructions]);
    }
}