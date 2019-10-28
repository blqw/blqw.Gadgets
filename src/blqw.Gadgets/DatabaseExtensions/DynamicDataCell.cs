using System;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    public class DynamicDataCell : DynamicAtom
    {
        private readonly DataRow _row;
        private readonly DataColumn _column;

        public DynamicDataCell(DataRow row, DataColumn column)
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
