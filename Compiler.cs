class Compiler{
    readonly List<FunctionCompiler> functions = [];

    public FunctionCompiler FindFunction(string name){
        return functions.First(f=>f.name == name);
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