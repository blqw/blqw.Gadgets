using blqw.Gadgets.DatabaseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace blqw.Gadgets
{
    public static class DbConnectionExtensions
    {
        private static readonly object _locked = new object();
        private static Dictionary<string, Func<string, string>> ArgsPlaceholderHandlers
            = new Dictionary<string, Func<string, string>>
            {
                ["default"] = x => x,
                ["SQLiteConnection"] = x => "@" + x,
                ["SqlConnection"] = x => "@" + x,
                ["SqlCeConnection"] = x => "@" + x,
                ["NpgsqlConnection"] = x => "@" + x,
                ["OracleConnection"] = x => ":" + x,
                ["MySqlConnection"] = x => "?" + x,
                ["OleIDbConnection"] = x => "?",
                //["AseConnection"] = x => x,
            };

        public static bool OpenIfClosed(this IDbConnection conn)
        {
            if (conn == null)
            {
                return false;
            }
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
                return true;
            }
            return false;
        }

        public static void SafeClose(this IDbConnection conn)
        {
            try
            {
                if (conn?.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        public static void RegisterArgsPlaceholder(this IDbConnection conn, Func<string, string> placeholder)
        {
            if (conn is null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            lock (_locked)
            {
                ArgsPlaceholderHandlers = new Dictionary<string, Func<string, string>>(ArgsPlaceholderHandlers)
                {
                    [conn.GetType().Name] = placeholder ?? (x => x),
                };
            }
        }


        public static Func<string, string> GetParameterNameHandler(this IDbConnection conn) =>
            ArgsPlaceholderHandlers.TryGetValue(conn.GetType().Name, out var handler)
                ? handler : ArgsPlaceholderHandlers["default"] ?? (x => x);

        public static T Execute<T>(this IDbConnection conn, Func<T> func)
        {
            if (conn is null || func is null)
            {
                throw new ArgumentNullException(func is null ? nameof(func) : nameof(conn));
            }

            using (new CloseableValue(conn))
            {
                return func();
            }
        }

        public static void Execute(this IDbConnection conn, Action action)
        {
            if (conn is null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (action is null)
            {
                return;
            }

            using (new CloseableValue(conn))
            {
                action();
            }
        }

        public static void Transaction(this IDbConnection conn, Action action)
        {
            using (new CloseableValue(conn))
            using (var tran = conn.BeginTransaction())
            {
                action();
                tran.Commit();
            }
        }

        public static T Transaction<T>(this IDbConnection conn, Func<T> func)
        {
            using (new CloseableValue(conn))
            using (var tran = conn.BeginTransaction())
            {
                var result = func();
                tran.Commit();
                return result;
            }
        }

        public static IDbCommand CreateCommand(this IDbConnection conn, FormattableString fsql)
        {
            if (conn == null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (fsql == null || string.IsNullOrWhiteSpace(fsql.Format))
            {
                throw new ArgumentNullException(nameof(fsql));
            }
            var (sql, arguments) = SQLParser.Parse(fsql);
            var cmd = conn.CreateCommand();
            if (arguments.Length == 0)
            {
                cmd.CommandText = sql;
                return cmd;
            }

            var placeholder = conn.GetParameterNameHandler();
            var placeholders = new object[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var p = cmd.AddParameter(arguments[i]);

                if (string.IsNullOrEmpty(p.ParameterName))
                {
                    p.ParameterName = "p" + i.ToString();
                }
                placeholders[i] = placeholder(p.ParameterName);
            }

            cmd.CommandText = string.Format(sql, placeholders);
            return cmd;
        }

        public static int NonQuery(this IDbConnection conn, FormattableString sql) =>
            conn.CreateCommand(sql).NonQuery();

        public static List<T> QueryList<T>(this IDbConnection conn, FormattableString sql)
            where T : new() => conn.CreateCommand(sql).QueryList<T>();

        public static T QueryFirst<T>(this IDbConnection conn, FormattableString sql)
            where T : new() => conn.CreateCommand(sql).QueryFirst<T>();

        public static T QueryScalar<T>(this IDbConnection conn, FormattableString sql, T defaultValue) =>
            conn.CreateCommand(sql).QueryScalar(defaultValue);

        public static DataTable QueryDataTable(this IDbConnection conn, FormattableString sql) =>
            conn.CreateCommand(sql).QueryDataTable();

        public static List<dynamic> QueryRecords(this IDbConnection conn, FormattableString sql) =>
            conn.CreateCommand(sql).QueryRecords();

        public static dynamic QueryFirstRecord(this IDbConnection conn, FormattableString sql) =>
            conn.CreateCommand(sql).QueryFirstRecord();

        public static DataRow QueryFirstRow(this IDbConnection conn, FormattableString sql) =>
            conn.CreateCommand(sql).QueryFirstRow();

    }
}
