using System;
using System.Collections.Generic;
using System.Dynamic;


#pragma warning disable CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
#pragma warning disable CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 动态类型: 数据记录
    /// </summary>
    internal class DynamicRecord : DynamicAtom
    {
        private readonly Dictionary<string, int> _indexer;
        private readonly object[] _record;

        public DynamicRecord(Dictionary<string, int> indexer, object[] record)
            : base(record)
        {
            _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            _record = record ?? throw new ArgumentNullException(nameof(record));
        }

        protected DynamicRecord(object value)
            : base(value)
        {

        }

        public override IEnumerable<string> GetDynamicMemberNames() => _indexer.Keys;

        protected virtual DynamicAtom this[int columnIndex] =>
            columnIndex < 0 || columnIndex >= _record.Length ? NULL : new DynamicAtom(_record[columnIndex]);

        protected virtual DynamicAtom this[string columnName] =>
            _indexer.TryGetValue(columnName, out var index) ? new DynamicAtom(_record[index]) : NULL;


        protected DynamicAtom this[object[] indexes]
        {
            get
            {
                if (indexes.Length != 1)
                {
                    return NULL;
                }
                switch (indexes[0])
                {
                    case int index:
                        return this[index];
                    case string columnName:
                        return this[columnName];
                    default:
                        return NULL;
                }
            }
        }


        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = this[indexes];
            return true;
        }


        #region Equals
        public static bool operator ==(DynamicRecord a, object b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(DynamicRecord a, object b) => !(a?.Equals(b) ?? b == null);
        #endregion

    }
}
