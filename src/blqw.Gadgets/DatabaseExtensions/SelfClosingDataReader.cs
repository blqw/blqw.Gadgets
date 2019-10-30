using System;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 自动关闭的 <see cref="IDataReader"/> 结构
    /// </summary>
    internal struct SelfClosingDataReader : IDisposable
    {
        private IDataReader _reader;
        private IDbCommand _command;

        public SelfClosingDataReader(IDataReader reader, IDbCommand command)
        {
            _reader = reader;
            _command = command;
        }

        public void Dispose()
        {
            if (_reader.NextResult())
            {
                // 如果datareader未读完直接dispose,将会自动读取剩下的数据后才释放
                _command?.Cancel();
            }
            _reader?.Close();
            _reader?.Dispose();
            _reader = null;
        }
    }
}
