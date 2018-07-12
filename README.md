# Remote-Process-Manager 

The Remote Process Manager library provides the launch multiple asynchronous processes on the remote computer
 and provides information about their statuses. 
 
 Your can saving your output to a specified location 
 
 ( NET CORE 2.1 + Visual Studio 2017 )
 
  
## Example:
 
    [TestClass]
    public class UnitTest
    {

        public static ExecuterManager executerManager = new ExecuterManager(); // Create container

        /// <summary>
        ///    Process  exit callback function
        /// </summary>
        /// <param name="ProcessID"></param>
        /// <param name="TaskName"></param>
        /// <returns></returns>
        public static bool OnProcessExiFunctiont(String ProcessID, String TaskName)
        {
            Console.WriteLine($"ProcessID: { ProcessID} , Task Completed : {TaskName} , Task Container count {executerManager.Count()}");
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
            executerManager.OnTaskExit = OnProcessExiFunctiont;
            executerManager.RunExec("CHECK DIR", "dir.exe", "*.*");
        }

        [TestMethod]

        public void RunExecCommandWithCallback()
        {
            executerManager.OnTaskExit = OnProcessExiFunctiont;
            executerManager.RunExec("CHECK DIR", @"cmd.exe", @"/c dir *.*", "", OnProcessCallBack, @"C:\Windows\System32");
            executerManager.WaitAll(); // wait all task
        }

        /// <summary>
        ///  Test Multi-Process 
        /// </summary>
        [TestMethod]
        public void RunConsoleApplication()
        {
            executerManager.OnTaskExit = OnProcessExiFunctiont;
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
            executerManager.OnTaskExit = OnProcessExiFunctiont;
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
            executerManager.OnTaskExit = OnProcessExiFunctiont;
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
            executerManager.OnTaskExit = OnProcessExiFunctiont;
            executerManager.RunExec(ProcessID, "TEST_MANAGEMED_APP.exe", "Process1", "", OnProcessCallBack,
                @"E:\STORE_EXEC\TEST_MANAGEMED_APP");
            Thread.Sleep(2000);
            double mb = executerManager.GetProcessMemory(ProcessID);

            Console.WriteLine($"Memory: {mb} MB");

        }

    }

Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick , 

vlad.novick@gmail.com , http://www.sgcombo.com , https://github.com/Vladimir-Novick
		 
# License		

		Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick

		Permission is hereby granted, free of charge, to any person obtaining a copy
		of this software and associated documentation files (the "Software"), to deal
		in the Software without restriction, including without limitation the rights
		to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		copies of the Software, and to permit persons to whom the Software is
		furnished to do so, subject to the following conditions:

		The above copyright notice and this permission notice shall be included in
		all copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
		AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
		LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
		OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
		THE SOFTWARE. 