using System.Collections.Generic;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    public interface IEntityBuilder<out T>
    {
        T ToSingle(IDataRecord record);

        IEnumerable<T> ToMultiple(IDataReader reader);
    }
}
