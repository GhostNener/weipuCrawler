using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace Helper
{
    public static class SqlHelper
    {
        public static string connstr =ConfigurationManager.ConnectionStrings["connstr"].ConnectionString;

        /// <summary>
        /// 打开数据库
        /// </summary>
        /// <param name="str">连接字符串</param>
        public static string openConnection(string str)
        {

            using (SqlConnection conn = new SqlConnection(str))
            {
                try
                {
                    conn.Open();
                    return "1";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
                finally
                {
                    closeConnection(str);
                }
            }

        }
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public static void closeConnection(string str)
        {
            using (SqlConnection conn = new SqlConnection(str))
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        /// <summary>
        /// 返回受影响行数
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string cmdText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// 返回第一行第一列
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string cmdText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// 返回dt
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(string cmdText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddRange(parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        /// <summary>
        /// 返回sdr
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static SqlDataReader ExecuteDataReader(string cmdText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
        }
        /// <summary>
        /// 格式化 DBNull 返回null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object FromDBValue(object value)
        {
            if (value == DBNull.Value)
            {
                return null;
            }
            else
            {
                return value;
            }
        }
        /// <summary>
        /// 格式化 null 返回DBNull
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ToDBValue(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            else
            {
                return value;
            }
        }
    }
}
