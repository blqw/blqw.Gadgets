using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        public static T ToEntity<T>(this DataRow row)
            where T : new()
        {
            if (row is null)
            {
                return default;
            }

            var props = typeof(T).GetProperties();
            var entity = new T();

            foreach (var prop in props)
            {
                if (prop.CanWrite && !prop.SetMethod.IsStatic && prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        var value = row[prop.Name].ChangeType(prop.PropertyType);
                        prop.SetValue(entity, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            return entity;
        }




        public static T ToEntity<T>(this IDataRecord record)
            where T : new()
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            //var factory = EntityBuilder.Get<T>();
            //return factory.CreateInstance(record);
            return default;
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
