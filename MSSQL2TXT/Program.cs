using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using static MSSQL2TXT.Config;

namespace MSSQL2TXT {
    class Program {
        public struct TableData {
            public string[] Cols { get; set; }
            public string[,] Rs { get; set; }
            public int IDIndex { get; set; }
            public int VINIndex { get; set; }
        }

        static Logger log;
        static Config cfg;
        static Model db;
        static TxtFile txt;

        static void Main(string[] args) {
            log = new Logger("./log", EnumLogLevel.LogLevelAll, true, 100);
            cfg = new Config(log);
            db = new Model(cfg, log);
            txt = new TxtFile(cfg, log);
            Console.WriteLine("Application started, input \"exit\" to close this application");
            Timer timer = new Timer(cfg.Main.Interval * 1000);
            timer.Elapsed += TimerJob;
            timer.Enabled = true;
            string line = "";
            while (line != "exit") {
                line = Console.ReadLine();
            }
            timer.Close();
            timer.Dispose();
        }

        static void TimerJob(object sender, ElapsedEventArgs e) {
            for (int i = 0; i < cfg.ExDBList.Count; i++) {
                // 从数据库中获取数据填充到tD中
                TableData[] tD = new TableData[cfg.ExDBList[i].TableList.Count];
                for (int j = 0; j < tD.Length; j++) {
                    tD[j].Cols = db.GetTableColumns(cfg.ExDBList[i].TableList[j], i);
                    tD[j].Rs = db.GetNewRecords(cfg.ExDBList[i].TableList[j], i);
                    int IDIndex = 0;
                    int VINIndex = 1;
                    int iCount = 0;
                    for (int k = 0; k < tD[j].Cols.Length && iCount <= 2; k++) {
                        if (tD[j].Cols[k] == cfg.DB.ID) {
                            IDIndex = k;
                            ++iCount;
                        } else if (tD[j].Cols[k] == cfg.DB.VIN) {
                            VINIndex = k;
                            ++iCount;
                        }
                    }
                    tD[j].IDIndex = IDIndex;
                    tD[j].VINIndex = VINIndex;
                }

                string strContent = "";
                strContent += "WorkStation: " + cfg.ExDBList[i].Name + "\r\n";

                int iRow = tD[0].Rs.GetLength(0); // 获取到的新记录数量
                for (int k = 0; k < iRow; k++) {
                    strContent += "ID: " + tD[0].Rs[k, tD[0].IDIndex] + "\r\n";
                    strContent += "VIN: " + tD[0].Rs[k, tD[0].VINIndex] + "\r\n";
                    Console.WriteLine("INFO: Get new test result of [VIN]" + tD[0].Rs[k, tD[0].VINIndex] + " in [WorkStation]" + cfg.ExDBList[i].Name);
                    for (int n = 0; n < tD.Length; n++) {
                        // 表处理循环
                        strContent += "====================\r\n";
                        strContent += "Test Item: " + cfg.ExDBList[i].TableList[n] + "\r\n";
                        int iCol = tD[n].Cols.Length; // 当前表的字段数量
                        for (int m = 0; m < iCol; m++) {
                            // 字段处理循环
                            if (m != tD[n].IDIndex && m != tD[n].VINIndex) {
                                strContent += tD[n].Cols[m] + " = " + tD[n].Rs[k, m] + "\r\n";
                            }
                        }
                    }
                    txt.WriteTxt(tD[0].Rs[k, tD[0].VINIndex] + "_" + cfg.ExDBList[i].Name, strContent);

                    // 修改LastID值
                    ExportDBConfig TempExDB = cfg.ExDBList[i];
                    int.TryParse(tD[0].Rs[k, tD[0].IDIndex], out int result);
                    TempExDB.LastID = result;
                    cfg.ExDBList[i] = TempExDB;
                }
            }
            txt.MoveTxt();
            cfg.SaveConfig();
        }
    }
}
