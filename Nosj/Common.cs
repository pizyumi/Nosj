using System;
using System.Collections.Generic;
using System.Linq;

namespace Nosj
{
    //共通の拡張関数
    public static class CommonExtension
    {
        //列挙の中で述語を充足する最初の要素を随意型の実体として返す
        //ただし、そのような要素が存在しない場合にはNone<T>型の実体を返す
        public static IOption<T> FirstOption<T>(this IEnumerable<T> self, Func<T, bool> predicate)
        {
            //列挙の中で述語を充足する最初の要素を取得する
            T first = self.FirstOrDefault(predicate);
            //nullである場合にはNone<T>型の実体を返す
            if (first == null)
                return Option.Return<T>();
            //そうでない場合にはSome<T>型の実体を返す
            else
                return Option.Return(first);
        }
    }

    //共通の関数
    public static class _
    {
        //恒等関数
        public static Func<T, T> Identity<T>()
        {
            return (elem) => elem;
        }
    }

    //普通の随意型のためのインターフェイス
    public interface IOption<T>
    {
        //値
        T Value { get; }
        //値が存在しないか
        bool IsEmpty { get; }

        //値が存在する場合には値を返し、存在しない場合には既定値を返す
        T GetOrDefault(T def);
        //値が存在するかどうかによって別々の処理を実行する
        void Match(Action none = null, Action<T> some = null);
        //値が存在するかどうかによって別々の関数を実行し、返り値を返す
        S Match<S>(Func<S> none = null, Func<T, S> some = null);
        //現在の実体から新しい普通の随意型の実体を作成し、返す
        IOption<S> Bind<S>(Func<T, IOption<S>> f);
    }

    //メッセージ付きの随意型のためのインターフェイス
    public interface IOptionM<T> : IOption<T>
    {
        //メッセージ
        string Message { get; }
    }

    //値が存在しない普通の随意型
    public class None<T> : IOption<T>
    {
        //値
        public T Value
        {
            //存在しないので例外を投げる
            get { throw new Exception("not existed value"); }
        }

        //値が存在しないか
        public bool IsEmpty
        {
            //存在しないので真を返す
            get { return true; }
        }

        //値が存在する場合には値を返し、存在しない場合には既定値を返す
        public T GetOrDefault(T def)
        {
            //存在しないので既定値を返す
            return def;
        }

        //値が存在するかどうかによって別々の処理を実行する
        public void Match(Action none = null, Action<T> some = null)
        {
            //値が存在しない場合に実行されるよう指定された処理が渡された場合には処理を実行する
            //渡されなかった場合には何もしない
            if (none != null)
                none();
        }

        //値が存在するかどうかによって別々の関数を実行し、返り値を返す
        public S Match<S>(Func<S> none = null, Func<T, S> some = null)
        {
            //値が存在しない場合に実行されるよう指定された関数が渡された場合には関数を実行し、返り値を返す
            //渡されなかった場合には例外を投げる
            if (none != null)
                return none();
            else
                throw new Exception("none is null");
        }

        //現在の実体から新しい普通の随意型の実体を作成し、返す
        public IOption<S> Bind<S>(Func<T, IOption<S>> f)
        {
            //基になる値が存在しないので値が存在しない新しい普通の随意型を作成し、返す
            return Option.Return<S>();
        }
    }

    //値が存在する普通の随意型
    public class Some<T> : IOption<T>
    {
        //値を受け取って実体化する
        public Some(T _value)
        {
            value = _value;
        }

        //値
        private T value;
        public T Value
        {
            //存在するので値を返す
            get { return value; }
        }

        //値が存在しないか
        public bool IsEmpty
        {
            //存在するので偽を返す
            get { return false; }
        }

        //値が存在する場合には値を返し、存在しない場合には既定値を返す
        public T GetOrDefault(T def)
        {
            //存在するので値を返す
            return Value;
        }

        //値が存在するかどうかによって別々の処理を実行する
        public void Match(Action none = null, Action<T> some = null)
        {
            //値が存在する場合に実行されるよう指定された処理が渡された場合には処理を実行する
            //渡されなかった場合には何もしない
            if (some != null)
                some(Value);
        }

