
static class Program{
    public static void Main(){
        var tokenizer = new Tokenizer("float Run(){ var x = 5; return x + 6 * x; }");
        var tokens = tokenizer.Tokenize();
        var compiler = new Compiler();
        var il = compiler.Compile(tokens);
        il.Emit();
    }
} 