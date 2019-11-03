using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 实体类构造器
    /// </summary>
    internal static class EntityBuilder
    {
        // IEntityBuilder<T> 缓存
        private static readonly ConcurrentDictionary<Type, IEntityBuilder<object>> _cachedBuilder = new ConcurrentDictionary<Type, IEntityBuilder<object>>();

        /// <summary>
        /// 获取指定实体类型的构造器
        /// </summary>
        public static IEntityBuilder<T> GetBuilder<T>() => (IEntityBuilder<T>)_cachedBuilder.GetOrAdd(typeof(T), t => CreateBuilder<T>());


        private static IEntityBuilder<object> CreateBuilder<T>()
        {
            var ctor = typeof(T).GetConstructor(FixedValue.TypesIDataRecord);
            if (ctor != null)
            {
                return (IEntityBuilder<object>)new ConstructorBuilder<T>(ctor);
            }
            foreach (var c in typeof(T).GetConstructors())
            {
                var p = c.GetParameters();
                if (p.Length == 1 && typeof(IDataRecord).IsAssignableFrom(p[0].ParameterType))
                {
                    return (IEntityBuilder<object>)new ConstructorBuilder<T>(c);
                }
            }
            return (IEntityBuilder<object>)new StandardBuilder<T>();
        }

        private class ConstructorBuilder<T> : IEntityBuilder<T>
        {
            private readonly Func<IDataRecord, T> _constructor;
            public ConstructorBuilder(ConstructorInfo constructor)
            {
                var p1 = Expression.Parameter(typeof(IDataRecord));
                var @new = Expression.New(constructor, p1);
                var lambda = Expression.Lambda(@new, p1);
                _constructor = (Func<IDataRecord, T>)lambda.Compile();
            }
            public IEnumerable<T> ToMultiple(IDataReader reader)
            {
                while (reader.Read())
                {
                    yield return _constructor(reader);
                }
            }

            public T ToSingle(IDataRecord record) => _constructor(record);
        }

        private class StandardBuilder<T> : IEntityBuilder<T>
        {
            public StandardBuilder()
            {
                _properties = typeof(T).GetProperties()
                    .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                    .Select(p => new Property<T>(p))
                    .ToArray();
                _propertyCount = _properties.Length;
            }


            private readonly Property<T>[] _properties;
            private readonly int _propertyCount;

            private Property<T> GetProperty(string columnName)
            {
                for (var j = 0; j < _propertyCount; j++)
                {
                    var p = _properties[j];
                    if (p.StrictMatch(columnName))
                    {
                        return p;
                    }
                }
                for (var j = 0; j < _propertyCount; j++)
                {
                    var p = _properties[j];
                    if (p.FuzzyMatch(columnName))
                    {
                        return p;
                    }
                }
                return null;
            }

            public T ToSingle(IDataRecord record)
            {
                var entity = Activator.CreateInstance<T>();
                for (var i = record.FieldCount - 1; i >= 0; i--)
                {
                    var columnName = record.GetName(i);
                    var p = GetProperty(columnName);
                    if (p != null)
                    {
                        if (p.IsNullable && record.IsDBNull(i))
                        {
                            p.SetNull(entity);
                        }
                        else
                        {
                            p.Fill(entity, record, i);
                        }
                    }
                }
                return entity;
            }

            public IEnumerable<T> ToMultiple(IDataReader reader)
            {
                if (!reader.Read())
                {
                    yield break;
                }
                var props = new List<(int, Property<T>)>();
                for (var i = reader.FieldCount - 1; i >= 0; i--)
                {
                    var columnName = reader.GetName(i);
                    var p = GetProperty(columnName);
                    if (p != null)
                    {
                        props.Add((i, p));
                    }
                }
                var propCount = props.Count;
                do
                {
                    var entity = Activator.CreateInstance<T>();
                    for (var j = 0; j < propCount; j++)
                    {
                        var (i, p) = props[j];
                        if (p.IsNullable && reader.IsDBNull(i))
                        {
                            p.SetNull(entity);
                        }
                        else
                        {
                            p.Fill(entity, reader, i);
                        }
                    }
                    yield return entity;
                } while (reader.Read());
            }
        }

        private class Property<T>
        {
            private readonly PropertyInfo _property;

            public Property(PropertyInfo property)
            {
                var reader = DataRecordReader(property.PropertyType);
                var p1 = Expression.Parameter(typeof(T));
                var p2 = Expression.Parameter(typeof(IDataRecord));
                var p3 = Expression.Parameter(typeof(int));
                Expression call = Expression.Invoke(reader, p2, p3);
                call = Expression.Call(p1, property.SetMethod, call);
                Fill = (Action<T, IDataRecord, int>)Expression.Lambda(call, p1, p2, p3).Compile();
                _property = property;
                IsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                if (IsNullable)
                {
                    p1 = Expression.Parameter(typeof(T));
                    var nul = Expression.Convert(Expression.Constant(null), property.PropertyType);
                    call = Expression.Call(p1, property.SetMethod, nul);
                    SetNull = (Action<T>)Expression.Lambda(call, p1).Compile();
                }
            }

            public bool IsNullable { get; }

            public bool StrictMatch(string columnName) => _property.Name == columnName;

            public bool FuzzyMatch(string columnName) => _property.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase);

            public Action<T, IDataRecord, int> Fill { get; }

            public Action<T> SetNull { get; }
        }

        // Func<IDataRecord, int, T> 缓存
        private static readonly ConcurrentDictionary<Type, object> _cachedReader = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// 读取 <paramref name="record"/> 中指定列的值
        /// </summary>
        public static T Read<T>(this IDataRecord record, int columnIndex)
        {
            var reader = (Func<IDataRecord, int, T>)_cachedReader.GetOrAdd(typeof(T), t => (Func<IDataRecord, int, T>)DataRecordReader(t).Compile());
            return reader(record, columnIndex);
        }

        private static LambdaExpression DataRecordReader(Type type)
        {
            var realType = Nullable.GetUnderlyingType(type) ?? type;
            switch (Type.GetTypeCode(realType))
            {
                case TypeCode.Boolean:
                    return CreateDataRecordGetMethod("GetBoolean", type);
                case TypeCode.Byte:
                    return CreateDataRecordGetMethod("GetByte", type);
                case TypeCode.Char:
                    return CreateDataRecordGetMethod("GetChar", type);
                case TypeCode.DateTime:
                    return CreateDataRecordGetMethod("GetDateTime", type);
                case TypeCode.DBNull:
                    return null;
                case TypeCode.Decimal:
                    return CreateDataRecordGetMethod("GetDecimal", type);
                case TypeCode.Double:
                    return CreateDataRecordGetMethod("GetDouble", type);
                case TypeCode.Empty:
                    return null;
                case TypeCode.Int16:
                    return CreateDataRecordGetMethod("GetInt16", type);
                case TypeCode.Int32:
                    return CreateDataRecordGetMethod("GetInt32", type);
                case TypeCode.Int64:
                    return CreateDataRecordGetMethod("GetInt64", type);
                case TypeCode.SByte:
                    return CreateDataRecordGetMethod("GetInt32", type);
                case TypeCode.Single:
                    return CreateDataRecordGetMethod("GetInt32", type);
                case TypeCode.String:
                    return CreateDataRecordGetMethod("GetString", type);
                case TypeCode.UInt16:
                    return CreateDataRecordGetMethod("GetInt32", type);
                case TypeCode.UInt32:
                    return CreateDataRecordGetMethod("GetInt64", type);
                case TypeCode.UInt64:
                    break;
                default:
                    if (realType == typeof(Guid))
                    {
                        return CreateDataRecordGetMethod("GetGuid", type);
                    }
                    break;
            }

            var getValue = typeof(IDataRecord).GetMethod("GetValue", FixedValue.TypesInt32);
            var p1 = Expression.Parameter(typeof(IDataRecord));
            var p2 = Expression.Parameter(typeof(int));
            var call = Expression.Call(p1, getValue, p2);
            var changeType = typeof(Convert).GetMethod("ChangeType", FixedValue.TypesObjectType);
            Expression ret = Expression.Call(changeType, call, Expression.Constant(realType));
            if (realType != type)
            {
                ret = Expression.Convert(call, type);
            }
            return Expression.Lambda(ret, p1, p2);
        }

        private static readonly Type[] _argsTypesInt = { typeof(int) };

        private static LambdaExpression CreateDataRecordGetMethod(string methodName, Type returnType)
        {
            var method = typeof(IDataRecord).GetMethod(methodName, FixedValue.TypesInt32);
            var p1 = Expression.Parameter(typeof(IDataRecord));
            var p2 = Expression.Parameter(typeof(int));
            Expression ret = Expression.Call(p1, method, p2);
            if (returnType != method.ReturnType)
            {
                ret = Expression.Convert(ret, returnType);
            }
            return Expression.Lambda(ret, p1, p2);
        }

    }
}