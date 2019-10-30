using System.Collections.Generic;
using System.Data;
using System.Linq;

#pragma warning disable CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
#pragma warning disable CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 动态类型: 行数据
    /// </summary>
    internal class DynamicDataRow : DynamicRecord
    {
        private readonly DataRow _row;

        public DynamicDataRow(DataRow row) : base(row) => _row = row;

        public override IEnumerable<string> GetDynamicMemberNames() => _row.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName);

        protected override DynamicAtom this[int columnIndex]
        {
            get
            {
                var col = _row.Table.Columns[columnIndex];
                if (col == null)
                {
                    return NULL;
                }
                return new DynamicDataCell(_row, col);
            }
        }

        protected override DynamicAtom this[string columnName]
        {
            get
            {
                var col = _row.Table.Columns[columnName];
                if (col == null)
                {
                    return NULL;
                }
                return new DynamicDataCell(_row, col);
            }
        }

        #region Equals
        public static bool operator ==(DynamicDataRow a, object b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(DynamicDataRow a, object b) => !(a?.Equals(b) ?? b == null);
        #endregion
    }
}