        //値が存在するかどうかによって別々の関数を実行し、返り値を返す
        public S Match<S>(Func<S> none = null, Func<T, S> some = null)
        {
            //値が存在する場合に実行されるよう指定された関数が渡された場合には関数を実行し、返り値を返す
            //渡されなかった場合には例外を投げる
            if (some != null)
                return some(Value);
            else
                throw new Exception("some is null");
        }

        //現在の実体から新しい普通の随意型の実体を作成し、返す
        public IOption<S> Bind<S>(Func<T, IOption<S>> f)
        {
            //基になる値が存在するので値を基に新しい普通の随意型を作成し、返す
            return f(Value);
        }
    }

    //値が存在しないメッセージ付きの随意型
    public sealed class NoneM<T> : None<T>, IOptionM<T>
    {
        //メッセージを受け取って実体化する
        public NoneM(string _message)
        {
            message = _message;
        }

        //メッセージ
        private string message;
        public string Message
        {
            get { return message; }
        }
    }

    //値が存在するメッセージ付きの随意型
    public sealed class SomeM<T> : Some<T>, IOptionM<T>
    {
        //値とメッセージを受け取って実体化する
        public SomeM(T _value, string _message) : base(_value)
        {
            message = _message;
        }

        //メッセージ
        private string message;
        public string Message
        {
            get { return message; }
        }
    }

    //随意型のための補助的な関数を格納するクラス
    public static class Option
    {
        //値を受け取って値が存在する普通の随意型を返す
        public static IOption<T> Return<T>(T value)
        {
            return new Some<T>(value);
        }

        //値が存在しない普通の随意型を返す
        public static IOption<T> Return<T>()
        {
            return new None<T>();
        }

        //値とメッセージを受け取って値が存在するメッセージ付きの随意型を返す
        public static IOptionM<T> ReturnM<T>(T value, string message)
        {
            return new SomeM<T>(value, message);
        }

        //値を受け取って値が存在するメッセージが空の文字列であるメッセージ付きの随意型を返す
        public static IOptionM<T> ReturnM<T>(T value)
        {
            return new SomeM<T>(value, string.Empty);
        }

        //メッセージを受け取って値が存在しないメッセージ付きの随意型を返す
        public static IOptionM<T> ReturnM<T>(string message)
        {
            return new NoneM<T>(message);
        }

        //値が存在しないメッセージが空の文字列であるメッセージ付きの随意型を返す
        public static IOptionM<T> ReturnM<T>()
        {
            return new NoneM<T>(string.Empty);
        }

        //普通の随意型の実体を受け取って新しい普通の随意型の実体を作成し、返す
        public static IOption<S> Bind<T, S>(IOption<T> m, Func<T, IOption<S>> f)
        {
            return m.Bind(f);
        }
    }

    //随意型に対する拡張関数
    public static class OptionExtension
    {
        //随意型用の関数合成（通常の関数合成の逆順）
        public static Func<T, IOption<R>> AndThen<T, S, R>(this Func<T, IOption<S>> f, Func<S, IOption<R>> g)
        {
            return x => f(x).Bind(g);
        }

        //任意の型の実体を随意型の実体に変換する
        public static IOption<T> ToOption<T>(this T self)
        {
            return Option.Return(self);
        }

        //随意型の実体から新しい随意型の実体を作成し、返す
        public static IOption<S> Map<T, S>(this IOption<T> self, Func<T, S> f)
        {
            return self.Bind((elem) => Option.Return(f(elem)));
        }

        //随意型の実体から新しい随意型の実体を作成し、返す
        public static IOption<S> SelectMany<T, S>(this IOption<T> self, Func<T, IOption<S>> f)
        {
            return self.Bind(f);
        }

        //随意型の実体から新しい随意型の実体を作成し、新しい随意型の実体から更に新しい随意型の実体を作成し、返す
        public static IOption<R> SelectMany<T, S, R>(this IOption<T> self, Func<T, IOption<S>> f, Func<T, S, R> g)
        {
            return self.Bind((x) => f(x).Bind((y) => g(x, y).ToOption()));
        }
    }

