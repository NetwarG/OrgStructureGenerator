using System;
using System.Collections.Generic;
using System.Text;
using DrxFaker;
using System.Data.SqlClient;
using System.Linq;
using Npgsql;
using static DrxFaker.Program;

namespace DrxFaker
{
    class WorkWithDB
    {

        /// <summary>
        /// Получить следующий id для указанной таблицы
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="command">SQL команда</param>
        /// <returns>Следующий id</returns>
        public static int GetNextId(string tableName, object command)
        {
            int maxId = 0;
            if (sqlType == sqlTypes.Postgres)
            {
                var postgresCommand = command as NpgsqlCommand;
                postgresCommand.CommandText = $"select Max(Id) from {tableName}";
                var reader = postgresCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (!int.TryParse(reader[0].ToString(), out maxId))
                        return -1;
                }
                reader.Close();
            }
            else
            {
                var sqlCommand = command as SqlCommand;
                sqlCommand.CommandText = $"select Max(Id) from {tableName}";
                var reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    if (!int.TryParse(reader[0].ToString(), out maxId))
                        return -1;
                }
                reader.Close();
            }

            return maxId + 1;
        }

        /// <summary>
        /// Вставка данных
        /// </summary>
        /// <param name="query">Строка запроса</param>
        /// <param name="command">SQL команда</param>
        /// <returns>True - при успешном выполнении комманды, иначе false</returns>
        public static bool InsertData(string query, object command)
        {
            try
            {
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    postgresCommand.ExecuteNonQuery();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    sqlCommand.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Обновление данных в таблице связанной с генерацией ID
        /// </summary>
        /// <param name="command">SQL команда</param>
        /// <param name="tablename">Название таблицы</param>
        /// <returns>True - при успешном выполнении комманды, иначе false</returns>
        public static bool UpdateId(object command, string tablename)
        {
            var query = string.Format("update sungero_system_ids set lastid = (select max(id) from {0}) where tablename = '{0}'", tablename);
            try
            {
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    postgresCommand.ExecuteNonQuery();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    sqlCommand.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Проверка на наличие столбца в таблице
        /// </summary>
        /// <param name="command">SQL команда</param>
        /// <param name="tablename">Название таблицы</param>
        /// <param name="column">Название столбца</param>
        /// <returns>True - при наличии столбца, иначе false</returns>
        public static bool IsContainsColumn(object command, string tablename, string column)
        {
            var query = "SELECT column_name FROM information_schema.columns " +
                $"WHERE table_name = '{tablename}' AND column_name = '{column}'";
            try
            {
                var isHasColumn = false;
                if (sqlType == sqlTypes.Postgres)
                {
                    var postgresCommand = command as NpgsqlCommand;
                    postgresCommand.CommandText = query;
                    var reader = postgresCommand.ExecuteReader();
                    isHasColumn = reader.HasRows;
                    reader.Close();
                }
                else
                {
                    var sqlCommand = command as SqlCommand;
                    sqlCommand.CommandText = query;
                    var reader = sqlCommand.ExecuteReader();
                    isHasColumn = reader.HasRows;
                    reader.Close();
                }

                return isHasColumn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
