
class ExpressionInfo(ILInstruction[] instructions, string type){
    public ILInstruction[] instructions = instructions;
    public string type = type;
}

static class ExpressionParser{

    static int FindSplit(Token[] tokens, string[] ops){
        for(var i=tokens.Length-1;i>=0;i--){
            if(tokens[i].type == TokenType.Punctuation && ops.Contains(tokens[i].value)){
                return i;
            }
        }
        return -1;
    }

    static Opcode GetOpcode(string op, string type){
        if(type == "float"){
            return op switch
            {
                "+" => Opcode.f32_add,
                "-" => Opcode.f32_sub,
                "*" => Opcode.f32_mul,
                "/" => Opcode.f32_div,
                _ => throw new Exception("Unexpected op:" + op),
            };
        }
        else if(type == "int"){
            return op switch
            {
                "+" => Opcode.i32_add,
                "-" => Opcode.i32_sub,
                "*" => Opcode.i32_mul,
                "/" => Opcode.i32_div_s,
                _ => throw new Exception("Unexpected op:" + op),
            };
        }
        else{
            throw new Exception("Unexpected type");
        }
    }

    static string CalcType(string left, string right){
        if(left == right){
            return left;
        }
        else{
            if(left == "int" && right == "float" || left == "float" && right == "int"){
                return "float";
            }
            throw new Exception("Type mismatch: "+left+" - "+right);
        }
    }

    static ILInstruction[] AddConvertInstructions(ILInstruction[] instructions, string oldType, string newType){
        if(oldType == "int" && newType == "float"){
            return [..instructions, new ILInstruction(Opcode.f32_convert_i32_s)];
        }
        throw new Exception("Unexpected old and new types");
    }

    public static ExpressionInfo Parse(Token[] tokens, Compiler compiler){
        string[][] operators = [["+", "-"], ["*", "/"]];

        if(tokens.Length == 1){
            if(tokens[0].type == TokenType.Number){
                if(tokens[0].value.Contains('.')){
                    return new ExpressionInfo([new ILInstruction(Opcode.f32_const, float.Parse(tokens[0].value))], "float");
                }
                return new ExpressionInfo([new ILInstruction(Opcode.i32_const, int.Parse(tokens[0].value))], "int");
            }
            else{
                throw new Exception("Unexpected tokentype: "+tokens[0].type);
            }
        }

        foreach(var ops in operators){
            var split = FindSplit(tokens, ops);
            if(split>=0){
                var left = Parse(tokens[0..split], compiler);
                var right = Parse(tokens[(split+1)..], compiler);
                var type = CalcType(left.type, right.type);
                if(left.type != type){
                    left.instructions = AddConvertInstructions(left.instructions, left.type, type);
                }
                if(right.type != type){
                    right.instructions = AddConvertInstructions(right.instructions, right.type, type);
                }
                ILInstruction[] instructions = [..left.instructions, ..right.instructions, new ILInstruction(GetOpcode(tokens[split].value, type))];
                return new ExpressionInfo(instructions, type);
            }
        }
        throw new Exception("Unexpected tokens");
    }
}