    //運用記録作成
    //擬人化　名前：美海（みう）　愛称：みうたそ
    public class Logger
    {
        //正式名称
        private static readonly string FormalName = "logger";
        //名前
        private static readonly string Name = "美海";
        //愛称
        private static readonly string Nickname = "みうたそ";
        //名前（ローマ字表記）
        private static readonly string NameRoma = "Miu";
        //愛称（ローマ字表記）
        private static readonly string NicknameRoma = "Miutaso";

        //既定の住所
        public static readonly string DefaultAddress = "defaultLogRepository";

        //指定された住所の運用記録保管所に接続する
        //!<未実装>
        //今は何もしない
        public bool Connect(string address)
        {
            return true;
        }

        //運用記録を発行する
        //!<未改良>
        //最終的には運用記録出力処理などは運用記録保管所の仕事にする
        public bool IssueLog(LogItem logItem)
        {
            //運用記録の種類が重大である場合の文字色
            const ConsoleColor critical = ConsoleColor.Magenta;
            //運用記録の種類が過誤である場合の文字色
            const ConsoleColor error = ConsoleColor.Red;
            //運用記録の種類が警告である場合の文字色
            const ConsoleColor warning = ConsoleColor.Yellow;
            //運用記録の種類が情報である場合の文字色
            const ConsoleColor information = ConsoleColor.Cyan;
            //運用記録の種類が詳細である場合の文字色
            const ConsoleColor verbose = ConsoleColor.White;

            //コンソールの既定の文字色
            const ConsoleColor def = ConsoleColor.Gray;

            //運用記録の種類に応じてコンソールの文字色を適切なものに変更する
            if (logItem.Type == LogType.Critical)
                Console.ForegroundColor = critical;
            else if (logItem.Type == LogType.Error)
                Console.ForegroundColor = error;
            else if (logItem.Type == LogType.Warning)
                Console.ForegroundColor = warning;
            else if (logItem.Type == LogType.Information)
                Console.ForegroundColor = information;
            else if (logItem.Type == LogType.Verbose)
                Console.ForegroundColor = verbose;
            //運用記録の種類が存在しないものである場合には警告の運用記録を出力する
            else
            {
                const string id = "6a5b9197-4f43-4fef-9697-9b1462c03ab0";
                const string text = "not existed log type";
                const LogType type = LogType.Warning;

                IssueLog(new LogItem(Guid.Parse(id), text, type, FormalName));
            }

            //運用記録の文言をコンソールに出力する
            Console.WriteLine(logItem.Text);

            //コンソールの文字色を既定のものに戻す
            Console.ForegroundColor = def;

            return true;
        }
    }

    //運用記録
    public class LogItem
    {
        public LogItem(Guid _id, string _text, LogType _type)
        {
            Id = _id;
            Text = _text;
            Type = _type;

            //時刻として現在時刻を設定する
            Datetime = DateTime.Now;
        }

        public LogItem(Guid _id, string _text, LogType _type, string _source)
        {
            Id = _id;
            Text = _text;
            Type = _type;

            Source = _source;

            //時刻として現在時刻を設定する
            Datetime = DateTime.Now;
        }

        public LogItem(Guid _id, string _text, LogType _type, Info _info)
        {
            Id = _id;
            Text = _text;
            Type = _type;

            Info = _info;

            //時刻として現在時刻を設定する
            Datetime = DateTime.Now;
        }

        public LogItem(Guid _id, string _text, LogType _type, string _source, Info _info)
        {
            Id = _id;
            Text = _text;
            Type = _type;

            Source = _source;
            Info = _info;

            //時刻として現在時刻を設定する
            Datetime = DateTime.Now;
        }

        //識別子
        public Guid Id { get; private set; }
        //文言
        public string Text { get; private set; }
        //種類
        public LogType Type { get; private set; }

        //発生源
        public string Source { get; private set; }
        //付随情報
        public Info Info { get; private set; }

        //時刻
        public DateTime Datetime { get; private set; }
    }

    //運用記録の種類
    public enum LogType
    {
        //重大
        Critical,
        //過誤
        Error,
        //警告
        Warning,
        //情報
        Information,
        //詳細
        Verbose
    }

    //情報
    //任意の情報を保持する用途で使用する
    public class Info : Dictionary<string, object> { }
}