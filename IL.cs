
class ILInstruction(Opcode opcode, object? value = null){
    public Opcode opcode = opcode;
    public object? value = value;
}

class ILVariable(Valtype type, string name){
    public Valtype type = type;
    public string name = name;
    public int id = -1;
}

class ILImportFunction(string name, Valtype returnType, ILVariable[] parameters, string code){
    public string name = name;
    public Valtype returnType = returnType;
    public ILVariable[] parameters = parameters;
    public string code = code;
    public int id = -1;
}

class ILFunction(bool export, string name, Valtype returnType, ILVariable[] parameters, ILVariable[] locals, ILInstruction[] instructions){
    public bool export = export;
    public string name = name;
    public Valtype returnType = returnType;
    public ILVariable[] parameters = parameters;
    public ILVariable[] locals = locals;
    public ILInstruction[] instructions = instructions;
    public int id = -1;

    public uint FindLocalID(string name){
        foreach(var p in parameters){
            if(p.name == name){
                return (uint)p.id;
            }
        }
        return (uint)locals.First(l=>l.name == name).id;
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

    

    uint FindFunctionID(string name){
        foreach(var f in importFunctions){
            if(f.name == name){
                return (uint)f.id;
            }
        }
        return (uint)functions.First(f=>f.name == name).id;
    }

    public void Emit(){
        var fid = 0;
        foreach(var f in importFunctions){
            f.id = fid;
            fid++;
        }
        foreach(var f in functions){
            f.id = fid;
            fid++;
        }

        List<byte[]> codeSection = [];
        foreach(var f in functions){
            var vid = 0;
            foreach(var p in f.parameters){
                p.id = vid;
                vid++;
            }

            Dictionary<Valtype, List<ILVariable>> locals = [];
            foreach(var l in f.locals){
                if(locals.TryGetValue(l.type, out List<ILVariable>? localsOfType)){
                    localsOfType.Add(l);
                }
                else{
                    locals.Add(l.type, [l]);
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
                    var funcName = (string)instruction.value!;
                    var id = FindFunctionID(funcName);
                    codeBytes.AddRange([(byte)Opcode.call, ..WasmEmitter.UnsignedLEB128(id)]);
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
                ..WasmEmitter.Vector(f.parameters.Select(p=>(byte)p.type).ToArray()),
                ..WasmEmitter.Return(f.returnType)
            ]);
        }
        foreach(var f in functions){
            typeSection.Add([
                WasmEmitter.functionType, 
                ..WasmEmitter.Vector(f.parameters.Select(p=>(byte)p.type).ToArray()), 
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

        var importString = "";
        foreach(var f in importFunctions){
            importString += "imports.env."+f.name+"=function(";
            for(var i=0;i<f.parameters.Length;i++){
                importString+=f.parameters[i].name;
                if(i<f.parameters.Length-1){
                    importString+=", ";
                }
            }
            importString += "){\n";
            importString += f.code;
            importString += "\n}\n";
        }
        WasmEmitter.Emit(wasm, importString);
    }
}