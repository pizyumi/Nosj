using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nosj
{
    //文字に対する拡張
    public static class CharExtensions
    {
        //制御文字か
        public static bool IsControl(this char c)
        {
            return c >= 0 && c <= 0x1F;
        }

        //空白文字か
        public static bool IsWhitespace(this char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        //数字か
        public static bool IsDigit(this char c)
        {
            return c >= '0' && c <= '9';
        }

        //16進数の数字か
        public static bool IsHexDigit(this char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        //文字を16進数の数字として解釈する
        public static int ToHexValue(this char c)
        {
            //16進数の数字でない場合には例外を投げる
            if (!c.IsHexDigit())
                throw new Exception();

            return c >= '0' && c <= '9' ? c - '0' : char.ToUpper(c) - 'A' + 10;
        }
    }

    //文字列中の位置に関する情報
    public class PositionInfo
    {
        public PositionInfo(string _line, int _lineNumber, int _linePosition)
        {
            Line = _line;
            LineNumber = _lineNumber;
            LinePosition = _linePosition;
        }

        //文字列中の位置における行の文字列
        public string Line { get; private set; }
        //文字列中の位置が何行目であるか
        public int LineNumber { get; private set; }
        //文字列中の位置が（その行の）何文字目であるか
        public int LinePosition { get; private set; }
    }

    //文字列を1文字ずつ処理するためのインターフェイス
    public interface IText
    {
        //文字列中の現在位置に関する情報
        PositionInfo PositionInfo { get; }

        //現在位置の文字を返す
        char Peek();
        //現在位置を1文字分進める
        void Advance();
    }

    //文字列を1文字ずつ処理するためのインターフェイスに対する拡張
    public static class ITextExtension
    {
        //現在位置を1文字分進め、現在位置の文字を返す
        public static char AdvancePeek(this IText itext)
        {
            itext.Advance();

            return itext.Peek();
        }
    }

    //現在位置が文字列の範囲外である場合に投げられる例外
    public class OutOfRangeException : Exception { }

    //文字列クラス
    //文字列を1文字ずつ処理するためのインターフェイスを実装する
    public class Text : IText
    {
        //文字列を受け取る
        public Text(string s)
        {
            //文字列の末尾を示す文字
            const string nullchar = "\0";

            //文字列の末尾が文字列の末尾を示す文字でない場合には文字列に末尾を示す文字を追加する
            //そうでない場合には何もしない
            if (!s.EndsWith(nullchar))
                s += nullchar;

            //文字列を行毎に分割する（ただし、それぞれの行の末尾には改行文字を追加する）
            lines = s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Select((elem) => elem + Environment.NewLine).ToArray();
            //文字列の現在の行を0行目とする
            lineNumber = 0;
            //文字列の現在の行の現在の文字の位置を0文字目とする
            linePosition = 0;
        }

        //文字列の全ての行
        private string[] lines;
        //文字列の現在の行
        private int lineNumber;
        //文字列の現在の行の現在の文字の位置
        private int linePosition;

        //文字列中の現在位置に関する情報
        public PositionInfo PositionInfo
        {
            get
            {
                try
                {
                    return new PositionInfo(lines[lineNumber], lineNumber, linePosition);
                }
                catch (IndexOutOfRangeException)
                {
                    //現在位置が文字列の範囲外である場合には例外を投げる
                    throw new OutOfRangeException();
                }
            }
        }

        //現在位置の文字を返す
        public char Peek()
        {
            try
            {
                return lines[lineNumber][linePosition];
            }
            catch (IndexOutOfRangeException)
            {
                //現在位置が文字列の範囲外である場合には例外を投げる
                throw new OutOfRangeException();
            }
        }

        //現在位置を1文字分進める
        public void Advance()
        {
            try
            {
                //現在の文字が現在の行の末尾に位置する場合には現在の行を次の行に更新し、現在の文字の位置を0文字目とする
                if (linePosition == lines[lineNumber].Length - 1)
                {
                    lineNumber++;
                    linePosition = 0;
                }
                //そうでない場合には現在の文字の位置を1進める
                else
                    linePosition++;
            }
            catch (IndexOutOfRangeException)
            {
                //現在位置が文字列の範囲外である場合には例外を投げる
                throw new OutOfRangeException();
            }
        }
    }

    //構文解析の汎用的な処理を行う
    public class Scanner
    {
        //文字列クラスを受け取る
        public Scanner(IText _itext)
        {
            //文字列クラスを設定する
            Itext = _itext;
            //現在の文字列を空にする
            Scan = string.Empty;
            //現在位置の文字を設定する
            Current = Itext.Peek();
        }

        //文字列クラス
        public IText Itext { get; private set; }
        //現在の文字列
        public string Scan { get; private set; }
        //現在位置の文字
        public char Current { get; private set; }

        //現在の文字列を空にする
        public void Empty()
        {
            Scan = string.Empty;
        }

        //現在位置の文字が述語を充足するか確認する
        public bool Check(Func<char, bool> func)
        {
            return func(Current);
        }

        //現在の文字列に文字列を付加する
        public void Add(string s)
        {
            Scan += s;
        }

        //現在位置を1文字分進め、現在位置の文字を更新する
        public void Advance()
        {
            Current = Itext.AdvancePeek();
        }

        //現在の文字列に現在位置の文字を付加し、現在位置を1文字分進め、現在位置の文字を更新する
        public void AddAdvance()
        {
            Scan += Current;
            Current = Itext.AdvancePeek();
        }

        //現在位置の文字が述語を充足するか確認し、充足する場合には現在位置を1文字分進め、現在位置の文字を更新する
        public bool CheckAdvance(Func<char, bool> func)
        {
            if (func(Current))
            {
                Current = Itext.AdvancePeek();

                return true;
            }

            return false;
        }

        //現在位置以降で与えられた文字列と一致する位置まで現在位置を進め、現在位置の文字を更新する
        public bool CheckAdvance(string s)
        {
            //与えられた文字列の現在の対象位置
            int i = 0;

            //与えられた文字列の現在の対象位置の文字と一致するかを確認する述語
            Func<char, bool> func = (_) => _ == s[i];

            //与えられた文字列の冒頭から末尾まで述語を充足するか確認する
            //充足しなくなった時点で処理を終了する
            for (; i < s.Length; i++)
                if (!CheckAdvance(func))
                    return false;

            return true;
        }

        //現在位置の文字が述語を充足するか確認し、充足する場合には現在位置を1文字分進め、現在位置の文字を更新する
        //ただし、充足しなくなるまで反復する
        public bool CheckAdvanceLoop(Func<char, bool> func)
        {
            bool b = false;

            while (CheckAdvance(func))
                b = true;

            return b;
        }

        //現在位置の文字が述語を充足するか確認し、充足する場合には現在の文字列に現在位置の文字を付加し、現在位置を1文字分進め、現在位置の文字を更新する
        public bool CheckAddAdvance(Func<char, bool> func)
        {
            if (func(Current))
            {
                Scan += Current;

                Current = Itext.AdvancePeek();

                return true;
            }

            return false;
        }

        //現在位置の文字が述語を充足するか確認し、充足する場合には現在の文字列に現在位置の文字を付加し、現在位置を1文字分進め、現在位置の文字を更新する
        //ただし、充足しなくなるまで反復する
        public bool CheckAddAdvanceLoop(Func<char, bool> func)
        {
            bool b = false;

            while (CheckAddAdvance(func))
                b = true;

            return b;
        }
    }

    //構文解析が失敗した場合に投げられる例外
    public class ParserException : Exception
    {
        public ParserException(string _message, PositionInfo _positionInfo) : base(_message)
        {
            PositionInfo = _positionInfo;
        }

        //文字列中で構文解析が失敗した位置に関する情報
        public PositionInfo PositionInfo { get; private set; }
    }

    //JSONの構文解析を行う
    public static class JsonParser
    {
        //文字が符号であるか
        private static Func<char, bool> IsSign = (c) => c == '-' || c == '+';
        //文字が数字であるか
        private static Func<char, bool> IsDigit = (c) => c.IsDigit();
        //文字が小数点であるか
        private static Func<char, bool> IsDecimalPoint = (c) => c == '.';
        //文字がeかEであるか
        private static Func<char, bool> IsExponent = (c) => c == 'e' || c == 'E';
        //文字が二重引用符であるか
        private static Func<char, bool> IsDoubleQuote = (c) => c == '"';
        //文字が逆斜線であるか
        private static Func<char, bool> IsBackslash = (c) => c == '\\';
        //文字が斜線であるか
        private static Func<char, bool> IsSlash = (c) => c == '/';
        //文字がbであるか
        private static Func<char, bool> IsSmallB = (c) => c == 'b';
        //文字がfであるか
        private static Func<char, bool> IsSmallF = (c) => c == 'f';
        //文字がnであるか
        private static Func<char, bool> IsSmallN = (c) => c == 'n';
        //文字がrであるか
        private static Func<char, bool> IsSmallR = (c) => c == 'r';
        //文字がtであるか
        private static Func<char, bool> IsSmallT = (c) => c == 't';
        //文字がuであるか
        private static Func<char, bool> IsSmallU = (c) => c == 'u';
        //文字が制御文字であるか
        private static Func<char, bool> IsControl = (c) => c.IsControl();
        //文字が空白文字であるか
        private static Func<char, bool> IsWhitespace = (c) => c.IsWhitespace();
        //文字が左角括弧であるか
        private static Func<char, bool> IsLeftBracket = (c) => c == '[';
        //文字が右角括弧であるか
        private static Func<char, bool> IsRightBracket = (c) => c == ']';
        //文字がカンマであるか
        private static Func<char, bool> IsComma = (c) => c == ',';
        //文字が左波括弧であるか
        private static Func<char, bool> IsLeftBrace = (c) => c == '{';
        //文字が右波括弧であるか
        private static Func<char, bool> IsRightBrace = (c) => c == '}';
        //文字がコロンであるか
        private static Func<char, bool> IsColon = (c) => c == ':';

        //数値を構文解析する
        private static double ParseNumber(Scanner scanner)
        {
            //現在の文字列を空にする
            scanner.Empty();

            //現在位置の文字が符号であるか確認する
            //符号である場合には現在の文字列に付加する
            //更に現在位置を1文字分進める
            scanner.CheckAddAdvance(IsSign);

            //現在位置の文字が数字であるか確認する
            //数字である場合には現在の文字列に付加する
            //更に現在位置を1文字分進める
            //これを現在位置の文字が数字でなくなるまで反復する
            scanner.CheckAddAdvanceLoop(IsDigit);

            //現在位置の文字が小数点であるか確認する
            //小数点である場合には現在の文字列に付加する
            //更に現在位置を1文字分進める
            scanner.CheckAddAdvance(IsDecimalPoint);

            //現在位置の文字が数字であるか確認する
            //数字である場合には現在の文字列に付加する
            //更に現在位置を1文字分進める
            //これを現在位置の文字が数字でなくなるまで反復する
            scanner.CheckAddAdvanceLoop(IsDigit);

            //現在位置の文字がeかEであるか確認する
            //eかEである場合には現在の文字列に付加する
            //更に現在位置を1文字分進める
            if (scanner.CheckAddAdvance(IsExponent))
            {
                //現在位置の文字が符号であるか確認する
                //符号である場合には現在の文字列に付加する
                //更に現在位置を1文字分進める
                scanner.CheckAddAdvance(IsSign);

                //現在位置の文字が数字であるか確認する
                //数字である場合には現在の文字列に付加する
                //更に現在位置を1文字分進める
                //これを現在位置の文字が数字でなくなるまで反復する
                scanner.CheckAddAdvanceLoop(IsDigit);
            }

            try
            {
                //現在の文字列をdouble型に変換し、返す
                return double.Parse(scanner.Scan);
            }
            //現在の文字列が数値として解釈できなかった場合には例外を投げる
            catch (FormatException)
            {
                throw new ParserException("bad format number", scanner.Itext.PositionInfo);
            }
            //現在の文字列を数値として解釈した場合の値がdouble型の値の範囲を超えている場合には例外を投げる
            catch (OverflowException)
            {
                throw new ParserException("overflow number", scanner.Itext.PositionInfo);
            }
            catch (Exception)
            {
                throw new ParserException("unexpected error number", scanner.Itext.PositionInfo);
            }
        }

        //文字列を構文解析する
        private static string ParseString(Scanner scanner)
        {
            //1文字後退
            const string b = "\b";
            //改ページ
            const string f = "\f";
            //改行
            const string n = "\n";
            //復帰
            const string r = "\r";
            //水平タブ
            const string t = "\t";

            //現在の文字列を空にする
            scanner.Empty();

            //現在位置の文字が二重引用符であるか確認する
            //二重引用符である場合には現在位置を1文字分進める
            //そうでない場合には例外を投げる
            if (!scanner.CheckAdvance(IsDoubleQuote))
                throw new ParserException("string -> double quotation mark required", scanner.Itext.PositionInfo);

            //現在位置の文字が二重引用符であるか確認する
            //二重引用符である場合には現在位置を1文字分進め、次の処理に進む
            while (!scanner.CheckAdvance(IsDoubleQuote))
            {
                //現在位置の文字が逆斜線であるか確認する
                //逆斜線である場合には現在位置を1文字分進める
                if (scanner.CheckAdvance(IsBackslash))
                {
                    //現在位置の文字が二重引用符であるか確認する
                    //二重引用符である場合には現在の文字列に付加する
                    //更に現在位置を1文字分進める
                    if (scanner.CheckAddAdvance(IsDoubleQuote))
                        continue;

                    //現在位置の文字が逆斜線であるか確認する
                    //逆斜線である場合には現在の文字列に付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAddAdvance(IsBackslash))
                        continue;

                    //現在位置の文字が斜線であるか確認する
                    //斜線である場合には現在の文字列に付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAddAdvance(IsSlash))
                        continue;

                    //現在位置の文字がbであるか確認する
                    //bである場合には現在の文字列に1文字後退を付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallB))
                        scanner.Add(b);

                    //現在位置の文字がfであるか確認する
                    //fである場合には現在の文字列に改ページを付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallF))
                        scanner.Add(f);

                    //現在位置の文字がnであるか確認する
                    //nである場合には現在の文字列に改行を付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallN))
                        scanner.Add(n);

                    //現在位置の文字がrであるか確認する
                    //rである場合には現在の文字列に復帰を付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallR))
                        scanner.Add(r);

                    //現在位置の文字がtであるか確認する
                    //tである場合には現在の文字列に水平タブを付加する
                    //更に現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallT))
                        scanner.Add(t);

                    //現在位置の文字がuであるか確認する
                    //uである場合には現在位置を1文字分進める
                    else if (scanner.CheckAdvance(IsSmallU))
                    {
                        //符号位置
                        int codepoint = 0;

                        //符号位置を計算する
                        //符号位置は現在位置から最大で4文字分の文字列を16進数の数値として解釈したものである
                        for (int i = 0; i < 4; i++)
                            try
                            {
                                codepoint = codepoint * 16 + scanner.Current.ToHexValue();

                                scanner.Advance();
                            }
                            catch (Exception)
                            {
                                break;
                            }

                        //現在の文字列に符号位置に対応する文字を付加する
                        scanner.Add(Convert.ToChar(codepoint).ToString());
                    }

                    //現在位置の文字が何れでもない場合には例外を投げる
                    else
                        throw new ParserException("string -> not supported escape", scanner.Itext.PositionInfo);
                }

                //現在位置の文字が制御文字である場合には例外を投げる
                else if (scanner.Check(IsControl))
                    throw new ParserException("string -> control character", scanner.Itext.PositionInfo);

                //現在位置の文字が何れでもない場合には現在の文字列に付加する
                //更に現在位置を1文字分進める
                else
                    scanner.AddAdvance();
            }

            //現在の文字列を返す
            return scanner.Scan;
        }

        //空白を構文解析する
        private static void ParseWhitespace(Scanner scanner)
        {
            //現在位置の文字が空白文字であるか確認する
            //空白文字である場合には現在位置を1文字分進める
            //これを現在位置の文字が空白文字でなくなるまで反復する
            scanner.CheckAdvanceLoop(IsWhitespace);
        }

        //配列を構文解析する
        private static object[] ParseArray(Scanner scanner)
        {
            //配列の要素を格納するリスト
            List<object> osList = new List<object>();

            //現在位置の文字が左角括弧であるか確認する
            //左角括弧である場合には現在位置を1文字分進める
            //そうでない場合には例外を投げる
            if (!scanner.CheckAdvance(IsLeftBracket))
                throw new ParserException("array -> left bracket required", scanner.Itext.PositionInfo);

            //空白を構文解析する
            ParseWhitespace(scanner);

            //現在位置の文字が右角括弧であるか確認する
            //右角括弧である場合には現在位置を1文字分進める
            //更に配列の要素を格納するリストを配列に変換し、返す
            if (scanner.CheckAdvance(IsRightBracket))
                return osList.ToArray();

            while (true)
            {
                //値を構文解析し、配列の要素を格納するリストに追加する
                osList.Add(ParseValue(scanner));

                //空白を構文解析する
                ParseWhitespace(scanner);

                //現在位置の文字が右角括弧であるか確認する
                //右角括弧である場合には現在位置を1文字分進める
                //更に配列の要素を格納するリストを配列に変換し、返す
                if (scanner.CheckAdvance(IsRightBracket))
                    return osList.ToArray();

                //現在位置の文字がカンマであるか確認する
                //カンマである場合には現在位置を1文字分進める
                //そうでない場合には例外を投げる
                else if (!scanner.CheckAdvance(IsComma))
                    throw new ParserException("array -> comma required", scanner.Itext.PositionInfo);

                //空白を構文解析する
                ParseWhitespace(scanner);
            }
        }

        //オブジェクトを構文解析する
        private static Dictionary<string, object> ParseObject(Scanner scanner)
        {
            //オブジェクトの内容を格納する辞書
            Dictionary<string, object> osDict = new Dictionary<string, object>();

            //現在位置の文字が左波括弧であるか確認する
            //左波括弧である場合には現在位置を1文字分進める
            //そうでない場合には例外を投げる
            if (!scanner.CheckAdvance(IsLeftBrace))
                throw new ParserException("object -> left brace required", scanner.Itext.PositionInfo);

            //空白を構文解析する
            ParseWhitespace(scanner);

            //現在位置の文字が右波括弧であるか確認する
            //右波括弧である場合には現在位置を1文字分進める
            //更にオブジェクトの内容を格納する辞書を返す
            if (scanner.CheckAdvance(IsRightBrace))
                return osDict;

            while (true)
            {
                //文字列を構文解析し、オブジェクトの鍵とする
                string key = ParseString(scanner);

                //既に同一の鍵が存在する場合には例外を投げる
                if (osDict.Keys.Contains(key))
                    throw new ParserException("object -> same key", scanner.Itext.PositionInfo);

                //空白を構文解析する
                ParseWhitespace(scanner);

                //現在位置の文字がコロンであるか確認する
                //コロンである場合には現在位置を1文字分進める
                //そうでない場合には例外を投げる
                if (!scanner.CheckAdvance(IsColon))
                    throw new ParserException("object -> colon required", scanner.Itext.PositionInfo);

                //空白を構文解析する
                ParseWhitespace(scanner);

                //値を構文解析し、オブジェクトの鍵に対応する値とする
                osDict[key] = ParseValue(scanner);

                //空白を構文解析する
                ParseWhitespace(scanner);

                //現在位置の文字が右波括弧であるか確認する
                //右波括弧である場合には現在位置を1文字分進める
                //更にオブジェクトの内容を格納する辞書を返す
                if (scanner.CheckAdvance(IsRightBrace))
                    return osDict;

                //現在位置の文字がカンマであるか確認する
                //カンマである場合には現在位置を1文字分進める
                //そうでない場合には例外を投げる
                else if (!scanner.CheckAdvance(IsComma))
                    throw new ParserException("object -> comma required", scanner.Itext.PositionInfo);

                //空白を構文解析する
                ParseWhitespace(scanner);
            }
        }

        //予約語を構文解析する
        private static object ParseWord(Scanner scanner)
        {
            //現在位置の文字がtであるか確認する
            if (scanner.Check(IsSmallT))
            {
                //現在位置以降がtrueで始まるか確認する
                //始まる場合には現在位置を4文字分進める
                //そうでない場合には例外を投げる
                if (!scanner.CheckAdvance("true"))
                    throw new ParserException("word -> true required", scanner.Itext.PositionInfo);

                return true;
            }
            //現在位置の文字がfであるか確認する
            else if (scanner.Check(IsSmallF))
            {
                //現在位置以降がfalseで始まるか確認する
                //始まる場合には現在位置を5文字分進める
                //そうでない場合には例外を投げる
                if (!scanner.CheckAdvance("false"))
                    throw new ParserException("word -> false required", scanner.Itext.PositionInfo);

                return false;
            }
            //現在位置の文字がnであるか確認する
            else if (scanner.Check(IsSmallN))
            {
                //現在位置以降がnullで始まるか確認する
                //始まる場合には現在位置を4文字分進める
                //そうでない場合には例外を投げる
                if (!scanner.CheckAdvance("null"))
                    throw new ParserException("word -> null required", scanner.Itext.PositionInfo);

                return null;
            }
            //現在位置の文字が何れでもない場合には例外を投げる
            else
                throw new ParserException("word -> unexpected character", scanner.Itext.PositionInfo);
        }

        //値を構文解析する
        private static object ParseValue(Scanner scanner)
        {
            //空白を構文解析する
            ParseWhitespace(scanner);

            //現在位置の文字が左波括弧であるか確認する
            if (scanner.Check(IsLeftBrace))
                //オブジェクトを構文解析する
                return ParseObject(scanner);
            //現在位置の文字が左角括弧であるか確認する
            else if (scanner.Check(IsLeftBracket))
                //配列を構文解析する
                return ParseArray(scanner);
            //現在位置の文字が二重引用符であるか確認する
            else if (scanner.Check(IsDoubleQuote))
                //文字列を構文解析する
                return ParseString(scanner);
            //現在位置の文字が符号であるか確認する
            else if (scanner.Check(IsSign))
                //数値を構文解析する
                return ParseNumber(scanner);
            //現在位置の文字が数字であるか確認する
            else if (scanner.Check(IsDigit))
                //数値を構文解析する
                return ParseNumber(scanner);
            else
                //予約語を構文解析する
                return ParseWord(scanner);
        }

        //JSONを構文解析する
        public static object Parse(Scanner scanner)
        {
            //値を構文解析する
            return ParseValue(scanner);
        }
    }

    //JSON形式のオブジェクトから値を取得可能なプロパティであることを示す属性
    //プロパティに付与することができる
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class JsonPropertyAttribute : Attribute
    {
        public JsonPropertyAttribute()
        {
            Name = Option.Return<string>();
        }

        public JsonPropertyAttribute(string _name)
        {
            Name = Option.Return(_name);
        }

        //JSON形式のオブジェクトにおける名称
        public IOption<string> Name { get; private set; }
    }

    //JSON形式のオブジェクトに対応する名称の要素が含まれていない場合に投げられる例外
    public class NotFoundException : Exception
    {
        public NotFoundException(string _message, string _name) : base(_message)
        {
            Name = _name;
        }

        //要素の名称
        public string Name { get; private set; }
    }

    //プロパティの型と要素の型が合わない場合に投げられる例外
    public class TypeMismatchException : Exception { }

    //値の型の変換が失敗した場合に投げられる例外
    public class ConversionException : Exception
    {
        public ConversionException(string _message) : base(_message) { }
    }

    //変換クラス
    //変換の内容を表すクラス
    public class Conversion
    {
        public Conversion(Func<Type, bool> _canConvert, Func<Type, Type> _typeConverter, Func<object, Type, object> _converter)
        {
            CanConvert = _canConvert;
            TypeConverter = _typeConverter;
            Converter = _converter;
        }

        //引数として受け取った変換先の型がこの変換クラスで対応している型であるかを返す関数
        public Func<Type, bool> CanConvert { get; private set; }
        //引数として変換先の型を受け取り、変換元の型を返す関数
        public Func<Type, Type> TypeConverter { get; private set; }
        //変換前の値と変換元の型を受け取り、変換後の値を返す関数
        public Func<object, Type, object> Converter { get; private set; }
    }

    public static class JsonMapper
    {
        static JsonMapper()
        {
            //運用記録を作成するためのオブジェクトを作成する
            Logger = new Logger();
        }

        //この機能の正式名称
        private static readonly string FormalName = "JsonMapper";

        //運用記録を作成するためのオブジェクト
        private static Logger Logger { get; set; }

        //オブジェクトをdouble型の値に変換し、返す
        private static double ObjectToDouble(object o)
        {
            //オブジェクトがstring型の実体である場合
            if (o is string)
            {
                try
                {
                    //オブジェクトをstring型にキャストしてdouble型の値に変換し、返す
                    return double.Parse((string)o);
                }
                //double型の値として解釈できなかった場合には例外を投げる
                catch (FormatException)
                {
                    throw new ConversionException("bad format");
                }
                //double型の値の範囲外であった場合には例外を投げる
                catch (OverflowException)
                {
                    throw new ConversionException("overflow");
                }
                //それ以外の問題が発生した場合にも例外を投げる
                catch (Exception)
                {
                    throw new ConversionException("unexpected error");
                }
            }
            //オブジェクトがdouble型の値である場合にはオブジェクトをdouble型にキャストし、返す
            else if (o is double)
                return (double)o;
            //オブジェクトが何れの型の実体でもない場合には例外を投げる
            else
                throw new TypeMismatchException();
        }

        //オブジェクトを数値型の値に変換し、返す
        private static T GetValueSmallerDouble<T>(object o, Func<double, T> converter, Func<T, double> deconverter, double max, double min)
        {
            //オブジェクトをdouble型の値に変換する
            double d = ObjectToDouble(o);

            //値が最大許容値より大きいか、最小許容値より小さい場合には例外を投げる
            if (Math.Floor(d) > max || Math.Ceiling(d) < min)
                throw new ConversionException("overflow");

            //double型の値をT型の実体に変換する
            T b = converter(d);

            //元のdouble型の値とT型の実体をdouble型に変換した値が一致しない（値の精度が落ちた）場合
            if (d != deconverter(b))
            {
                //警告の運用記録を出力する
                const string id = "9e6746a5-42c5-4851-ba2c-ee77f66f4345";
                const string text = "rounded off fraction";
                const LogType type = LogType.Warning;

                Logger.IssueLog(new LogItem(Guid.Parse(id), text, type, FormalName));
            }

            //T型の実体を返す
            return b;
        }

        //オブジェクトを与えられた型の実体に変換し、返す
        private static object GetValue(object o, Type pType, IEnumerable<Conversion> conversions)
        {
            //型がNullable<T>型であり、オブジェクトがnullである場合にはnullを返す
            if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(Nullable<>) && o == null)
                return null;

            //これより下の条件分岐では、型がNullable<T>型である場合には必ずオブジェクトはnull以外となる
            //Nullable<T>型を返さなければならない場合にT型を返しても勝手に変換されるので問題ない

            //型がbyte型又はbyte?型である場合にはオブジェクトをbyte型の値に変換し、返す
            else if (pType == typeof(byte) || pType == typeof(byte?))
                return GetValueSmallerDouble(o, (d) => (byte)d, (b) => b, byte.MaxValue, byte.MinValue);
            //型がsbyte型又はsbyte?型である場合にはオブジェクトをsbyte型の値に変換し、返す
            else if (pType == typeof(sbyte) || pType == typeof(sbyte?))
                return GetValueSmallerDouble(o, (d) => (sbyte)d, (b) => b, sbyte.MaxValue, sbyte.MinValue);
            //型がint型又はint?型である場合にはオブジェクトをint型の値に変換し、返す
            else if (pType == typeof(int) || pType == typeof(int?))
                return GetValueSmallerDouble(o, (d) => (int)d, (b) => b, int.MaxValue, int.MinValue);
            //型がuint型又はuint?型である場合にはオブジェクトをuint型の値に変換し、返す
            else if (pType == typeof(uint) || pType == typeof(uint?))
                return GetValueSmallerDouble(o, (d) => (uint)d, (b) => b, uint.MaxValue, uint.MinValue);
            //型がshort型又はshort?型である場合にはオブジェクトをshort型の値に変換し、返す
            else if (pType == typeof(short) || pType == typeof(short?))
                return GetValueSmallerDouble(o, (d) => (short)d, (b) => b, short.MaxValue, short.MinValue);
            //型がushort型又はushort?型である場合にはオブジェクトをushort型の値に変換し、返す
            else if (pType == typeof(ushort) || pType == typeof(ushort?))
                return GetValueSmallerDouble(o, (d) => (ushort)d, (b) => b, ushort.MaxValue, ushort.MinValue);
            //型がlong型又はlong?型である場合にはオブジェクトをlong型の値に変換し、返す
            else if (pType == typeof(long) || pType == typeof(long?))
                return GetValueSmallerDouble(o, (d) => (long)d, (b) => b, long.MaxValue, long.MinValue);
            //型がulong型又はulong?型である場合にはオブジェクトをulong型の値に変換し、返す
            else if (pType == typeof(ulong) || pType == typeof(ulong?))
                return GetValueSmallerDouble(o, (d) => (ulong)d, (b) => b, ulong.MaxValue, ulong.MinValue);
            //型がfloat型又はfloat?型である場合にはオブジェクトをfloat型の値に変換し、返す
            else if (pType == typeof(float) || pType == typeof(float?))
                return GetValueSmallerDouble(o, (d) => (float)d, (b) => b, float.MaxValue, float.MinValue);
            //型がdouble型又はdouble?型である場合にはオブジェクトをdouble型の値に変換し、返す
            else if (pType == typeof(double) || pType == typeof(double?))
                return ObjectToDouble(o);
            //型がchar型又はchar?型である場合
            else if (pType == typeof(char) || pType == typeof(char?))
            {
                //オブジェクトがstring型の実体でない場合には例外を投げる
                if (!(o is string))
                    throw new TypeMismatchException();

                //オブジェクトをstring型にキャストする
                string s = (string)o;

                //文字列の長さが1でない場合には例外を投げる
                if (s.Length != 1)
                    throw new ConversionException("too long");

                //文字列の0文字目を返す
                return s[0];
            }
            //型がbool型又はbool?型である場合
            else if (pType == typeof(bool) || pType == typeof(bool?))
            {
                //オブジェクトがbool型の実体でない場合には例外を投げる
                if (!(o is bool))
                    throw new TypeMismatchException();

                //オブジェクトをbool型にキャストし、返す
                return (bool)o;
            }
            //型がstring型である場合
            else if (pType == typeof(string))
            {
                //オブジェクトがnullでなく、string型の実体でもない場合には例外を投げる
                if (o != null && !(o is string))
                    throw new TypeMismatchException();

                //オブジェクトをstring型にキャストし、返す
                return (string)o;
            }
            //型がdecimal型又はdecimal?型である場合にはオブジェクトをdecimal型の値に変換し、返す
            else if (pType == typeof(decimal) || pType == typeof(decimal?))
                return (decimal)ObjectToDouble(o);
            else
            {
                //オブジェクトがnullである場合にはnullを返す
                if (o == null)
                    return null;

                //オブジェクトがJSON形式のオブジェクトの型（Dictionary<string, object>）の実体でない場合には例外を投げる
                if (!(o is Dictionary<string, object>))
                    throw new TypeMismatchException();

                //JSON形式のオブジェクトを与えられた型の実体に変換する
                return MapObject(o as Dictionary<string, object>, pType, conversions);
            }
        }

        //JSON形式の値を与えられた型の実体に変換し、返す
        private static object MapValue(object o, Type type, IEnumerable<Conversion> conversions)
        {
            //変換先の型が配列型である場合
            if (type.IsArray)
            {
                //配列型の次元が1でない場合には例外を投げる
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("multidimensional array is not supported");

                //オブジェクトがnullである場合にはnullを返す
                if (o == null)
                    return o;

                //オブジェクトがobject[]型の実体でない場合には例外を投げる
                if (!(o is object[]))
                    throw new TypeMismatchException();

                //配列の要素の型を取得する
                Type aType = type.GetElementType();

                //オブジェクトをobject[]型にキャストする
                object[] osIn = (object[])o;
                //配列を作成する
                Array osOut = Array.CreateInstance(aType, osIn.Length);

                //配列の要素にオブジェクトの要素を変換した結果を設定する
                for (int i = 0; i < osIn.Length; i++)
                    osOut.SetValue(MapValue(osIn[i], aType, conversions), i);

                //配列を返す
                return osOut;
            }
            //変換先の型が配列型でない場合
            else
            {
                //変換クラスと変換元の型の系列
                Stack<Tuple<IOption<Conversion>, Type>> conversionsStack = new Stack<Tuple<IOption<Conversion>, Type>>();
                //変換元の型
                Type cType = type;

                //変換前の値
                object co = null;

                //変換が提供されていないか調べる
                while (true)
                {
                    //変換先の型に対応する変換クラスを取得する
                    IOption<Conversion> conversion = conversions.FirstOption((elem) => elem.CanConvert(cType));

                    //変換元の型を取得する
                    Type nextType = conversion.Map((elem) => elem.TypeConverter).GetOrDefault(_.Identity<Type>())(cType);

                    //これ以上の変換がない場合には変換元の型は確定である
                    if (cType == nextType)
                        break;
                    //そうでない場合には変換クラスと変換元の型を系列に追加し、更なる変換が提供されていないか調べる
                    else
                    {
                        conversionsStack.Push(Tuple.Create(conversion, nextType));

                        cType = nextType;

                        //変換元の型が配列型である場合には変換元の型は確定である
                        if (cType.IsArray)
                            break;
                    }
                }

                //変換元の型が配列型である場合には再帰して変換前の値を取得する
                if (cType.IsArray)
                    co = MapValue(o, cType, conversions);
                //そうでない場合にはそのまま変換前の値を取得する
                else
                    co = GetValue(o, cType, conversions);

                //変換前の値を変換の系列で変換したものを返す
                return conversionsStack.Select((conv) => conv.Item1.Map<Conversion, Func<object, object>>((elem) => (oc) => elem.Converter(oc, conv.Item2)).GetOrDefault(_.Identity<object>())).Aggregate(co, (acc, elem) => elem(acc));
            }
        }

        //JSON形式のオブジェクトを与えられた型の実体に変換し、返す
        private static object MapObject(Dictionary<string, object> osDict, Type type, IEnumerable<Conversion> conversions)
        {
            //与えられた型の実体
            object entity = null;
            try
            {
                //与えられた型の実体を作成する
                entity = Activator.CreateInstance(type);
            }
            //引数なしの構築子がない型である場合には例外を投げる
            catch (MissingMethodException)
            {
                throw new NotSupportedException("only supported type with no parameter constructer");
            }

            //JsonProperty属性が付与されている、非公開であるものも含む全てのプロパティを取得する
            foreach (var attr in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .SelectMany((elem) => (JsonPropertyAttribute[])elem.GetCustomAttributes(typeof(JsonPropertyAttribute), false), (elem, attr) => new { PropertyInfo = elem, Name = attr.Name.GetOrDefault(elem.Name) }))
            {
                //JSON形式のオブジェクトが対応する名称のオブジェクトを含まない場合には例外を投げる
                if (!osDict.Keys.Contains(attr.Name))
                    throw new NotFoundException("not found json key", attr.Name);

                //プロパティに対応する名称のオブジェクトを変換した結果を設定する
                attr.PropertyInfo.SetValue(entity, MapValue(osDict[attr.Name], attr.PropertyInfo.PropertyType, conversions));
            }

            //与えられた型の実体を返す
            return entity;
        }

        //JSON形式のオブジェクトを与えられた型の実体に変換し、返す
        public static T Map<T>(Dictionary<string, object> osDict) where T : class
        {
            return MapObject(osDict, typeof(T), new Conversion[] { }) as T;
        }

        //JSON形式のオブジェクトを与えられた型の実体に変換し、返す
        public static T Map<T>(Dictionary<string, object> osDict, IEnumerable<Conversion> conversions) where T : class
        {
            return MapObject(osDict, typeof(T), conversions) as T;
        }
    }

    //変換クラスを提供するクラス
    public static class JsonMapperConversions
    {
        static JsonMapperConversions()
        {
            //随意型のための変換クラスの実体を作成する
            OptionConversion = new Conversion(
                //引数として受け取った変換先の型がこの変換クラスで対応している型であるかを返す関数
                //Option<T>型である場合には変換可能である
                _canConvert: (type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOption<>),
                //引数として変換先の型を受け取り、変換元の型を返す関数
                _typeConverter: (type) =>
                {
                    try
                    {
                        //Option<T>型からNullable<T>型に変換する
                        //値型の場合には成功するが、参照型の場合には失敗する（ArgumentExceptionが発生する）
                        return typeof(Nullable<>).MakeGenericType(type.GetGenericArguments());
                    }
                    catch (ArgumentException)
                    {
                        //ArgumentExceptionが発生した場合には参照型なので、
                        //Option<T>型からT型に変換する
                        return type.GetGenericArguments()[0];
                    }
                },
                //変換前の値と変換元の型を受け取り、変換後の値を返す関数
                _converter: (o, type) =>
                {
                    //変換元の型の型引数を取得する
                    //変換先の型の型引数とする
                    Type[] genericArguments = type.GetGenericArguments();

                    //変換元の型の型引数が0個である場合には変換先の型の型引数を変換元の型とする
                    if (genericArguments.Length == 0)
                        genericArguments = new Type[] { type };

                    //変換前の値がnullである場合にはNone<T>型の実体を返す
                    if (o == null)
                        return Activator.CreateInstance(typeof(None<>).MakeGenericType(genericArguments));
                    //そうでない場合にはSome<T>型の実体を返す
                    //ただし、実体自身の値として変換前の値を指定する
                    else
                        return Activator.CreateInstance(typeof(Some<>).MakeGenericType(genericArguments), o);
                }
            );

            //日時型のための変換クラスの実体を作成する
            DateTimeConversion = new Conversion(
                //引数として受け取った変換先の型がこの変換クラスで対応している型であるかを返す関数
                //DateTime型である場合には変換可能である
                _canConvert: (type) => type == typeof(DateTime),
                //引数として変換先の型を受け取り、変換元の型を返す関数
                //string型を返す
                _typeConverter: (type) => typeof(string),
                //変換前の値と変換元の型を受け取り、変換後の値を返す関数
                //文字列を日時型に変換し、返す
                _converter: (o, type) => DateTime.Parse((string)o)
            );

            //ヌル許容日時型のための変換クラスの実体を作成する
            DateTimeNullableConversion = new Conversion(
                //引数として受け取った変換先の型がこの変換クラスで対応している型であるかを返す関数
                //DateTime型である場合には変換可能である
                _canConvert: (type) => type == typeof(DateTime?),
                //引数として変換先の型を受け取り、変換元の型を返す関数
                //string型を返す
                _typeConverter: (type) => typeof(string),
                //変換前の値と変換元の型を受け取り、変換後の値を返す関数
                //文字列を日時型に変換し、返す
                _converter: (o, type) => o == null ? null : (DateTime?)DateTime.Parse((string)o)
            );

            //リスト型のための変換クラスの実体を作成する
            ListConversion = new Conversion(
                //引数として受け取った変換先の型がこの変換クラスで対応している型であるかを返す関数
                //List<T>型である場合には変換可能である
                _canConvert: (type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>),
                //引数として変換先の型を受け取り、変換元の型を返す関数
                _typeConverter: (type) =>
                {
                    //List<T>型からT[]型に変換する
                    return type.GetGenericArguments()[0].MakeArrayType();
                },
                //変換前の値と変換元の型を受け取り、変換後の値を返す関数
                _converter: (o, type) =>
                {
                    //変換元の型の要素の型を取得する
                    //変換先の型の型引数とする
                    Type[] genericArguments = new Type[] { type.GetElementType() };

                    //変換前の値がnullである場合にはnullを返す
                    if (o == null)
                        return null;
                    //そうでない場合にはList<T>型の実体を返す
                    //ただし、実体の要素として変換前の配列の要素を指定する
                    else
                        return Activator.CreateInstance(typeof(List<>).MakeGenericType(genericArguments), o);
                }
            );
        }

        //随意型のための変換クラスの実体
        public static readonly Conversion OptionConversion;
        //日時型のための変換クラスの実体
        public static readonly Conversion DateTimeConversion;
        //ヌル許容日時型のための変換クラスの実体
        public static readonly Conversion DateTimeNullableConversion;
        //リスト型のための変換クラスの実体
        public static readonly Conversion ListConversion;
    }
}