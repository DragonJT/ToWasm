
static class StatementCompiler{
    public static ILInstruction[] Compile(Token[] tokens, Compiler compiler){
        if(tokens[0].type == TokenType.Return){
            var expressionInfo = ExpressionCompiler.Compile(tokens[1..], compiler);
            if(TypeCompiler.ValidFromToType(expressionInfo.type, compiler.returnType)){
                return [
                    ..TypeCompiler.AddConvertInstructions(expressionInfo.instructions, expressionInfo.type, compiler.returnType),
                    new ILInstruction(Opcode.ret)
                ];
            }
            else{
                throw new Exception("Return type mismatch: "+expressionInfo.type+" - "+compiler.returnType);
            }
        }
        else if(tokens[0].type == TokenType.Var){
            var varname = tokens[1].value;
            var expressionInfo = ExpressionCompiler.Compile(tokens[3..], compiler);
            compiler.types.Add(varname, expressionInfo.type);
            return [
                ..expressionInfo.instructions,
                new ILInstruction(Opcode.set_local, varname),
            ];
        }
        throw new Exception("Unexpected statement");
    }
}