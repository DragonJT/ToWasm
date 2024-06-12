enum TokenType{
    Varname,
    Number,
    Punctuation,
    Return,
    Var,
    If,
    For,
    Break,
    True,
    False,
    Import,
    Parentheses,
    Curly,
    Square,
}

class Token(string value, TokenType type, int start){
    public string value = value;
    public TokenType type = type;
    public int start = start;

    public string GetVarnameValue(){
        if(type == TokenType.Varname){
            return value;
        }
        throw new Exception("Expecting varname");
    }

    public List<Token> GetCurlyTokens(){
        if(type == TokenType.Curly){
            return new Tokenizer(value[1..^1]).Tokenize(start+1);
        }
        throw new Exception("Expecting curly");
    }

    public string GetJavascript(){
        if(type == TokenType.Curly){
            return value[1..^1];
        }
        throw new Exception("Expecting curly");
    }

    public List<Token> GetParenthesesTokens(){
        if(type == TokenType.Parentheses){
            return new Tokenizer(value[1..^1]).Tokenize(start+1);
        }
        throw new Exception("Expecting parentheses");
    }
}

class Tokenizer(string code){
    string code = code;
    int index = 0;
    
    static bool IsWhitespace(char c){
        return c==' ' || c=='\t' || c=='\r' || c=='\n';
    }

    static bool IsCharacter(char c){
        return (c>='a' && c<='z') || (c>='A' && c<='Z') || c=='_';
    }

    static bool IsDigit(char c){
        return c>='0' && c<='9';
    }

    static bool IsAlphaNumeric(char c){
        return IsCharacter(c) || IsDigit(c);
    }

    static Token VarnameToken(string value, int start){
        return value switch
        {
            "var" => new Token(value, TokenType.Var, start),
            "return" => new Token(value, TokenType.Return, start),
            "import" => new Token(value, TokenType.Import, start),
            "true" => new Token(value, TokenType.True, start),
            "false" => new Token(value, TokenType.False, start),
            "if" => new Token(value, TokenType.If, start),
            "for" => new Token(value, TokenType.For, start),
            "break" => new Token(value, TokenType.Break, start),
            _ => new Token(value, TokenType.Varname, start),
        };
    }

    void ReadOpenClose(char open, char close){
        var depth = 1;
        Start:
        index++;
        if(code[index] == open){
            depth++;
        }
        else if(code[index] == close){
            depth--;
            if(depth<=0){
                index++;
                return;
            }
        }
        goto Start;
    }

    public List<Token> Tokenize(int offset){
        List<Token> tokens = [];
        Start:
        if(index >= code.Length){
            return tokens;
        }
        var c = code[index];
        if(IsCharacter(c)){
            var start = index;
            StartVarname:
            index++;
            if(index>=code.Length){
                tokens.Add(VarnameToken(code[start..index], start+offset));
                return tokens;
            }
            if(IsAlphaNumeric(code[index])){
                goto StartVarname;
            }
            tokens.Add(VarnameToken(code[start..index], start+offset));
            goto Start;
        }
        else if(IsDigit(c)){
            var start = index;
            StartNumber:
            index++;
            if(index >= code.Length){
                tokens.Add(new Token(code[start..index], TokenType.Number, start+offset));
                return tokens;
            }
            if(IsDigit(code[index]) || code[index]=='.'){
                goto StartNumber;
            }
            tokens.Add(new Token(code[start..index], TokenType.Number, start+offset));
            goto Start;
        }
        else if(c == '('){
            var start = index;
            ReadOpenClose('(', ')');
            tokens.Add(new Token(code[start..index], TokenType.Parentheses, start+offset));
            goto Start;
        }
        else if(c == '{'){
           var start = index;
            ReadOpenClose('{', '}');
            tokens.Add(new Token(code[start..index], TokenType.Curly, start+offset));
            goto Start;
        }
        else if(c == '['){
           var start = index;
            ReadOpenClose('[', ']');
            tokens.Add(new Token(code[start..index], TokenType.Square, start+offset));
            goto Start;
        }
        else if(IsWhitespace(c)){
            index++;
            goto Start;
        }
        else{
            tokens.Add(new Token(c.ToString(), TokenType.Punctuation, index+offset));
            index++;
            goto Start;
        }
    }
}