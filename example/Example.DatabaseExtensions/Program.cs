using blqw.Gadgets;
using MySql.Data.MySqlClient;
using System;
//using blqw.Gadgets;

namespace Example.DatabaseExtensions
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var str = "12345";
            Console.WriteLine(str + "\b\b12");

            using (var conn = new MySqlConnection($"Server=192.168.1.111;Port=3306;Database=fuxiao_xinzhu;Uid={Environment.GetEnvironmentVariable("mysql_user")};Pwd={Environment.GetEnvironmentVariable("mysql_pwd")};"))
            {
                var wechat = conn.ExecuteFirst($"select * from wechats where OPEN_ID in ({new byte[0]:qwert,1234}) limit 1");
            }



            //var table = new DataTable();
            //table.Columns.Add("ID", typeof(int));
            //table.Columns.Add("Name", typeof(string));
            //table.Columns.Add("Sex", typeof(bool));

            //table.LoadDataRow(new object[] { 1, "周子鉴", true }, false);
            //table.LoadDataRow(new object[] { 2, "吕学梅", false }, false);
            //table.LoadDataRow(new object[] { 3, "周一一", DBNull.Value }, false);

            //var userBuidler = EntityBuilder.GetBuilder<User>();
            //var users = userBuidler.ToMultiple(table.CreateDataReader()).ToList();
            //var json = System.Text.Json.JsonSerializer.Serialize(users);
            //Console.WriteLine(json);
        }
    }


    public class Students
    {
        public int ID { get; set; }
        public int NUMBER { get; set; }
        public string NAME { get; set; }
        public string PHONE_BA { get; set; }
        public string PHONE_MA { get; set; }
        public string GENDER { get; set; }
        public string IDENTITY_CARD { get; set; }
        public string ADDRESS { get; set; }
        public int LEAVED { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime MODIFIED_AT { get; set; }
        public string PHONE { get; set; }
    }

}
