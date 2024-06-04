class Compiler{
    readonly List<FunctionCompiler> functions = [];

    public FunctionCompiler FindFunction(string name, string[] parameterTypes){
        foreach(var f in functions){
            if(f.name == name && f.MatchingParameterTypes(parameterTypes)){
                return f;
            }
        }
        throw new Exception("Cant find function with name: "+name);
    }

    public IL Compile(List<Token> tokens){
        var il = new IL();
        List<Token> functionTokens = [];
        foreach(var t in tokens){
            if(t.type == TokenType.Curly){
                functionTokens.Add(t);
                functions.Add(new FunctionCompiler(this, [..functionTokens]));
                functionTokens.Clear();
            }
            else{
                functionTokens.Add(t);
            }
        }

        foreach(var f in functions){
            il.Add(f.Compile());
        }
        return il;
    }
}