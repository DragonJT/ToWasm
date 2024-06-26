
class ILInstruction(Opcode opcode, object? value = null){
    public Opcode opcode = opcode;
    public object? value = value;
}

class Variable(Type type, string name){
    public Type type = type;
    public string name = name;
    public int id = -1;
}

class ILImportFunction(string name, int id, Type returnType, Variable[] parameters, string code){
    public string name = name;
    public int id = id;
    public Type returnType = returnType;
    public Variable[] parameters = parameters;
    public string code = code;

    public string EmitParameters(){
        string result = "(";
        for(var i=0;i<parameters.Length;i++){
            result+=parameters[i].name;
            if(i<parameters.Length-1){
                result+=", ";
            }
        }
        result+=")";
        return result;
    }

    public string EmitArgs(){
        string result = "(";
        for(var i=0;i<parameters.Length;i++){
            if(parameters[i].type == Type.Bool){
                result+="ConvertIntToBool("+parameters[i].name+")";
            }
            else{
                result+=parameters[i].name;
            }
            if(i<parameters.Length-1){
                result+=", ";
            }
        }
        result+=")";
        return result;
    }
}

class ILFunction(bool export, string name, int id, Valtype returnType, Variable[] parameters, Variable[] locals, ILInstruction[] instructions){
    public bool export = export;
    public string name = name;
    public Valtype returnType = returnType;
    public Variable[] parameters = parameters;
    public Variable[] locals = locals;
    public ILInstruction[] instructions = instructions;
    public int id = id;

    public uint FindLocalID(string name){
        foreach(var p in parameters){
            if(p.name == name){
                return (uint)p.id;
            }
        }
        foreach(var l in locals){
            if(l.name == name){
                return (uint)l.id;
            }
        }
        throw new Exception("Cant find local or parameter with name: "+name);
    }
}

class IL{
    readonly List<ILImportFunction> importFunctions = [];
    readonly List<ILFunction> functions = [];

    public void Add(ILFunction function){
        functions.Add(function);
    }
    
    public void Add(ILImportFunction importFunction){
        importFunctions.Add(importFunction);
    }

    public void Print(){
        foreach(var f in functions){
            Console.WriteLine(f.name);
            foreach(var i in f.instructions){
                Console.WriteLine(i.opcode+" - "+i.value);
            }
        }
    }

