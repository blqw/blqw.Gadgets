using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 关于 <see cref="Type"/> 的扩展方法
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// 判断<paramref name="value"/>是否为<see cref="null"/>或<see cref="DBNull"/>
        /// </summary>
        public static bool IsNull(this object value) => value is null || value is DBNull;

        /// <summary>
        /// 类型转换
        /// </summary>
        public static object ChangeType(this object value, Type type)
        {
            if (value.IsNull())
            {
                return Convert.ChangeType(null, type);
            }
            if (type == typeof(object))
            {
                return value;
            }
            return Convert.ChangeType(value, type);
        }

        /// <summary>
        /// 判断<paramref name="value"/>是否为原子类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsAtom(this object value) =>
            value.GetType().IsPrimitive || value is string || value is Guid || value is DateTime || value is TimeSpan;

        /// <summary>
        /// 获取 <paramref name="value"/> 对应的数据库可用的类型值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object GetDbObject(this object value)
        {
            if (value.IsNull())
            {
                return DBNull.Value;
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
