# Process-Manager 

The Process Manager library provides the launch multiple asynchronous processes 
 and provides information about their statuses. Provides running multiple external console program as hidden.
 You must be including RemoteProcessManager in an existing .NET Core application without installation additional components

### Example:

####   Create callback functions:

1) Global callback function:
       A callback function is executed after any process is finished
 
        public static bool OnExitallbackFunction(String ProcessID, String TaskName)
        {
            Console.WriteLine($"ProcessID: { ProcessID} , Task Completed : {TaskName} , Task Container count {executerManager.Count()}");
            return true;
        }

2) Process callback function:
      A callback function is executed all time if specific process write message to console.
      Console.WriteLine() is not showing any output on screen.   
    

        public static bool OnProcessCallBack(String ProcessID, String line)
        {
           Console.WriteLine($"ProcessID {ProcessID} , Line: {line}");
            return true;
        }

####   Using:


        using (ExecuterManager executerManager = new ExecuterManager())
        {
           executerManager.OnProcessExit = OnExitCallbackFunction;
           executerManager.RunExec("App_1",
                   "APP1.exe", "task1", "", 
                   OnProcessCallBack,
                    @"D:\STORE");
           executerManager.RunExec("App_2",
                   "APP2.exe",
                    "task2", "",
                     OnProcessCallBack,
                     @"D:\STORE");
           executerManager.WaitAll();
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
