
static class Program{
    public static void Main(){
        var tokenizer = new Tokenizer("var x = 2.5; return x + 6 * x;");
        var tokens = tokenizer.Tokenize();
        var functionCompiler = new FunctionCompiler("float");
        var il = new IL();
        il.Add(functionCompiler.Compile(tokens));
        il.Emit();
    }
} 