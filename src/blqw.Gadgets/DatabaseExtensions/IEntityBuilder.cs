using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace blqw.Gadgets.DatabaseExtensions
{
    public interface IEntityBuilder<out T>
    {
        T ToSingle(IDataRecord record);

        IEnumerable<T> ToMultiple(IDataReader reader);

    }
}
