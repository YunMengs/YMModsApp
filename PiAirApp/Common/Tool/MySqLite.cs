using YMModsApp.Common.Models;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace YMModsApp.Common.Tool
{
    public class MySqLite
    {
        // 数据库文件夹
        static string DbPath = Path.Combine(@"./", @"Database");

        //与指定的数据库(实际上就是一个文件)建立连接
        private static SQLiteConnection CreateDatabaseConnection(string dbName = null)
        {
            if (!string.IsNullOrEmpty(DbPath) && !Directory.Exists(DbPath))
                Directory.CreateDirectory(DbPath);
            dbName = dbName == null ? "database.db" : dbName;
            var dbFilePath = Path.Combine(DbPath, dbName);
            return new SQLiteConnection("DataSource = " + dbFilePath);
        }

        // 使用全局静态变量保存连接
        public static SQLiteConnection connection = CreateDatabaseConnection();

        // 判断连接是否处于打开状态
        public static void Open(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }

        /// <summary>
        /// 执行非查询SQL语句代码，适用于建表、增删改等
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>受影响行数</returns>
        public static int ExecuteNonQuery(string sql)
        {
            // 确保连接打开
            Open(connection);
            int i = 0;
            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    i = command.ExecuteNonQuery();
                }
                tr.Commit();
            }
            return i;
        }

        /// <summary>
        /// 执行非查询SQL语句代码，适用于建表、增删改等
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>自增ID</returns>
        public static long ExecuteNonQueryGetID(string sql)
        {
            // 确保连接打开
            Open(connection);
            long i = 0;
            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    using (var cmdSonID = new SQLiteCommand("SELECT last_insert_rowid();", connection))
                    {
                        command.CommandText = sql;
                        command.ExecuteScalar();
                        i = (long)cmdSonID.ExecuteScalar();
                    }
                }
                tr.Commit();
            }
            return i;
        }

        public delegate void ExecuteQueryDelegate(SQLiteDataReader reader);
        public static void ExecuteQuery(string sql, ExecuteQueryDelegate executeQueryDelegate)
        {
            // 确保连接打开
            Open(connection);

            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    // 执行查询会返回一个SQLiteDataReader对象
                    SQLiteDataReader reader = command.ExecuteReader();

                    //reader.Read()方法会从读出一行匹配的数据到reader中。注意：是一行数据。
                    while (reader.Read())
                    {
                        // 有一系列的Get方法，方法的参数是列数。意思是获取第n列的数据，转成Type返回。
                        // 比如这里的语句，意思就是：获取第0列的数据，转成int值返回。
                        executeQueryDelegate(reader);
                    }
                }
                tr.Commit();
            }
        }
        public static SQLiteDataReader ExecuteQueryFirst(string sql)
        {
            // 确保连接打开
            Open(connection);
            SQLiteDataReader reader;
            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    reader = command.ExecuteReader();
                }
                tr.Commit();
            }
            return reader;
        }

        // 因为SQLite是文件型数据库，可以直接删除文件。但只要数据库连接没有被回收，就无法删除文件。
        public static void DeleteDatabase(string dbName)
        {
            var path = Path.Combine(DbPath, dbName);
            connection.Close();

            // 置空，手动GC，并等待GC完成后执行文件删除。
            connection = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(path);
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfig(string name)
        {
            // 确保连接打开
            Open(connection);
            string sql = "select * from `piairconfigs` where `key`= '" + name + "'";
            string value;
            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    var reader = command.ExecuteReader();
                    reader.Read();
                    value = reader.GetString(1);
                }
                tr.Commit();
            }

            return value;
        }

        /// <summary>
        /// 修改配置
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>受影响行数</returns>
        public static int SaveConfig(string name, string value)
        {
            // 确保连接打开
            Open(connection);
            string sql = "update `piairconfigs` set `value` = '" + value + "' where `key`= '" + name + "'";
            int i;
            using (var tr = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    i = command.ExecuteNonQuery();
                }
                tr.Commit();
            }
            return i;
        }


    }
}