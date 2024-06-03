
static class Program{
    public static void Main(){
        var code = @"
float Test(){
    return 7;
}

float Run(){ 
    var x = Test(); 
    return x + 6 * x; 
}";
        var tokenizer = new Tokenizer(code);
        var tokens = tokenizer.Tokenize();
        var compiler = new Compiler();
        var il = compiler.Compile(tokens);
        il.Emit();
    }
} 