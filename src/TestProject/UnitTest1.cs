using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteProcessManagerLib.Runner;
using System;
using System.Diagnostics;
using System.Threading;
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
namespace TestProject
{
    [TestClass]
    public class UnitTest
    {

        public static ExecuterManager executerManager = new ExecuterManager(); // Create container

        /// <summary>
        ///    Process  exit callback function
        /// </summary>
        /// <param name="ProcessID"></param>
        /// <param name="ProcessName"></param>
        /// <returns></returns>
        public static bool OnProcessExiFunction(String ProcessID, String ProcessName)
        {
            Console.WriteLine($"ProcessID: { ProcessID} , Task Completed : {ProcessName} , Task Container count {executerManager.Count()}");
            return true;
        }

        /// <summary>
        ///  Process callback function 
        /// </summary>
        /// <param name="ProcessID"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool OnProcessCallBack(String ProcessID, String line)
        {
           Console.WriteLine($"ProcessID {ProcessID} , Line: {line}");
            return true;
        }


        [TestMethod]
        public void RunExecCommand()
        {
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec("CHECK DIR", "dir.exe", "*.*");
        }

        [TestMethod]

        public void RunExecCommandWithCallback()
        {
          
            executerManager.RunExec("CHECK DIR", @"cmd.exe", @"/c dir *.*", "", OnProcessCallBack, @"C:\Windows\System32");
            executerManager.WaitAll(); // wait all process
        }

        /// <summary>
        ///  Test Multi-Process 
        /// </summary>
        [TestMethod]
        public void RunConsoleApplication()
        {
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task2", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.WaitAll();
        }

        [TestMethod]
        public void AbortProcess()
        {

            String ProcessID = Guid.NewGuid().ToString();
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(ProcessID, "TEST_MANAGEMED_APP.exe", "task1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(6000);
            executerManager.Abort(ProcessID);
            executerManager.WaitAll();
        }

        [TestMethod]
        public void GetProcessInfo()
        {
            String ProcessID = Guid.NewGuid().ToString();
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(ProcessID, "TEST_MANAGEMED_APP.exe", "Process1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(2000);
            Process process = executerManager.GetProcess(ProcessID);
            double mb = (double)process.WorkingSet64;
            mb = mb / 1000;
            mb = Math.Truncate(mb) / 1000;
            Console.WriteLine($"Memory: {mb} MB");

        }

        [TestMethod]
        public void GetProcessMemory()
        {
            String ProcessID = Guid.NewGuid().ToString();
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(ProcessID, "TEST_MANAGEMED_APP.exe", "Process1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(2000);
            double mb = executerManager.GetProcessMemory(ProcessID);

            Console.WriteLine($"Memory: {mb} MB");

        }

        [TestMethod]
        public void WaitAllProcess()
        {
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task2", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task3", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP"); executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task2", "", OnProcessCallBack,
              @"E:\STORE_EXEC\TEST_MANAGEMED_APP");

            while (executerManager.Count() > 0)
            {
                Console.WriteLine($" Task Completed ");
                executerManager.WaitAny();
            }

        }
        /// <summary>
        ///  get latest application message by processID
        /// </summary>
        [TestMethod]
        public void GetCurrentMessage()
        {
            String ProcessID = Guid.NewGuid().ToString();
            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(ProcessID, "TEST_MANAGEMED_APP.exe", "Process1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(2000);
            String lastMessage = executerManager.GetCurrentMessage(ProcessID);

            Console.WriteLine($"Last Message: {lastMessage}");

            Thread.Sleep(2000);
            lastMessage = executerManager.GetCurrentMessage(ProcessID);

            Console.WriteLine($"Last Message: {lastMessage}");

            executerManager.WaitAll();
        }

        /// <summary>
        ///  Abort All Processes
        /// </summary>
        [TestMethod]
        public void AbortAll()
        {

            executerManager.OnProcessExit = OnProcessExiFunction;
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task2", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task3", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP"); executerManager.RunExec(Guid.NewGuid().ToString(), "TEST_MANAGEMED_APP.exe", "task2", "", OnProcessCallBack,
              @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(1000);

            executerManager.AbortAll();
            executerManager.WaitAll();
        }

    }
}
