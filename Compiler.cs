
interface IFunctionCompiler{
    string Name{get;}
    string ReturnType {get;}
    Variable[] Parameters {get;}
}

class ImportFunctionCompiler:IFunctionCompiler{
    public string Name {get;}
    public string ReturnType { get;}
    public Variable[] Parameters {get;}
    public readonly string code;

    public ImportFunctionCompiler(Token[] tokens){
        ReturnType = tokens[1].GetVarnameValue();
        Name = tokens[2].GetVarnameValue();
        var splitParametersTokens = Compiler.SplitByComma([..tokens[3].GetParenthesesTokens()]).ToArray();
        Parameters = splitParametersTokens.Select(t=>new Variable(t[0].value, t[1].value)).ToArray();
        code = tokens[4].GetJavascript();
    }

    public ILImportFunction Compile(){
        var ilParameters = Parameters.Select(p=>new ILVariable(TypeCompiler.StringToValtype(p.type), p.name)).ToArray();
        return new ILImportFunction(Name, TypeCompiler.StringToValtype(ReturnType), ilParameters, code);
    }
}

class Compiler{
    readonly List<ImportFunctionCompiler> importFunctions = [];
    readonly List<FunctionCompiler> functions = [];

    static bool MatchingParameterTypes(IFunctionCompiler func, string[] parameterTypes){
        var parameters = func.Parameters;
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


    public IFunctionCompiler FindFunction(string name, string[] parameterTypes){
        IFunctionCompiler[] funcs = [..importFunctions, ..functions];
        foreach(var f in funcs){
            if(f.Name == name && MatchingParameterTypes(f, parameterTypes)){
                return f;
            }
        }
        throw new Exception("Cant find function with name: "+name);
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
        foreach(var f in importFunctions){
            il.Add(f.Compile());
        }
        foreach(var f in functions){
            il.Add(f.Compile());
        }
        return il;
    }
}