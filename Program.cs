
static class Program{
    public static void Main(){
        var code = @"
import void PrintFloat(float value){
    console.log(value);
}

float Test(float a, float b){
    return a + b;
}

float Run(){ 
    var x = Test(2, 4); 
    PrintFloat(Test(3, 4));
    return x + 6 * x; 
}";
        var tokenizer = new Tokenizer(code);
        var tokens = tokenizer.Tokenize(0);
        var compiler = new Compiler();
        var il = compiler.Compile(tokens);
        il.Emit();
        il.Print();
    }
} 