    public void Emit(){
        List<byte[]> codeSection = [];
        foreach(var f in functions){
            var vid = 0;
            foreach(var p in f.parameters){
                p.id = vid;
                vid++;
            }

            Dictionary<Valtype, List<Variable>> locals = [];
            foreach(var l in f.locals){
                if(locals.TryGetValue(l.type.valtype, out List<Variable>? localsOfType)){
                    localsOfType.Add(l);
                }
                else{
                    locals.Add(l.type.valtype, [l]);
                }
            }

            List<byte[]> localBytes = [];
            foreach(var key in locals.Keys){
                var localsOfType = locals[key];
                foreach(var v in localsOfType){
                    v.id = vid;
                    vid++;
                }
                localBytes.Add(WasmEmitter.Local((uint)localsOfType.Count, key));
            }

            List<byte> codeBytes = [];
            foreach(var instruction in f.instructions){
                if(instruction.opcode == Opcode.i32_const){
                    codeBytes.AddRange([(byte)Opcode.i32_const, ..WasmEmitter.SignedLEB128((int)instruction.value!)]);
                }
                else if(instruction.opcode == Opcode.f32_const){
                    codeBytes.AddRange([(byte)Opcode.f32_const, ..WasmEmitter.Ieee754((float)instruction.value!)]);
                }
                else if(instruction.opcode == Opcode.get_local){
                    var localName = (string)instruction.value!;
                    var id = f.FindLocalID(localName);
                    codeBytes.AddRange([(byte)Opcode.get_local, ..WasmEmitter.UnsignedLEB128(id)]);
                }
                else if(instruction.opcode == Opcode.set_local){
                    var localName = (string)instruction.value!;
                    var id = f.FindLocalID(localName);
                    codeBytes.AddRange([(byte)Opcode.set_local, ..WasmEmitter.UnsignedLEB128(id)]);
                }
                else if(instruction.opcode == Opcode.call){
                    var id = (uint)instruction.value!;
                    codeBytes.AddRange([(byte)Opcode.call, ..WasmEmitter.UnsignedLEB128(id)]);
                }
                else if(instruction.opcode == Opcode.@if){
                    codeBytes.AddRange([(byte)Opcode.@if, (byte)(Valtype)instruction.value!]);
                }
                else if(instruction.opcode == Opcode.block){
                    codeBytes.AddRange([(byte)Opcode.block, (byte)(Valtype)instruction.value!]);
                }
                else if(instruction.opcode == Opcode.loop){
                    codeBytes.AddRange([(byte)Opcode.loop, (byte)(Valtype)instruction.value!]);
                }
                else if(instruction.opcode == Opcode.br){
                    var id = (uint)instruction.value!;
                    codeBytes.AddRange([(byte)Opcode.br, ..WasmEmitter.UnsignedLEB128(id)]);
                }
                else{
                    codeBytes.Add((byte)instruction.opcode);
                }
            }
            codeSection.Add(WasmEmitter.Vector([..WasmEmitter.Vector([..localBytes]), ..codeBytes, (byte)Opcode.end]));
        }
    
        List<byte[]> importSection = [];
        foreach(var f in importFunctions){
            importSection.Add([
                ..WasmEmitter.String("env"),
                ..WasmEmitter.String(f.name),
                (byte)ExportType.Func,
                ..WasmEmitter.UnsignedLEB128((uint)f.id)
            ]);
        }

        List<byte[]> typeSection = [];
        foreach(var f in importFunctions){
            typeSection.Add([
                WasmEmitter.functionType,
                ..WasmEmitter.Vector(f.parameters.Select(p=>(byte)p.type.valtype).ToArray()),
                ..WasmEmitter.Return(f.returnType.valtype)
            ]);
        }
        foreach(var f in functions){
            typeSection.Add([
                WasmEmitter.functionType, 
                ..WasmEmitter.Vector(f.parameters.Select(p=>(byte)p.type.valtype).ToArray()), 
                ..WasmEmitter.Return(f.returnType)
            ]);
        }
        
        List<byte[]> funcSection = [];
        foreach(var f in functions){
            funcSection.Add(WasmEmitter.UnsignedLEB128((uint)f.id));
        }

        List<byte[]> exportSection = [];
        foreach(var f in functions){
            exportSection.Add([..WasmEmitter.String(f.name), (byte)ExportType.Func, ..WasmEmitter.UnsignedLEB128((uint)f.id)]);
        }

        byte[] wasm = [
            .. WasmEmitter.MagicModuleHeader,
            .. WasmEmitter.ModuleVersion,
            .. WasmEmitter.Section(SectionType.Type, [..typeSection]),
            .. WasmEmitter.Section(SectionType.Import, [.. importSection]),
            .. WasmEmitter.Section(SectionType.Func, [..funcSection]),
            .. WasmEmitter.Section(SectionType.Export, [..exportSection]),
            .. WasmEmitter.Section(SectionType.Code, [..codeSection])];

        var importStringHelpers = "";
        importStringHelpers+=@"
function ConvertIntToBool(value){
    return value==0?false:true;
}
";
        var importString0 = "";
        foreach(var f in importFunctions){
            importString0 += "function "+f.name+f.EmitParameters()+"{\n";
            importString0 += f.code;
            importString0 += "}\n";
        }

        var importString1 = "";
        foreach(var f in importFunctions){
            importString1 += "imports.env."+f.name+"=function"+f.EmitParameters()+ "{\n";
            importString1 += "return "+f.name+f.EmitArgs()+";";
            importString1 += "\n}\n";
        }
        WasmEmitter.Emit(wasm, importStringHelpers+importString0+importString1);
    }
}