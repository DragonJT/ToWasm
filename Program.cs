
class Compiler(string returnType){
    public string returnType = returnType;
    public Dictionary<string, string> types = [];
}

static class Program{
    public static void Main(){
        var tokenizer = new Tokenizer("var x = 10; return x + 6 * 1.5;");
        var tokens = tokenizer.Tokenize();
        var compiler = new Compiler("float");
        var il = new IL();
        il.Add(FunctionCompiler.Compile(tokens, compiler));
        il.Emit();
    }
} 