using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MSSQL2TXT {
    public class Config {
        public struct DBConnConfig {
            public string IP { get; set; }
            public string Port { get; set; }
            public string UserID { get; set; }
            public string Pwd { get; set; }
            public string ID { get; set; }
            public string VIN { get; set; }
        }

        public struct MainConfig {
            public int Interval { get; set; } // 单位秒
            public string RemoteAddress { get; set; } // 远程输出txt文件夹的路径
        }

        public struct ExportDBConfig {
            public string Name { get; set; } // 欲导出的数据库的名字
            public int LastID { get; set; } // 数据库最新记录的ID
            public List<string> TableList; // 欲导出的表名列表
        }

        public DBConnConfig DB;
        public MainConfig Main;
        public List<ExportDBConfig> ExDBList;
        readonly Logger Log;
        string ConfigFile { get; set; }

        public Config(Logger Log, string strConfigFile = "config.xml") {
            this.Log = Log;
            this.ExDBList = new List<ExportDBConfig>();
            this.ConfigFile = strConfigFile;
            LoadConfig();
        }

        ~Config() {
            SaveConfig();
        }

        void LoadConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    if (node.Name == "Main") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "Interval") {
                                int.TryParse(item.InnerText, out int result);
                                Main.Interval = result;
                            } else if (item.Name == "RemoteAddress") {
                                Main.RemoteAddress = item.InnerText;
                            }
                        }
                    } else if (node.Name == "DB") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "IP") {
                                DB.IP = item.InnerText;
                            } else if (item.Name == "Port") {
                                DB.Port = item.InnerText;
                            } else if (item.Name == "UserID") {
                                DB.UserID = item.InnerText;
                            } else if (item.Name == "Pwd") {
                                DB.Pwd = item.InnerText;
                            } else if (item.Name == "ID") {
                                DB.ID = item.InnerText;
                            } else if (item.Name == "VIN") {
                                DB.VIN = item.InnerText;
                            }
                        }
                    } else if (node.Name == "ExDB") {
                        ExportDBConfig TempExDB = new ExportDBConfig();

                        foreach (XmlNode item in xnlChildren) {
                            XmlNodeList xnlSubChildren = item.ChildNodes;
                            foreach (XmlNode subItem in xnlSubChildren) {
                                if (subItem.Name == "Name") {
                                    TempExDB.Name = subItem.InnerText;
                                } else if (subItem.Name == "LastID") {
                                    int.TryParse(subItem.InnerText, out int result);
                                    TempExDB.LastID = result;
                                } else if (subItem.Name == "TableList") {
                                    string str = subItem.InnerText;
                                    TempExDB.TableList = new List<string>(str.Split(','));
                                }
                            }
                            ExDBList.Add(TempExDB);
                        }
                    }
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }

        public void SaveConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    // 只操作了需要被修改的配置项
                    if (node.Name == "ExDB") {
                        for (int i = 0; i < ExDBList.Count; i++) {
                            XmlNodeList xnlSubChildren = xnlChildren[i].ChildNodes;
                            foreach (XmlNode item in xnlSubChildren) {
                                if (item.Name == "LastID") {
                                    item.InnerText = ExDBList[i].LastID.ToString();
                                }
                            }
                        }
                    }
                }

                xmlDoc.Save(ConfigFile);
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }
    }
}
