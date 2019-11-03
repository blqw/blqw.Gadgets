using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 动态类型: 原子值
    /// </summary>
    [DebuggerDisplay("{Value.GetType().Name} : {Value}")]
    internal class DynamicAtom : DynamicObject, IConvertible
    {
        public static readonly DynamicAtom NULL = new DynamicAtom(null);
        public static readonly DynamicAtom EMPTY = new DynamicAtom("");
        public static readonly DynamicAtom ONE = new DynamicAtom(1);
        public static readonly DynamicAtom ZERO = new DynamicAtom(0);
        public static readonly DynamicAtom TRUE = new DynamicAtom(true);
        public static readonly DynamicAtom FALSE = new DynamicAtom(false);
        public static readonly DynamicAtom NAN = new DynamicAtom(double.NaN);

        private static readonly ConcurrentDictionary<(ExpressionType, Type), Func<object, object, object>> _binaryOperations
            = new ConcurrentDictionary<(ExpressionType, Type), Func<object, object, object>>();
        private static readonly ConcurrentDictionary<(ExpressionType, Type), Func<object, object>> _unaryOperations
         = new ConcurrentDictionary<(ExpressionType, Type), Func<object, object>>();


        private readonly object _value;
        public DynamicAtom(object atom) => _value = atom;

        private static Func<object, object, object> CreateBinaryOperation((ExpressionType expressionsType, Type type) arg)
        {
            var p1 = Expression.Parameter(typeof(object));
            var p2 = Expression.Parameter(typeof(object));
            var arg1 = Expression.Convert(p1, arg.type);
            var arg2 = Expression.Convert(p2, arg.type);
            var binary = Expression.MakeBinary(arg.expressionsType, arg1, arg2);
            var result = Expression.Convert(binary, typeof(object));
            return Expression.Lambda<Func<object, object, object>>(result, p1, p2).Compile();

        }

        private static Func<object, object> CreateUnaryOperation((ExpressionType expressionsType, Type type) arg)
        {
            var p1 = Expression.Parameter(typeof(object));
            var arg1 = Expression.Convert(p1, arg.type);
            var binary = Expression.MakeUnary(arg.expressionsType, arg1, arg.type);
            var result = Expression.Convert(binary, typeof(object));
            return Expression.Lambda<Func<object, object>>(result, p1).Compile();

        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            try
            {
                if (arg is DBNull)
                {
                    arg = null;
                }
                var type = arg == null ? typeof(object) : arg.GetType();
                var value = Value.ChangeType(type);
                var handler = _binaryOperations.GetOrAdd((binder.Operation, type), CreateBinaryOperation);
                result = handler(value, arg);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                result = null;
                return false;
            }
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            try
            {
                var type = Value == null ? typeof(object) : Value.GetType();
                var handler = _unaryOperations.GetOrAdd((binder.Operation, type), CreateUnaryOperation);
                result = handler(Value);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                result = null;
                return false;
            }
        }



        public virtual object Value => _value is DBNull ? null : _value;

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = Value.ChangeType(binder.ReturnType);
            return true;
        }

        #region Equals
        public override bool Equals(object obj)
        {
            obj = obj is DynamicAtom atom ? atom.Value : obj;
            var value = Value;
            if (value.IsNull())
            {
                return obj.IsNull();
            }
            if (obj.IsNull())
            {
                return false;
            }
            try
            {
                value = value.ChangeType(obj.GetType());
                return value?.Equals(obj) ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }


        public override int GetHashCode() => Value?.GetHashCode() ?? 0;


        public static bool operator ==(DynamicAtom a, object b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(DynamicAtom a, object b) => !(a?.Equals(b) ?? b == null);
        #endregion

        public override string ToString() => Value?.ToString() ?? "";

        TypeCode IConvertible.GetTypeCode() => Value.IsNull() ? TypeCode.DBNull : Type.GetTypeCode(Value.GetType());
        bool IConvertible.ToBoolean(IFormatProvider provider) => Convert.ToBoolean(Value, provider);
        byte IConvertible.ToByte(IFormatProvider provider) => Convert.ToByte(Value, provider);
        char IConvertible.ToChar(IFormatProvider provider) => Convert.ToChar(Value, provider);
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => Convert.ToDateTime(Value, provider);
        decimal IConvertible.ToDecimal(IFormatProvider provider) => Convert.ToDecimal(Value, provider);
        double IConvertible.ToDouble(IFormatProvider provider) => Convert.ToDouble(Value, provider);
        short IConvertible.ToInt16(IFormatProvider provider) => Convert.ToInt16(Value, provider);
        int IConvertible.ToInt32(IFormatProvider provider) => Convert.ToInt32(Value, provider);
        long IConvertible.ToInt64(IFormatProvider provider) => Convert.ToInt64(Value, provider);
        sbyte IConvertible.ToSByte(IFormatProvider provider) => Convert.ToSByte(Value, provider);
        float IConvertible.ToSingle(IFormatProvider provider) => Convert.ToSingle(Value, provider);
        string IConvertible.ToString(IFormatProvider provider) => Convert.ToString(Value, provider);
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) =>
            conversionType == typeof(object) ? Value : Convert.ChangeType(Value, conversionType, provider);
        ushort IConvertible.ToUInt16(IFormatProvider provider) => Convert.ToUInt16(Value, provider);
        uint IConvertible.ToUInt32(IFormatProvider provider) => Convert.ToUInt32(Value, provider);
        ulong IConvertible.ToUInt64(IFormatProvider provider) => Convert.ToUInt64(Value, provider);
    }
}
