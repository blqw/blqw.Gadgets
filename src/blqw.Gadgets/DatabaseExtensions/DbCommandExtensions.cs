using blqw.Gadgets.DatabaseExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace blqw.Gadgets
{
    /// <summary>
    /// <see cref="IDbCommand"/>扩展方法
    /// </summary>
    public static class DbCommandExtensions
    {
        /// <summary>
        /// 将 <see cref="IDataReader"/> 转为 <see cref="DataTable"/>
        /// </summary>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="thenclose">完成后是否关闭<see cref="IDataReader"/></param>
        /// <returns></returns>
        private static DataTable ToDataTable(this IDataReader reader, bool thenclose)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            using (new CloseableValue(thenclose ? reader : null))
            {
                var table = new DataTable();
                table.Load(reader, LoadOption.Upsert);
                return table;
            }
        }

        public static void Query(this IDbCommand command, Action action)
        {
            using (new CloseableValue(command))
            {
                action();
            }
        }

        public static T Query<T>(this IDbCommand command, Func<T> func)
        {
            using (new CloseableValue(command))
            {
                return func();
            }
        }

        public static int NonQuery(this IDbCommand command)
        {
            using (new CloseableValue(command))
            {
                return command.ExecuteNonQuery();
            }
        }

        public static List<T> QueryList<T>(this IDbCommand command)
            where T : new()
        {
            using (var reader = command.ExecuteReader())
            {
                return null;
            }
        }


        public static T QueryFirst<T>(this IDbCommand command)
            where T : new()
        {
            return command.QueryFirstRow().ToEntity<T>();
        }

        public static T QueryScalar<T>(this IDbCommand command, T defaultValue)
        {
            using (new CloseableValue(command))
            {
                return command.ExecuteScalar().ChangeType(defaultValue);
            }
        }

        public static DataTable QueryDataTable(this IDbCommand command)
        {
            using (new CloseableValue(command))
            {
                return command.ExecuteReader().ToDataTable(true);
            }
        }

        public static DataRow QueryFirstRow(this IDbCommand command)
        {
            return command.QueryDataTable().Rows.Cast<DataRow>().FirstOrDefault();
        }

        public static List<dynamic> QueryRecords(this IDbCommand command)
        {
            using (var table = command.QueryDataTable())
            {
                return table.Rows.Cast<DataRow>().Select(x => (dynamic)new DynamicDataRow(x)).ToList();
            }
        }

        public static dynamic QueryFirstRecord(this IDbCommand command)
        {
            return new DynamicDataRow(command.QueryFirstRow());
        }

        public static IDataParameter AddParameter(this IDbCommand command, object value)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var p = command.CreateParameter();
            p.Value = value.GetDbObject() ?? DBNull.Value;
            command.Parameters.Add(p);
            return p;
        }
    }
}
