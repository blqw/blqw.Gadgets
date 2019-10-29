using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace blqw.Gadgets.DatabaseExtensions
{
    internal static class TypeExtensions
    {
        public static bool IsNull(this object obj) => obj is null || obj is DBNull;

        public static T ChangeType<T>(this object value, T defaultValue)
        {
            if (value is null || value is DBNull)
            {
                return defaultValue;
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static object ChangeType(this object value, Type type)
        {
            if (value is null || value is DBNull)
            {
                return Convert.ChangeType(null, type);
            }
            if (type == typeof(object))
            {
                return value;
            }
            return Convert.ChangeType(value, type);
        }


        public static T ToEntity<T>(this IDataRecord record)
            where T : new()
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            if (record is IDataReader reader && reader.IsClosed)
            {
                throw new ArgumentException("IDataReader已经关闭", nameof(record));
            }
            var builder = EntityBuilder.BuildOrGet<T>();
            return builder.ToSingle(record);
        }

        public static List<T> ToEntities<T>(this IDataReader reader)
            where T : new()
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (reader.IsClosed)
            {
                throw new ArgumentException("IDataReader已经关闭", nameof(reader));
            }
            var builder = EntityBuilder.BuildOrGet<T>();
            return builder.ToMultiple(reader).ToList();
        }

        public static bool IsAtom(this object value) =>
            value.GetType().IsPrimitive || value is string || value is Guid || value is DateTime || value is TimeSpan;

        public static object GetDbObject(this object value)
        {
            if (value is null || value is DBNull)
            {
                return null;
            }
            if (value.IsAtom())
            {
                return value;
            }
            if (value is IConvertible convertible)
            {
                value = convertible.ToType(typeof(object), null);
            }
            return value;
        }
    }
}
