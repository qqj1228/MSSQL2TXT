using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace MSSQL2TXT {
    public class Model {
        public string[] StrConn { get; set; }
        public string StrConfigFile { get; set; }
        readonly Logger Log;
        readonly Config Cfg;

        public Model(Config Cfg, Logger Log) {
            this.Cfg = Cfg;
            this.Log = Log;
            ReadConfig();
        }

        void ReadConfig() {
            StrConn = new string[Cfg.ExDBList.Count];
            for (int i = 0; i < StrConn.Length; i++) {
                StrConn[i] = "user id=" + Cfg.DB.UserID + ";";
                StrConn[i] += "password=" + Cfg.DB.Pwd + ";";
                StrConn[i] += "database=" + Cfg.ExDBList[i].Name + ";";
                StrConn[i] += "data source=" + Cfg.DB.IP + "," + Cfg.DB.Port;
            }
        }

        public void ShowDB(string StrTable, int index) {
            string StrSQL = "select * from " + StrTable;

            using (SqlConnection sqlConn = new SqlConnection(StrConn[index])) {
                sqlConn.Open();
                SqlCommand sqlCmd = new SqlCommand(StrSQL, sqlConn);
                SqlDataReader sqlData = sqlCmd.ExecuteReader();
                string str = "";
                int c = sqlData.FieldCount;
                while (sqlData.Read()) {
                    for (int i = 0; i < c; i++) {
                        object obj = sqlData.GetValue(i);
                        if (obj.GetType() == typeof(DateTime)) {
                            str += ((DateTime)obj).ToString("yyyy-MM-dd") + "\t";
                        } else {
                            str += obj.ToString() + "\t";
                        }
                    }
                    str += "\n";
                }
                Console.WriteLine(str);
            }
        }

        public string[] GetTableName(int index) {
            try {
                using (SqlConnection sqlConn = new SqlConnection(StrConn[index])) {
                    sqlConn.Open();
                    DataTable schema = sqlConn.GetSchema("Tables");
                    int count = schema.Rows.Count;
                    string[] tableName = new string[count];
                    for (int i = 0; i < count; i++) {
                        DataRow row = schema.Rows[i];
                        foreach (DataColumn col in schema.Columns) {
                            if (col.Caption == "TABLE_NAME") {
                                if (col.DataType.Equals(typeof(DateTime))) {
                                    tableName[i] = string.Format("{0:d}", row[col]);
                                } else if (col.DataType.Equals(typeof(Decimal))) {
                                    tableName[i] = string.Format("{0:C}", row[col]);
                                } else {
                                    tableName[i] = string.Format("{0}", row[col]);
                                }
                            }
                        }
                    }
                    //foreach (var item in tableName) {
                    //    Console.WriteLine(item);
                    //}
                    //Console.WriteLine();
                    return tableName;
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
            return new string[] { "" };
        }

        public string[] GetTableColumns(string strTableName, int index) {
            try {
                using (SqlConnection sqlConn = new SqlConnection(StrConn[index])) {
                    sqlConn.Open();
                    DataTable schema = sqlConn.GetSchema("Columns", new string[] { null, null, strTableName });
                    schema.DefaultView.Sort = "ORDINAL_POSITION";
                    schema = schema.DefaultView.ToTable();
                    int count = schema.Rows.Count;
                    string[] tableName = new string[count];
                    for (int i = 0; i < count; i++) {
                        DataRow row = schema.Rows[i];
                        foreach (DataColumn col in schema.Columns) {
                            if (col.Caption == "COLUMN_NAME") {
                                if (col.DataType.Equals(typeof(DateTime))) {
                                    tableName[i] = string.Format("{0:d}", row[col]);
                                } else if (col.DataType.Equals(typeof(Decimal))) {
                                    tableName[i] = string.Format("{0:C}", row[col]);
                                } else {
                                    tableName[i] = string.Format("{0}", row[col]);
                                }
                            }
                        }
                    }
                    //foreach (var item in tableName) {
                    //    Console.WriteLine(item);
                    //}
                    //Console.WriteLine();
                    return tableName;
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
            return new string[] { "" };
        }

        string[,] SelectDB(string strSQL, int index) {
            try {
                int count = 0;
                List<string[]> rowList;
                using (SqlConnection sqlConn = new SqlConnection(StrConn[index])) {
                    SqlCommand sqlCmd = new SqlCommand(strSQL, sqlConn);
                    sqlConn.Open();
                    SqlDataReader sqlData = sqlCmd.ExecuteReader();
                    count = sqlData.FieldCount;
                    rowList = new List<string[]>();
                    while (sqlData.Read()) {
                        string[] items = new string[count];
                        for (int i = 0; i < count; i++) {
                            object obj = sqlData.GetValue(i);
                            if (obj.GetType() == typeof(DateTime)) {
                                items[i] = ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                            } else {
                                items[i] = obj.ToString();
                            }
                        }
                        rowList.Add(items);
                    }
                }
                string[,] records = new string[rowList.Count, count];
                for (int i = 0; i < rowList.Count; i++) {
                    for (int j = 0; j < count; j++) {
                        records[i, j] = rowList[i][j];
                    }
                }
                return records;
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
            return new string[,] { { "" }, { "" } };
        }

        public string[,] GetNewRecords(string strTableName, int index) {
            string strLastID = "";
            if (Cfg.ExDBList[index].LastID >= 0) {
                strLastID = Cfg.ExDBList[index].LastID.ToString();
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("ERROR: The {0}.LastID < 0", Cfg.ExDBList[index].Name));
                Console.ResetColor();
                Log.TraceError(string.Format("The {0}.LastID < 0", Cfg.ExDBList[index].Name));
                return new string[,] { { "" }, { "" } };
            }
            string strSQL = "select * from " + strTableName + " where ID > '" + strLastID + "'";
            //Log.TraceInfo("SQL: " + strSQL);
            string[,] strArr = SelectDB(strSQL, index);
            return strArr;
        }
    }
}
