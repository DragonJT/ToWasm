
class FuncSignature(Type[] @params, Type output){
    public readonly Type[] @params = @params;
    public readonly Type output = output;

    public ExpressionInfo? IsMatch(ExpressionInfo[] args){
        List<ILInstruction> instructions = [];
        if(args.Length != @params.Length){
            return null;
        }
        for(var i=0;i<args.Length;i++){
            if(@params[i] != args[i].type){
                return null;
            }
            else{
                instructions.AddRange(args[i].instructions);
            }
        }
        return new ExpressionInfo([..instructions], output);
    }

    public ExpressionInfo? CanMatch(ExpressionInfo[] args){
        List<ILInstruction> instructions = [];
        if(args.Length != @params.Length){
            return null;
        }
        for(var i=0;i<args.Length;i++){
            if(@params[i] == args[i].type){
                instructions.AddRange(args[i].instructions);
            }
            else{
                var convert = TypeConversions.Convert(args[i].type, @params[i]);
                if(convert == null){
                    return null;
                }
                else{
                    instructions.AddRange(args[i].instructions);
                    instructions.Add(new ILInstruction(convert.opcode));
                }
            }
        }
        return new ExpressionInfo([..instructions], output); 
    }
}

class FuncCall(FuncSignature funcSignature, ILInstruction instruction){
    public readonly FuncSignature funcSignature = funcSignature;
    public readonly ILInstruction instruction = instruction;
}

static class FuncCalls{
    public static ExpressionInfo CallFunction(this FuncCall[] calls, ExpressionInfo[] args){
        foreach(var c in calls){
            var expressionInfo = c.funcSignature.IsMatch(args);
            if(expressionInfo!=null){
                expressionInfo.instructions = [..expressionInfo.instructions, c.instruction];
                return expressionInfo;
            }
        }
        foreach(var c in calls){
            var expressionInfo = c.funcSignature.CanMatch(args);
            if(expressionInfo!=null){
                expressionInfo.instructions = [..expressionInfo.instructions, c.instruction];
                return expressionInfo;
            }
        }

        Console.WriteLine("=============");
        foreach(var a in args){
            Console.Write(a.type.name);
            Console.Write(",");
        }
        Console.WriteLine();
        foreach(var c in calls){
            foreach(var a in c.funcSignature.@params){
                Console.Write(a.name);
                Console.Write(",");
            }
            Console.WriteLine();
        }
        throw new Exception("Cant match function signature");
    }
}

class BinaryOp(string symbol, FuncCall[] calls){
    public readonly string symbol = symbol;
    public readonly FuncCall[] calls = calls;
}

class SplitOp(BinaryOp binaryOp, int index){
    public readonly BinaryOp binaryOp = binaryOp;
    public readonly int index = index;
}

static class Operators{
    public readonly static BinaryOp[][] operators;

    static FuncCall BinaryOpSignature(Type left, Type right, Type output, Opcode opcode){
        return new FuncCall(new FuncSignature([left, right], output), new ILInstruction(opcode));
    }

    static Operators(){
        operators = [[
            new BinaryOp("<", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Bool, Opcode.f32_lt),
                BinaryOpSignature(Type.Int, Type.Int, Type.Bool, Opcode.i32_lt_s)]),
            new BinaryOp(">", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Bool, Opcode.f32_gt),
                BinaryOpSignature(Type.Int, Type.Int, Type.Bool, Opcode.i32_gt_s)]),
            ],[
            new BinaryOp("+", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Float, Opcode.f32_add),
                BinaryOpSignature(Type.Int, Type.Int, Type.Int, Opcode.i32_add)]),
            new BinaryOp("-", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Float, Opcode.f32_sub),
                BinaryOpSignature(Type.Int, Type.Int, Type.Int, Opcode.i32_sub)]),
            ],[
            new BinaryOp("*", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Float, Opcode.f32_mul),
                BinaryOpSignature(Type.Int, Type.Int, Type.Int, Opcode.i32_mul)]),
            new BinaryOp("/", [
                BinaryOpSignature(Type.Float, Type.Float, Type.Float, Opcode.f32_div),
                BinaryOpSignature(Type.Int, Type.Int, Type.Int, Opcode.i32_div_s)]),
            ]];
    }
}

class ExpressionInfo(ILInstruction[] instructions, Type type){
    public ILInstruction[] instructions = instructions;
    public Type type = type;
}

class FunctionCompiler: IFunctionCompiler{
    readonly Compiler compiler;
    public Type ReturnType {get;}
    public string Name {get;}
    public int ID{get;set;} = -1;
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

    static SplitOp? FindSplit(Token[] tokens, BinaryOp[] ops){
        for(var i=tokens.Length-1;i>=0;i--){
            if(tokens[i].type == TokenType.Punctuation){
                foreach(var o in ops){
                    if(o.symbol == tokens[i].value){
                        return new SplitOp(o, i);
                    }
                }
            }
        }
        return null;
    }

    ExpressionInfo GetInvocationExpression(Token[] tokens){
        var argsTokens = tokens[1].GetParenthesesTokens();
        var splitArgsTokens = Compiler.SplitByComma([..argsTokens]);
        var argExpressions = splitArgsTokens.Select(CompileExpression).ToArray();
        var funcCalls = compiler.FindFuncCalls(tokens[0].value);
        return funcCalls.CallFunction(argExpressions);
    }

    ExpressionInfo CompileExpression(Token[] tokens){
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

        foreach(var ops in Operators.operators){
            var split = FindSplit(tokens, ops);
            if(split!=null){
                var left = CompileExpression(tokens[0..split.index]);
                var right = CompileExpression(tokens[(split.index+1)..]);
                Console.WriteLine(split.binaryOp.symbol);
                return split.binaryOp.calls.CallFunction([left, right]);
            }
        }
        throw new Exception("Unexpected tokens");
    }

    ILInstruction[] CompileStatement(Token[] tokens){
        if(tokens[0].type == TokenType.Return){
            var expressionInfo = CompileExpression(tokens[1..]);
            return [..TypeConversions.Convert(expressionInfo, ReturnType), new ILInstruction(Opcode.ret)];
        }
        else if(tokens[0].type == TokenType.Var){
            var varname = tokens[1].value;
            var expressionInfo = CompileExpression(tokens[3..]);
            Console.WriteLine(expressionInfo.type.name+" - "+varname);
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
            return [..TypeConversions.Convert(expressionInfo, variable.type), new ILInstruction(Opcode.set_local, variable.name)];
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
        return new ILFunction(true, Name, ID, ReturnType.valtype, Parameters, [..locals], [..instructions]);
    }
}