using System;
using System.Data;

#pragma warning disable CS0660 // 类型定义运算符 == 或运算符 !=，但不重写 Object.Equals(object o)
#pragma warning disable CS0661 // 类型定义运算符 == 或运算符 !=，但不重写 Object.GetHashCode()
namespace blqw.Gadgets.DatabaseExtensions
{

    /// <summary>
    /// 动态类型: 单元格数据
    /// </summary>
    internal class DynamicDataCell : DynamicAtom
    {
        private readonly DataRow _row;
        private readonly DataColumn _column;

        public DynamicDataCell(DataRow row, DataColumn column)
            : base(null)
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            _column = column ?? throw new ArgumentNullException(nameof(column));
            if (column.Table != _row.Table)
            {
                throw new ArgumentException($"{nameof(column)}不属于${nameof(row)}");
            }
        }

        public override object Value
        {
            get
            {
                var value = _row[_column];
                return value is DBNull ? null : value;
            }
        }

        #region Equals
        public static bool operator ==(DynamicDataCell a, object b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(DynamicDataCell a, object b) => !(a?.Equals(b) ?? b == null);
        #endregion
    }
}