using blqw.Gadgets.DatabaseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace blqw.Gadgets
{
    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class DbConnectionExtensions
    {
        /// <summary>
        /// 查询列表时的默认的最大返回行数
        /// </summary>
        public const int DEFAULT_LIMIT = 100000;

        private static readonly object _locked = new object();
        // 参数占位符
        private static Dictionary<string, Func<string, string>> _argsPlaceholderHandlers
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

        /// <summary>
        /// 注册指定连接类型中, 参数的占位符写法
        /// </summary>
        public static void RegisterArgsPlaceholder(this IDbConnection conn, Func<string, string> placeholder)
        {
            if (conn is null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            lock (_locked)
            {
                _argsPlaceholderHandlers = new Dictionary<string, Func<string, string>>(_argsPlaceholderHandlers)
                {
                    [conn.GetType().Name] = placeholder ?? (x => x),
                };
            }
        }

        /// <summary>
        /// 如果数据库连接当前为关闭状态, 则打开返回 <see cref="true"/>, 否则返回 <see cref="false"/>
        /// <para> 如果 <paramref name="conn"/> 为null, 也返回 <see cref="false"/> </para>
        /// </summary>
        /// <param name="conn"></param>
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

        /// <summary>
        /// 关闭数据库连接, 不返回任何错误
        /// </summary>
        /// <param name="conn"></param>
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

        // 获取转换参数占位符的委托方法
        internal static Func<string, string> GetParameterPlaceholderHandler(this IDbConnection conn) =>
            _argsPlaceholderHandlers.TryGetValue(conn.GetType().Name, out var handler)
                ? handler : _argsPlaceholderHandlers["default"] ?? (x => x);

        private static readonly ObjectPool<DbParameterFormatProvider> _pool = new ObjectPool<DbParameterFormatProvider>(
            null,
            () => new DbParameterFormatProvider(),
            x =>
            {
                x.ClearCommand();
                return true;
            },
            64
        );

        /// <summary>
        /// 使用 sql语句(<paramref name="fsql"/>) 创建数据库执行命令(<see cref="IDbCommand"/>)
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="fsql"></param>
        /// <returns></returns>
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
            //var (sql, arguments) = SQLParser.Parse(fsql);
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            using (_pool.Get(out var formatProvider))
            {
                formatProvider.SetCommand(cmd);
                cmd.CommandText = fsql.ToString(formatProvider);
            }
            return cmd;
        }

        /// <summary>
        /// 将 <paramref name="value"/> 添加到 <paramref name="command"/>
        /// </summary>
        public static IDataParameter AddParameter(this IDbCommand command, object value)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var p = command.CreateParameter();
            p.Value = value.GetDbObject();
            command.Parameters.Add(p);
            return p;
        }

        /// <summary>
        /// 执行委托 <paramref name="func"/>
        /// <para> 如果数据库连接未打开, 则打开, 执行完毕后关闭连接 </para>
        /// <para> 如果数据库连接已打开则直接执行委托, 执行完毕后也不关闭连接 </para>
        /// </summary>
        public static T Execute<T>(this IDbConnection conn, Func<T> func)
        {
            if (conn is null || func is null)
            {
                throw new ArgumentNullException(func is null ? nameof(func) : nameof(conn));
            }

            using (new SelfClosingDbCommand(conn))
            {
                return func();
            }
        }

        /// <summary>
        /// 执行委托 <paramref name="action"/>
        /// <para> 如果数据库连接未打开, 则打开, 执行完毕后关闭连接 </para>
        /// <para> 如果数据库连接已打开则直接执行委托, 执行完毕后也不关闭连接 </para>
        /// </summary>
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

            using (new SelfClosingDbCommand(conn))
            {
                action();
            }
        }

        /// <summary>
        /// 执行事务, 除非 <paramref name="action"/> 抛出异常, 否则直接提交
        /// <para> 如果数据库连接未打开, 则打开, 执行完毕后关闭连接 </para>
        /// <para> 如果数据库连接已打开则直接执行委托, 执行完毕后也不关闭连接 </para>
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="action"></param>
        public static void Transaction(this IDbConnection conn, Action action)
        {
            using (new SelfClosingDbCommand(conn))
            using (var tran = conn.BeginTransaction())
            {
                action();
                tran.Commit();
            }
        }

        /// <summary>
        /// 执行事务, 除非 <paramref name="func"/> 抛出异常, 否则直接提交
        /// <para> 如果数据库连接未打开, 则打开, 执行完毕后关闭连接 </para>
        /// <para> 如果数据库连接已打开则直接执行委托, 执行完毕后也不关闭连接 </para>
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="action"></param>
        public static T Transaction<T>(this IDbConnection conn, Func<T> func)
        {
            using (new SelfClosingDbCommand(conn))
            using (var tran = conn.BeginTransaction())
            {
                var result = func();
                tran.Commit();
                return result;
            }
        }


        public static void ExecuteReader(this IDbConnection conn, FormattableString sql, Action<IDataReader> func)
        {
            if (conn is null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var command = conn.CreateCommand(sql);
            using (new SelfClosingDbCommand(command))
            {
                var reader = command.ExecuteReader();
                using (new SelfClosingDataReader(reader, command))
                {
                    func(reader);
                }
            }
        }

        public static T ExecuteReader<T>(this IDbConnection conn, FormattableString sql, Func<IDataReader, T> func)
        {
            if (conn is null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var command = conn.CreateCommand(sql);
            using (new SelfClosingDbCommand(command))
            {
                var reader = command.ExecuteReader();
                using (new SelfClosingDataReader(reader, command))
                {
                    return func(reader);
                }
            }
        }

        /// <summary>
        /// 执行查询命令返回受影响行数
        /// </summary>
        /// <returns></returns>
        public static int ExecuteNonQuery(this IDbConnection conn, FormattableString sql)
        {
            var command = conn.CreateCommand(sql);
            using (new SelfClosingDbCommand(command))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 执行sql, 返回实体类集合
        /// </summary>
        /// <param name="limit">最大返回行数</param>
        /// <returns></returns>
        public static List<T> ExecuteList<T>(this IDbConnection conn, FormattableString sql, int limit = DEFAULT_LIMIT)
           where T : new()
        {
            if (typeof(object) == typeof(T))
            {
                return (List<T>)(object)ExecuteList(conn, sql, limit);
            }
            return conn.ExecuteReader(sql, reader => EntityBuilder.GetBuilder<T>().ToMultiple(reader).Take(limit).ToList());
        }

        /// <summary>
        /// 执行sql, 返回动态对象集合
        /// </summary>
        /// <param name="limit">最大返回行数</param>
        /// <returns></returns>
        public static List<dynamic> ExecuteList(this IDbConnection conn, FormattableString sql, int limit = DEFAULT_LIMIT) => conn.ExecuteReader(sql, reader =>
        {
            var fieldCount = reader.FieldCount;
            var mapping = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < fieldCount; i++)
            {
                mapping.Add(reader.GetName(i), i);
            }
            var list = new List<dynamic>();
            for (var i = 0; i < limit && reader.Read(); i++)
            {
                var values = new object[fieldCount];
                reader.GetValues(values);
                list.Add(new DynamicRecord(mapping, values));
            }
            return list;
        });

        /// <summary>
        /// 执行sql, 返回第一行数据的实体类
        /// </summary>
        /// <returns></returns>
        public static T ExecuteFirst<T>(this IDbConnection conn, FormattableString sql)
            where T : new() =>
            conn.ExecuteReader(sql, reader => reader.Read() ? EntityBuilder.GetBuilder<T>().ToSingle(reader) : default);

        /// <summary>
        /// 执行sql, 返回第一行数据的动态对象
        /// </summary>
        /// <returns></returns>
        public static dynamic ExecuteFirst(this IDbConnection conn, FormattableString sql) => conn.ExecuteReader(sql, reader =>
        {
            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                var mapping = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < fieldCount; i++)
                {
                    mapping.Add(reader.GetName(i), i);
                }
                var values = new object[fieldCount];
                reader.GetValues(values);
                return new DynamicRecord(mapping, values);
            }
            return default;
        });

        /// <summary>
        /// 执行sql, 返回第一行第一列的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="defaultValue">如果没有查询到任何数据,则返回该值</param>
        public static T ExecuteScalar<T>(this IDbConnection conn, FormattableString sql, T defaultValue) =>
            conn.ExecuteReader(sql, reader => reader.Read() ? EntityBuilder.Read<T>(reader, 0) : defaultValue);

        /// <summary>
        /// 执行sql, 返回第一行第一列的动态对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="defaultValue">如果没有查询到任何数据,则返回该值</param>
        public static dynamic ExecuteScalarDynamic(this IDbConnection conn, FormattableString sql, object defaultValue) =>
            conn.ExecuteReader(sql, reader => reader.Read() ? (dynamic)new DynamicAtom(reader.GetValue(0)) : (dynamic)new DynamicAtom(defaultValue));

        /// <summary>
        /// 执行sql, 返回 <see cref="DataTable"/>
        /// </summary>
        /// <param name="limit">最大返回行数</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(this IDbConnection conn, FormattableString sql, int limit = DEFAULT_LIMIT) => conn.ExecuteReader(sql, reader =>
        {
            var table = new DataTable();
            var fieldCount = reader.FieldCount;
            for (var i = 0; i < fieldCount; i++)
            {
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            }
            var values = new object[fieldCount];
            for (var i = 0; i < limit && reader.Read(); i++)
            {
                reader.GetValues(values);
                table.LoadDataRow(values, LoadOption.OverwriteChanges); // TODO: 我想知道1和3的区别
            }
            return table;
        });

        public static DataRow ExecuteDataRow(this IDbConnection conn, FormattableString sql) => conn.ExecuteReader(sql, reader =>
        {
            var values = new object[reader.FieldCount];
            var table = new DataTable();
            reader.GetValues(values);
            return table.LoadDataRow(values, LoadOption.OverwriteChanges);
        });

    }
}
