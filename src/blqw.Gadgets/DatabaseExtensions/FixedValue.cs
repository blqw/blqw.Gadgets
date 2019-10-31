using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 一些经常用到,但不想重复实例化的值
    /// </summary>
    internal static class FixedValue
    {
        public static readonly Type[] TypesInt32 = { typeof(int) };
        public static readonly Type[] TypesIDataRecord = { typeof(IDataRecord) };
        public static readonly Type[] TypesObjectType = { typeof(object), typeof(Type) };
    }
}
