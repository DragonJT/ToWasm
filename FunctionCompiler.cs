

static class FunctionCompiler{
    public static ILFunction Compile(List<Token> tokens, Compiler compiler){
        List<Token> statementTokens = [];
        List<ILInstruction> instructions = [];
        foreach(var t in tokens){
            if(t.type == TokenType.Punctuation && t.value == ";"){
                instructions.AddRange(StatementCompiler.Compile([..statementTokens], compiler));
                statementTokens.Clear();
            }
            else{
                statementTokens.Add(t);
            }
        }
        var locals = compiler.types.Select(t=>new ILVariable(TypeCompiler.StringToValtype(t.Value), t.Key)).ToArray();
        return new ILFunction(true, "Run", Valtype.F32, [], locals, [..instructions]);
    }
}