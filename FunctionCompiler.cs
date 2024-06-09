

class ExpressionInfo(ILInstruction[] instructions, Type type){
    public ILInstruction[] instructions = instructions;
    public Type type = type;
}

class FunctionCompiler: IFunctionCompiler{
    readonly Compiler compiler;
    public Type ReturnType {get;}
    public string Name {get;}
    public Variable[] Parameters {get;}
    readonly List<Variable> locals = [];
    readonly List<Token> bodyTokens;

    Variable GetVariable(string name){
        foreach(var p in Parameters){
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
        ReturnType = Type.Parse(tokens[0].GetVarnameValue());
        Name = tokens[1].GetVarnameValue();
        var parametersTokens = tokens[2].GetParenthesesTokens();
        var splitParametersTokens = Compiler.SplitByComma([..parametersTokens]);
        Parameters = splitParametersTokens.Select(p=>new Variable(Type.Parse(p[0].value), p[1].value)).ToArray();
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

    static Opcode GetOpcode(string op, Type type){
        if(type == Type.Float){
            return op switch
            {
                "+" => Opcode.f32_add,
                "-" => Opcode.f32_sub,
                "*" => Opcode.f32_mul,
                "/" => Opcode.f32_div,
                _ => throw new Exception("Unexpected op:" + op),
            };
        }
        else if(type == Type.Int){
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

    ExpressionInfo GetInvocationExpression(Token[] tokens){
        var argsTokens = tokens[1].GetParenthesesTokens();
        var splitArgsTokens = Compiler.SplitByComma([..argsTokens]);
        var argExpressions = splitArgsTokens.Select(CompileExpression).ToArray();
        var function = compiler.FindFunction(tokens[0].value, argExpressions.Select(e=>e.type).ToArray());
        var instructions = new List<ILInstruction>();
        for(var i=0;i<function.Parameters.Length;i++){
            instructions.AddRange(Type.AddConvertInstructions(argExpressions[i].instructions, argExpressions[i].type, function.Parameters[i].type));
        }
        return new ExpressionInfo([..instructions, new ILInstruction(Opcode.call, function.Name)], function.ReturnType);
    }

    ExpressionInfo CompileExpression(Token[] tokens){
        string[][] operators = [["+", "-"], ["*", "/"]];

        if(tokens.Length == 1){
            if(tokens[0].type == TokenType.Number){
                if(tokens[0].value.Contains('.')){
                    return new ExpressionInfo([new ILInstruction(Opcode.f32_const, float.Parse(tokens[0].value))], Type.Float);
                }
                return new ExpressionInfo([new ILInstruction(Opcode.i32_const, int.Parse(tokens[0].value))], Type.Int);
            }
            else if(tokens[0].type == TokenType.Varname){
                return new ExpressionInfo([new ILInstruction(Opcode.get_local, tokens[0].value)], GetVariable(tokens[0].value).type);
            }
            else if(tokens[0].type == TokenType.True){
                return new ExpressionInfo([new ILInstruction(Opcode.i32_const, 1)],Type.Bool);
            }
            else if(tokens[0].type == TokenType.False){
                return new ExpressionInfo([new ILInstruction(Opcode.i32_const, 0)], Type.Bool);
            }
            else{
                throw new Exception("Unexpected tokentype: "+tokens[0].type);
            }
        }
        else if(tokens.Length == 2){
            if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parentheses){
                return GetInvocationExpression(tokens);
            }
        }

        foreach(var ops in operators){
            var split = FindSplit(tokens, ops);
            if(split>=0){
                var left = CompileExpression(tokens[0..split]);
                var right = CompileExpression(tokens[(split+1)..]);
                var type = Type.ConvertType(left.type, right.type);
                left.instructions = Type.AddConvertInstructions(left.instructions, left.type, type);
                right.instructions = Type.AddConvertInstructions(right.instructions, right.type, type);

                ILInstruction[] instructions = [..left.instructions, ..right.instructions, new ILInstruction(GetOpcode(tokens[split].value, type))];
                return new ExpressionInfo(instructions, type);
            }
        }
        throw new Exception("Unexpected tokens");
    }

    ILInstruction[] CompileStatement(Token[] tokens){
        if(tokens[0].type == TokenType.Return){
            var expressionInfo = CompileExpression(tokens[1..]);
            if(expressionInfo.type.ValidConversionType(ReturnType)){
                return [
                    ..Type.AddConvertInstructions(expressionInfo.instructions, expressionInfo.type, ReturnType!),
                    new ILInstruction(Opcode.ret)
                ];
            }
            else{
                throw new Exception("Return type mismatch: "+expressionInfo.type+" - "+ReturnType!);
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
        else if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Parentheses){
            return GetInvocationExpression(tokens).instructions;
        }
        else if(tokens[0].type == TokenType.Varname && tokens[1].type == TokenType.Punctuation && tokens[1].value == "="){
            var expressionInfo = CompileExpression(tokens[2..]);
            var variable = GetVariable(tokens[0].value);
            if(expressionInfo.type.ValidConversionType(variable.type)){
                return [
                    ..Type.AddConvertInstructions(expressionInfo.instructions, expressionInfo.type, variable.type),
                    new ILInstruction(Opcode.set_local, variable.name)
                ];
            }
            else{
                throw new Exception("Type mismatch: "+expressionInfo.type+" - "+variable.type);
            }
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
        return new ILFunction(true, Name, ReturnType.valtype, Parameters, [..locals], [..instructions]);
    }
}