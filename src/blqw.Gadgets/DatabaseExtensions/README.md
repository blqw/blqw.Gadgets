# blqw.Gadgets.DatabaseExtensions
数据库拓展方法

## Demo
很简单的demo，几乎所有方法都是从`IDbConnection`类型扩展而来。
感受就跟使用原生的SQL一样
```csharp
using (var conn = new MySqlConnection("Server=*;Port=*;Database=*;Uid=*;Pwd=*;"))
{
    var list = conn.ExecuteList<Students>($"select * from students");
    var first = conn.ExecuteFirst($"select * from students where id > {13} order by id");
    Console.WriteLine($"Name:{first.Name}");
}
```

## 特性

### 支持数组参数
> 所有格式化字符串SQL都会被转为参数化执行，例如：  
`$"select * from x where id = {11}"`  
--> `select * from x where id = @p1;`  
而数组转换会被转换为:  
`$"select * from x where id in ({new [] {1,2,3,4}})"`   
--> `select * from x where id in (@p1,@p2,@p3,@p4)`

### 最大返回行数
> 默认执行任何查询最多返回10万行, 除非手动指定行数
``` csharp
/// <summary>
/// 查询列表时的默认的最大返回行数
/// </summary>
public const int DEFAULT_LIMIT = 100000;
/// <summary>
/// 执行sql, 返回实体类集合
/// </summary>
/// <param name="limit">最大返回行数</param>
/// <returns></returns>
public static List<T> ExecuteList<T>(this IDbConnection conn, FormattableString sql, int limit = DEFAULT_LIMIT)
```

### 自动取消执行
> 当执行操作结束, 但`DataReader`中仍有数据为读出时, 自动调用`DBCommand.Cancel()`取消执行
```csharp
public void Dispose()
{
    if (_reader.Read() || _reader.NextResult())
    {
        // 如果datareader未读完直接dispose,将会自动读取剩下的数据后才释放
        _command?.Cancel();
    }
    _reader?.Close();
    _reader?.Dispose();
    _reader = null;
}
```


## 更新说明 
##### 2019.10.31
+ 初始版本