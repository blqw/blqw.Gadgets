using blqw.Gadgets.DatabaseExtensions;
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

    class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool? Sex { get; set; }
    }
}
