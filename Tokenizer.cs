enum TokenType{
    Varname,
    Number,
    Punctuation,
    Return,
    Var,
    Parenthesis,
    Curly,
    Square,
}

class Token(string value, TokenType type){
    public string value = value;
    public TokenType type = type;
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

    static Token VarnameToken(string value){
        return value switch
        {
            "var" => new Token(value, TokenType.Var),
            "return" => new Token(value, TokenType.Return),
            _ => new Token(value, TokenType.Varname),
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

    public List<Token> Tokenize(){
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
                tokens.Add(VarnameToken(code[start..index]));
                return tokens;
            }
            if(IsAlphaNumeric(code[index])){
                goto StartVarname;
            }
            tokens.Add(VarnameToken(code[start..index]));
            goto Start;
        }
        else if(IsDigit(c)){
            var start = index;
            StartNumber:
            index++;
            if(index >= code.Length){
                tokens.Add(new Token(code[start..index], TokenType.Number));
                return tokens;
            }
            if(IsDigit(code[index]) || code[index]=='.'){
                goto StartNumber;
            }
            tokens.Add(new Token(code[start..index], TokenType.Number));
            goto Start;
        }
        else if(c == '('){
            var start = index;
            ReadOpenClose('(', ')');
            tokens.Add(new Token(code[start..index], TokenType.Parenthesis));
            goto Start;
        }
        else if(c == '{'){
           var start = index;
            ReadOpenClose('{', '}');
            tokens.Add(new Token(code[start..index], TokenType.Curly));
            goto Start;
        }
        else if(c == '['){
           var start = index;
            ReadOpenClose('[', ']');
            tokens.Add(new Token(code[start..index], TokenType.Square));
            goto Start;
        }
        else if(IsWhitespace(c)){
            index++;
            goto Start;
        }
        else{
            index++;
            tokens.Add(new Token(c.ToString(), TokenType.Punctuation));
            goto Start;
        }
    }
}