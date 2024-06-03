class Compiler{
    readonly List<FunctionCompiler> functions = [];

    public IL Compile(List<Token> tokens){
        var il = new IL();
        List<Token> functionTokens = [];
        foreach(var t in tokens){
            if(t.type == TokenType.Curly){
                functionTokens.Add(t);
                functions.Add(new FunctionCompiler([..functionTokens]));
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