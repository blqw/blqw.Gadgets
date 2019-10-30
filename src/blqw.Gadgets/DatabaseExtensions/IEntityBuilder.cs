using System.Collections.Generic;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 实体类构造器接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityBuilder<out T>
    {
        /// <summary>
        /// 循环读取 <paramref name="reader"/> 并转换为 实体类型(<typeparamref name="T"/>) 依次返回
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        IEnumerable<T> ToMultiple(IDataReader reader);

        /// <summary>
        /// 将一条数据记录(<paramref name="record"/>)转换为实体类型(<typeparamref name="T"/>)返回
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        T ToSingle(IDataRecord record);
    }
}
