using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MSSQL2TXT {
    class TxtFile {
        readonly Logger Log;
        readonly Config Cfg;
        string StrTempPath { get; set; }

        public TxtFile(Config Cfg, Logger Log) {
            this.Cfg = Cfg;
            this.Log = Log;
            this.StrTempPath = ".\\temp\\";
            CreateLogPath();
        }

        void CreateLogPath() {
            if (!Directory.Exists(StrTempPath)) {
                Directory.CreateDirectory(StrTempPath);
            }
        }

        public void WriteTxt(string FileName, string content) {
            FileStream fs = new FileStream(this.StrTempPath + FileName + ".txt", FileMode.Create);
            byte[] data = new UTF8Encoding().GetBytes(content);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }

        public void MoveTxt() {
            bool status = ConnectState(Cfg.Main.RemoteAddress);
            if (status) {
                DirectoryInfo dirinfo = new DirectoryInfo(this.StrTempPath);
                FileInfo[] Files = dirinfo.GetFiles();
                foreach (FileInfo FileItem in Files) {
                    Transport(this.StrTempPath + FileItem.Name, Cfg.Main.RemoteAddress, FileItem.Name);
                    FileItem.Delete();
                }
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Can't connect remote address: " + Cfg.Main.RemoteAddress);
                Console.ResetColor();
                Log.TraceError("Can't connect remote address: " + Cfg.Main.RemoteAddress);
            }
        }

        /// <summary>
        /// 连接远程共享文件夹
        /// </summary>
        /// <param name="path">远程共享文件夹的路径</param>
        /// <param name="userName">用户名</param>
        /// <param name="passWord">密码</param>
        /// <returns></returns>
        bool ConnectState(string path, string userName = "", string passWord = "") {
            bool Flag = false;
            string dosLine = "";
            Process proc = new Process();
            try {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();

                if (path.EndsWith("\\")) {
                    path = path.Remove(path.Length - 1, 1);
                }
                if (userName == "") {
                    dosLine = "net use " + path;
                } else {
                    dosLine = "net use " + path + " " + passWord + " /user:" + userName;
                }

                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while (!proc.HasExited) {
                    proc.WaitForExit(1000);
                }
                string errormsg = proc.StandardError.ReadToEnd();
                proc.StandardError.Close();
                if (string.IsNullOrEmpty(errormsg)) {
                    Flag = true;
                } else {
                    throw new Exception(errormsg);
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            } finally {
                proc.Close();
                proc.Dispose();
            }
            return Flag;
        }

        /// <summary>
        /// 向远程文件夹保存本地内容，或者从远程文件夹下载文件到本地
        /// </summary>
        /// <param name="src">要保存的文件的路径，如果保存文件到共享文件夹，这个路径就是本地文件路径如：@"D:\1.avi"</param>
        /// <param name="dst">保存文件的路径，不含名称及扩展名</param>
        /// <param name="fileName">保存文件的名称以及扩展名</param>
        void Transport(string src, string dst, string fileName) {

            FileStream inFileStream = new FileStream(src, FileMode.Open);
            if (!Directory.Exists(dst)) {
                Directory.CreateDirectory(dst);
            }
            if (dst.EndsWith("\\")) {
                dst = dst + fileName;
            } else {
                dst = dst + "\\" + fileName;
            }
            FileStream outFileStream = new FileStream(dst, FileMode.OpenOrCreate);
            byte[] buf = new byte[inFileStream.Length];
            int byteCount;
            while ((byteCount = inFileStream.Read(buf, 0, buf.Length)) > 0) {
                outFileStream.Write(buf, 0, byteCount);
            }
            inFileStream.Flush();
            inFileStream.Close();
            outFileStream.Flush();
            outFileStream.Close();
        }
    }
}
