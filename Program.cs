
class Compiler{

}

static class Program{
    public static void Main(){
        var tokenizer = new Tokenizer("5 + 6 * 4.4");
        var tokens = tokenizer.Tokenize();
        var compiler = new Compiler();
        var expressionInfo = ExpressionParser.Parse([..tokens], compiler);
        var il = new IL();

        il.Add(new ILFunction(true, "Run", Valtype.F32, [], [], expressionInfo.instructions));
        il.Emit();
    }
} 