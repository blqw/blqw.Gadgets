using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace blqw.Gadgets.DatabaseExtensions
{
    public class DynamicDataRow : DynamicAtom
    {
        private readonly DataRow _row;

        public DynamicDataRow(DataRow row) => _row = row;

        public override IEnumerable<string> GetDynamicMemberNames() => _row.Table.Columns.Cast<DataColumn>().Select(x => x.ColumnName);

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var col = _row.Table.Columns[binder.Name];
            if (col == null)
            {
                result = NULL;
                return true;
            }
            result = new DynamicDataCell(_row, col);
            return true;
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            if (binder.Operation == ExpressionType.Equal)
            {
                result = Equals(_row, arg);
                return true;
            }
            else if (binder.Operation == ExpressionType.NotEqual)
            {
                result = !Equals(_row, arg);
                return true;
            }
            result = null;
            return false;
        }


        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length != 1)
            {
                result = null;
                return false;
            }

            var col = _row.Table.Columns[indexes[0] + ""];
            if (col == null)
            {
                result = NULL;
                return true;
            }
            result = new DynamicDataCell(_row, col);
            return true;
        }

        public override bool Equals(object obj)
        {
            if (_row is null)
            {
                return obj is null || obj is DBNull;
            }
            if (obj is null || obj is DBNull)
            {
                return false;
            }
            try
            {
                return _row.Equals(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public override int GetHashCode() => _row?.GetHashCode() ?? 0;

        public static bool operator ==(DynamicDataRow a, object b) => a?.Equals(b) ?? b == null;

        public static bool operator !=(DynamicDataRow a, object b) => !(a?.Equals(b) ?? b == null);

    }
}
