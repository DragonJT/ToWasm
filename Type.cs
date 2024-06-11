class TypeConversion(Type oldType, Type newType, Opcode opcode){
    public readonly Type oldType = oldType;
    public readonly Type newType = newType;
    public readonly Opcode opcode = opcode;
}

static class TypeConversions{
    public static readonly TypeConversion[] typeConversions;

    static TypeConversions(){
        typeConversions = [new TypeConversion(Type.Int, Type.Float, Opcode.f32_convert_i32_s)];
    }

    public static TypeConversion? Convert(Type oldType, Type newType){
        foreach(var c in typeConversions){
            if(c.oldType == oldType && c.newType == newType){
                return c;
            }
        }
        return null;
    }

    public static ILInstruction[] Convert(ExpressionInfo expressionInfo, Type finalType){
        if(expressionInfo.type == finalType){
            return expressionInfo.instructions;
        }
        var convert = Convert(expressionInfo.type, finalType);
        if(convert == null){
            throw new Exception("Type mismatch: "+expressionInfo.type.name+" - "+finalType.name);
        }
        else{
            return [..expressionInfo.instructions, new ILInstruction(convert.opcode)];
        }
    }
}

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
}