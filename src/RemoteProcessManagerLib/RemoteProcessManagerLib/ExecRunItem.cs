using System;
using System.Diagnostics;
using System.Security;
using System.Collections.Generic;
using System.Collections;
/*
Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick ,
    vlad.novick@gmail.com , http://www.sgcombo.com , https://github.com/Vladimir-Novick
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
namespace RemoteProcessManagerLib.Runner
{
    public class ExecRunItem
    {
        public List<EnvironmentVariable> EnvironmentVariables = new List<EnvironmentVariable>();
        public string WorkingDirectory { get; set; }
        public string LatestMessage { get; private set; }
        private static SecureString MakeSecureString(string text)
        {
            SecureString secure = new SecureString();
            foreach (char c in text)
            {
                secure.AppendChar(c);
            }
            return secure;
        }
        public ExecRunItem()
        {
        }
        public void setSystemEnvironment()
        {
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
            {
                EnvironmentVariables.Add(new EnvironmentVariable(item.Key.ToString(), item.Value.ToString()));
            }
        }
        ///
        /// <summary> Create Task Adapter with domain Credentional </summary> 
        /// 
        /// <param name="UserName">Domain Used Name</param>
        /// <param name="Password">Domain user name Password</param>
        /// <param name="Domain">Domain Name</param>
        ///
        public ExecRunItem(String Username, String Password, String Domain)
        {
            username = Username;
            password = Password;
            domain = Domain;
        }
        private String username = null;
        private String password = null;
        private String domain = null;
        private String _ProcessID = "";
        private Func<String, String, bool> Callback;
        //   private StringBuilder strSummaryOutputString = new StringBuilder();
        private Process RunningProcess = null;
        public Process getProcess()
        {
            return RunningProcess;
        }
        public void RunProcess(String ProcessID, string EXEFileName, string CommandArguments, Func<String, String, bool> callBack = null, int Timeout = 0)
        {
            Callback = callBack;
            _ProcessID = ProcessID;
            try
            {
                RunningProcess = new Process();
                RunningProcess.StartInfo.UseShellExecute = false;
                ProcessStartInfo StartInfo = new ProcessStartInfo();
                StartInfo.UseShellExecute = false;
                if (username != null)
                {
                    StartInfo.UserName = username;
                    StartInfo.Password = MakeSecureString(password);
                    StartInfo.Domain = domain;
                }
                foreach (EnvironmentVariable variable in EnvironmentVariables)
                {
                    StartInfo.EnvironmentVariables[variable.Name] = variable.Value;
                }
                StartInfo.WorkingDirectory = WorkingDirectory;
                StartInfo.FileName = EXEFileName;
                StartInfo.Arguments = CommandArguments;
                StartInfo.RedirectStandardOutput = true;
                StartInfo.RedirectStandardError = true;
                StartInfo.CreateNoWindow = true;
                StartInfo.ErrorDialog = false;
                StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                RunningProcess.EnableRaisingEvents = true;
                RunningProcess.Exited += new EventHandler(proc_Exited);
                RunningProcess.ErrorDataReceived += proc_DataReceived;
                RunningProcess.OutputDataReceived += proc_DataReceived;
                RunningProcess.StartInfo = StartInfo;
                RunningProcess.Start();
                RunningProcess.BeginErrorReadLine();
                RunningProcess.BeginOutputReadLine();
                if (Timeout > 0)
                {
                    if (!RunningProcess.WaitForExit(Timeout))
                    {
                        RunningProcess.Kill();
                        String message = String.Format(@"Timeout of {0} ms while is executing ""{1} {2}""",
                              Timeout, RunningProcess.StartInfo.FileName, RunningProcess.StartInfo.Arguments);
#if DEBUG
                        Console.WriteLine(message);
#endif
                        LogMessage(message);
                    }
                }
                else
                {
                    RunningProcess.WaitForExit();
                }
                int exitCode = RunningProcess.ExitCode;
                processExitCode = exitCode;
                LogMessage($"Process Exit with code {exitCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                String message = "Error:" + ex.Message;
                LogMessage(message);
            }
            return; // strSummaryOutputString.ToString();
        }
        private int processExitCode = 0;
        /// <summary>
        ///  Get exit code
        /// </summary>
        /// <returns></returns>
        public int getExitCode()
        {
            return processExitCode;
        }
        private void LogMessage(string message)
        {
            LatestMessage = message;
           
            if (Callback != null)
            {
                Callback(_ProcessID, message);
            }
            else
            {
#if DEBUG
                Console.WriteLine($"{_ProcessID}, {message}");
#endif
            }
        }
        private void proc_Exited(object sender, System.EventArgs e)
        {
        }
        void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            String strLine = e.Data as string;
            if (strLine != null)
            {
                LogMessage(strLine);
            }
        }
    }
}
