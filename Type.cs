

class Type(string name, Valtype valtype){
    public readonly string name = name;
    public readonly Valtype valtype = valtype;

    public static readonly Type Float = new("float", Valtype.F32);
    public static readonly Type Int = new("int", Valtype.I32);
    public static readonly Type Bool = new("bool", Valtype.I32);
    public static readonly Type Void = new("void", Valtype.Void);

    public static Type Parse(string name){
        return name switch
        {
            "float" => Float,
            "int" => Int,
            "bool" => Bool,
            "void" => Void,
            _ => throw new Exception("Unexpected type"),
        };
    }

    public bool ValidConversionType(Type conversion){
        if(this == conversion){
            return true;
        }
        if(this == Int && conversion == Float){
            return true;
        }
        return false;
    }

    public static Type ConvertType(Type left, Type right){
        if(left.ValidConversionType(right)){
            return right;
        }
        if(right.ValidConversionType(left)){
            return left;
        }
        throw new Exception("Type mismatch: "+left+" - "+right);
    }

    public static ILInstruction[] AddConvertInstructions(ILInstruction[] instructions, Type oldType, Type newType){
        if(oldType == newType){
            return instructions;
        }
        if(oldType == Int && newType == Float){
            return [..instructions, new ILInstruction(Opcode.f32_convert_i32_s)];
        }
        throw new Exception("Unexpected old and new types");
    }
}