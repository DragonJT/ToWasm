
static class Program{
    public static void Main(){
        var code = @"
import void PrintFloat(float value){
    console.log(value);
}

import void PrintBool(bool value){
    console.log(value);
}

import void CreateCanvas(){
    var canvas = document.createElement('canvas');
    canvas.width = 800;
    canvas.height = 600;
    document.body.appendChild(canvas); 
    globals.ctx = canvas.getContext('2d');
}

import void FillRect(float x, float y, float width, float height, float r, float g, float b){
    globals.ctx.fillStyle = 'rgb('+r+','+g+','+b+')';
    globals.ctx.fillRect(x,y,width,height);
}

float Run(){ 
    CreateCanvas();
    FillRect(0,0,800,600,0,0,0);
    var y = 100;
    for{
        FillRect(100,y,100,20,0,y/2,255);
        y = y+50;
        if(y>400){
            break;
        }
    }
    PrintFloat(4.5);
    return 0; 
}";
        var tokenizer = new Tokenizer(code);
        var tokens = tokenizer.Tokenize(0);
        var compiler = new Compiler();
        var il = compiler.Compile(tokens);
        il.Emit();
        il.Print();
    }
} 