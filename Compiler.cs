
interface IFunctionCompiler{
    string Name{get;}
    Type ReturnType {get;}
    Variable[] Parameters {get;}
    int ID{get;}
}

class ImportFunctionCompiler:IFunctionCompiler{
    public string Name {get;}
    public Type ReturnType { get;}
    public Variable[] Parameters {get;}
    public readonly string code;
    public int ID{get;set;} = -1;

    public ImportFunctionCompiler(Token[] tokens){
        ReturnType =Type.Parse(tokens[1].GetVarnameValue());
        Name = tokens[2].GetVarnameValue();
        var splitParametersTokens = Compiler.SplitByComma([..tokens[3].GetParenthesesTokens()]).ToArray();
        Parameters = splitParametersTokens.Select(t=>new Variable(Type.Parse(t[0].value), t[1].value)).ToArray();
        code = tokens[4].GetJavascript();
    }

    public ILImportFunction Compile(){
        return new ILImportFunction(Name, ID, ReturnType, Parameters, code);
    }
}

class Compiler{
    readonly List<ImportFunctionCompiler> importFunctions = [];
    readonly List<FunctionCompiler> functions = [];

    public static Token[][] SplitByComma(Token[] tokens){
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


    public FuncCall[] FindFuncCalls(string name){
        List<FuncCall> calls = [];
        foreach(var f in importFunctions){
            if(f.Name == name){
                calls.Add(new FuncCall(new FuncSignature(f.Parameters.Select(p=>p.type).ToArray(), f.ReturnType), new ILInstruction(Opcode.call, (uint)f.ID)));
            }
        }
        foreach(var f in functions){
            if(f.Name == name){
                calls.Add(new FuncCall(new FuncSignature(f.Parameters.Select(p=>p.type).ToArray(), f.ReturnType), new ILInstruction(Opcode.call, (uint)f.ID)));
            }
        }
        if(calls.Count == 0){
            throw new Exception("Cant find function with name: "+name);
        }
        return [..calls];
    }

    public IL Compile(List<Token> tokens){
        var il = new IL();
        List<Token> sectionTokens = [];
        foreach(var t in tokens){
            if(t.type == TokenType.Curly){
                sectionTokens.Add(t);
                var token = sectionTokens[0];
                if(token.type == TokenType.Import){
                    importFunctions.Add(new ImportFunctionCompiler([..sectionTokens]));
                }   
                else{
                    functions.Add(new FunctionCompiler(this, [..sectionTokens]));
                }
                sectionTokens.Clear();
            }
            else{
                sectionTokens.Add(t);
            }
        }
        var id = 0;
        foreach(var f in importFunctions){
            f.ID = id;
            id++;
        }
        foreach(var f in functions){
            f.ID = id;
            id++;
        }
        foreach(var f in importFunctions){
            il.Add(f.Compile());
        }
        foreach(var f in functions){
            il.Add(f.Compile());
        }
        return il;
    }
}