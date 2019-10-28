using System;
using System.Data;

namespace blqw.Gadgets.DatabaseExtensions
{
    /// <summary>
    /// 可关闭对象
    /// </summary>
    public struct CloseableValue : IDisposable
    {
        private IDbConnection _connection;
        private IDbCommand _command;
        private IDataReader _reader;

        public CloseableValue(IDbConnection connection)
        {
            _connection = connection.OpenIfClosed() ? connection : null;
            _command = null;
            _reader = null;
        }

        public CloseableValue(IDbCommand command)
            : this(command?.Connection) =>
            _command = command;

        public CloseableValue(IDataReader reader)
        {
            _connection = null;
            _command = null;
            _reader = reader;
        }

        public void Dispose()
        {
            _reader?.Close();
            _reader?.Dispose();
            _reader = null;
            //_command?.Cancel(); // TOOD: 会产生额外的一次数据库操作, 可能会引起其他问题, 但如果datareader未读完直接dispose,将会自动读取剩下的数据后才释放
            _command?.Dispose();
            _command = null;
            _connection?.Dispose();
            _connection = null;
        }
    }
}
