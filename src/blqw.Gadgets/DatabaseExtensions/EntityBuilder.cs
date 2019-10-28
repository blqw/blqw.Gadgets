using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace blqw.Gadgets.DatabaseExtensions
{
    public class EntityBuilder
    {
        private static readonly ConcurrentDictionary<Type, object> _cached = new ConcurrentDictionary<Type, object>();

        public static IEntityBuilder<T> BuildOrGet<T>() => (IEntityBuilder<T>)_cached.GetOrAdd(typeof(T), t => new EntityBuilderImpl<T>());


        class EntityBuilderImpl<T> : IEntityBuilder<T>
        {
            public EntityBuilderImpl()
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
                while (reader.Read())
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
                }
            }
        }

        class Property<T>
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



        private static LambdaExpression DataRecordReader(Type type)
        {
            switch (Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type))
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
                default:
                    break;
            }

            var getValue = typeof(IDataRecord).GetMethod("GetValue", _argsTypesInt);
            var p1 = Expression.Parameter(typeof(IDataRecord));
            var p2 = Expression.Parameter(typeof(int));
            var call = Expression.Call(p1, getValue, p2);
            var changeType = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
            call = Expression.Call(changeType, call, Expression.Constant(type));
            var ret = Expression.Convert(call, typeof(object));
            return Expression.Lambda(ret, p1, p2);
        }
        private static Type[] _argsTypesInt = { typeof(int) };
        private static LambdaExpression CreateDataRecordGetMethod(string methodName, Type returnType)
        {
            var method = typeof(IDataRecord).GetMethod(methodName, _argsTypesInt);
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