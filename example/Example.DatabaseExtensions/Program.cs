using blqw.Gadgets;
using blqw.Gadgets.DatabaseExtensions;
using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
//using blqw.Gadgets;

namespace Example.DatabaseExtensions
{
    class Program
    {
        static void Main(string[] args)
        {
           



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
