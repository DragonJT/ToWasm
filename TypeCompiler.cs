
static class TypeCompiler{
    public static Valtype StringToValtype(string type){
        return type switch{
            "bool" => Valtype.I32,
            "float" => Valtype.F32,
            "int" => Valtype.I32,
            "void" => Valtype.Void,
            _ => throw new Exception("Unexpected type"),
        };
    }

    public static bool ValidFromToType(string from, string to){
        if(from == to){
            return true;
        }
        if(from == "int" && to == "float"){
            return true;
        }
        return false;
    }

    public static string CalcType(string left, string right){
        if(ValidFromToType(left, right)){
            return right;
        }
        if(ValidFromToType(right, left)){
            return left;
        }
        throw new Exception("Type mismatch: "+left+" - "+right);
    }

    public static ILInstruction[] AddConvertInstructions(ILInstruction[] instructions, string oldType, string newType){
        if(oldType == newType){
            return instructions;
        }
        if(oldType == "int" && newType == "float"){
            return [..instructions, new ILInstruction(Opcode.f32_convert_i32_s)];
        }
        throw new Exception("Unexpected old and new types");
    }
}