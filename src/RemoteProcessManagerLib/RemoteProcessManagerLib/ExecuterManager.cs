using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Diagnostics;
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
    /// <summary>
    ///    Running multiple processes asynchronously
    /// </summary>
    public class ExecuterManager : IDisposable
    {
        bool disposed = false;
        /// <summary>
        ///    Dispose method to release unmanaged resources used by your application.
        ///    Abort all running process on the container
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        /// <summary>
        ///    Dispose method to release unmanaged resources used by your application.
        ///    Abort all running process on the container
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                var v = ExecuterContainer.Values.ToList();
                foreach (var item in v)
                {
                    try
                    {
                        Abort(item.ProcessName);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            disposed = true;
        }
        private ConcurrentDictionary<int, ExecItem> ExecuterContainer = new ConcurrentDictionary<int, ExecItem>();
        /// <summary>
        ///   Add process to process manager and run it 
        /// </summary>
        /// <param name="ProcessID"></param>
        /// <param name="exec"></param>
        /// <param name="param"></param>
        /// <param name="description"></param>
        /// <param name="callBack"></param>
        /// <param name="workingDir"></param>
        /// <param name="timeoout"></param>
        /// <returns></returns>
        public String RunExec(String ProcessID, String exec, String param = "",
                      String description = null, Func<String, String, bool> callBack = null, String workingDir = null, int timeoout = 0)
        {
            ExecRunItem executer = null;
            executer = new ExecRunItem();
            String error_Message = "";
            Task task = new Task(() =>
            {
                try
                {
                    executer.setSystemEnvironment();
                    String exec1 = exec;
                    if (workingDir == null)
                    {
                        executer.WorkingDirectory = Environment.CurrentDirectory;
                    }
                    else
                    {
                        executer.WorkingDirectory = workingDir;
                        exec1 = workingDir + Path.DirectorySeparatorChar + exec;
                    }
                    executer.RunProcess(ProcessID, exec1, param, callBack, timeoout);
                }
                catch (Exception ex)
                {
                    error_Message = ex.Message;
                }
                executer = null;
            });
            if (!this.TryAdd(task, executer, ProcessID, description, callBack)) { return "Failed to start. (app already running)"; }
            return error_Message;
        }
        // String description = null, Func<String, bool> callBack
        private Boolean OnExitFunction(String ProcessID, String Message, String exitCode)
        {
            return true;
        }
        /// <summary>
        ///   Kill the process by specifying its ProcessName. 
        ///   All the below kill conventions will send the TERM signal to the specified process.
        /// </summary>
        /// <param name="ProcessName"></param>
        public void Abort(string ProcessName)
        {
            ExecItem task = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == ProcessName);
            if (task != null && task.execItem != null)
            {
                try
                {
                    task.execItem.getProcess().Kill();
#if DEBUG
                    Console.WriteLine($"Process {ProcessName} is killed by user request");
#endif
                    OnProcessExit(ProcessName, $"Process is killed by user request", "-1");
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine($"Failed Kill Process {ProcessName} ");
#endif
                    OnProcessExit(ProcessName, $"Failed Kill Process {ex.Message}", "-1");
                }
            }
            else
            {
#if DEBUG
                Console.WriteLine($" Process {ProcessName} is not exist");
#endif
                OnProcessExit(ProcessName, $" Process  is not exist", "-1");
            }
        }
        /// <summary>
        ///     Get active process by ID
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public Process GetProcess(string processID)
        {
            ExecItem task = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == processID);
            if (task != null && task.execItem != null)
            {
                try
                {
                    return task.execItem.getProcess();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed Kill Process {ex.Message} ");
                    throw;
                }
            }
            else
            {
                throw new InvalidDataException(processID);
            }
        }
        /// <summary>
        ///   Get process memory (MB , trancate KB )
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public double GetMemory(String processID)
        {
            double mb = 0;
            try
            {
                Process process = GetProcess(processID);
                if (process != null)
                {
                    mb = process.WorkingSet64;
                    mb = mb / 1000;
                    mb = Math.Truncate(mb) / 1000;
                }
            }
            catch (Exception ex) { }
            return mb;
        }
        /// <summary>
        ///   Default Constructor.
        /// </summary>
        public ExecuterManager()
        {
            OnProcessExit = OnExitFunction;
        }
        /// <summary>
        ///   Use AbortAll to Stop Processes all process Executer Manager.
        ///   In contrast, kill terminates processes based on Process ID number 
        /// </summary>
        public void AbortAll()
        {
            var v = ExecuterContainer.Values.ToList();
            foreach (var item in v)
            {
                try
                {
                    Abort(item.ProcessName);
                }
                catch (Exception ex)
                {
                }
            }
        }
        /// <summary>
        ///   Task OnExit Callback Function
        /// </summary>
        /// <code>  
        ///  Example:
        ///    public static bool OnExitCallbackFunction(String ProcessID, String ProcessName, String exitCode)
        ///    {
        ///       Console.WriteLine($"ProcessID: { ProcessID} , Task Completed : {ProcessName} , Exit code {exitCode}");
        ///        return true;
        ///    }
        ///  </code>
        public Func<String, String, String, bool> OnProcessExit { private get; set; }
        /// <summary>
        ///   Wait All tasks from container
        /// </summary>
        public void WaitAll()
        {
            while (ExecuterContainer.Count > 0)
            {
                WaitAny();
            }
        }
        /// <summary>
        ///    The WaitAll blocks the current thread until all other tasks have completed execution.
        /// </summary>
        /// <param name="ProcessNames">List of process names</param>
        public void WaitAll(List<String> ProcessNames)
        {
            try
            {
                List<Task> TaskList = new List<Task>();
                foreach (ExecItem item in ExecuterContainer.Values)
                {
                    try
                    {
                        var ok = ProcessNames.Find(m => m == item.ProcessName);
                        if (ok != null)
                        {
                            if (item.Task_ != null)
                            {
                                TaskList.Add(item.Task_);
                            }
                        }
                    }
                    catch (Exception) { }
                }
                Task.WaitAll(TaskList.ToArray());
            }
            catch { }
        }
        /// <summary>
        ///    Set Current Task Status 
        /// </summary>
        /// <param name="ProcessName"></param>
        /// <param name="CurrentStatus"></param>
        public void SetCurrentStatus(string ProcessName, String CurrentStatus)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == ProcessName);
                if (item != null)
                {
                    item.CurrentStatus = CurrentStatus;
                }
            }
        }
        /// <summary>
        ///    Get current task status string
        /// </summary>
        /// <param name="ProcessName"></param>
        public string GetCurrentStatus(string ProcessName)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == ProcessName);
                if (item != null)
                {
                    return item.CurrentStatus;
                }
            }
            return "Finished";
        }
        /// <summary>
        ///   Get Message from Active Process
        /// </summary>
        /// <param name="ProcessName"></param>
        /// <returns></returns>
        public string GetLatestMessage(string ProcessName)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == ProcessName);
                if (item != null)
                {
                    return (item?.execItem?.LatestMessage) ?? "";
                }
            }
            return "";
        }
        /// <summary>
        ///    Get all active task
        /// </summary>
        /// <returns></returns>
        public List<ExecItemStatus> GetStatuses()
        {
            List<ExecItemStatus> TaskList = new List<ExecItemStatus>();
            try
            {
                foreach (var item in ExecuterContainer.Values)
                {
                    try
                    {
                        ExecItemStatus itemStatus = new ExecItemStatus()
                        {
                            Name = item.ProcessName,
                            Description = item.Description,
                            Start = item.StartTime
                        };
                        if (item.Task_ == null)
                        {
                            itemStatus.Status = TaskStatus.Canceled;
                        }
                        else
                        {
                            try
                            {
                                itemStatus.Status = item.Task_.Status;
                            }
                            catch (Exception)
                            {
                                itemStatus.Status = TaskStatus.Canceled;
                            }
                        }
                        TaskList.Add(itemStatus);
                    }
                    catch (Exception) { }
                }
            }
            catch { }
            return TaskList;
        }
        /// <summary>
        ///   Wait Any Task.  WaitAny locks the current thread until any process have completed execution.
        /// </summary>
        public void WaitAny()
        {
            try
            {
                List<Task> TaskList = new List<Task>();
                foreach (var item in ExecuterContainer.Values)
                {
                    TaskList.Add(item.Task_);
                }
                Task.WaitAny(TaskList.ToArray());
            }
            catch { }
        }
        /// <summary>
        ///    Get Task Count on container
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ExecuterContainer.Count();
        }
        /// <summary>
        ///   Check specific task is completed
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsCompleted(String name)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == name);
                    if (item != null)
                    {
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }
        /// <summary>
        ///   Get task status by task name
        /// </summary>
        /// <param name="ProcessName"></param>
        /// <returns></returns>
        public TaskStatus Status(String ProcessName)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    List<ExecItem> items = ExecuterContainer.Values.ToList<ExecItem>();
                    ExecItem item = items.FirstOrDefault(x => x.ProcessName == ProcessName);
                    if (item != null)
                    {
                        Task task = item.Task_;
                        if (task == null) return TaskStatus.RanToCompletion;
                        return task.Status;
                    }
                }
            }
            catch (Exception)
            {
            }
            return TaskStatus.RanToCompletion;
        }
        private bool TryAdd(Task task, ExecRunItem executer, String ParamProcessName = null, String description = null, Func<String, String, bool> callBack = null)
        {
            String mProcessName = "";
            String errorMessage = "";
            if (ParamProcessName == null || ParamProcessName == "")
            {
                mProcessName = Guid.NewGuid().ToString();
            }
            else
            {
                mProcessName = ParamProcessName;
            }
            task.ContinueWith(currentTask =>
            {
                ExecItem outTaskItem = null;
                try
                {
                    if (ExecuterContainer.TryGetValue(currentTask.Id, out outTaskItem))
                    {
                        if (outTaskItem.Callback != null)
                        {
                            try
                            {
#if DEBUG
                                Console.WriteLine($"exit {outTaskItem.ProcessName}");
#endif
                                outTaskItem.Callback(outTaskItem.ProcessName, $"Exit:{outTaskItem.ProcessName}"); // callback function for specific task
                            }
                            catch (Exception ex) { errorMessage = ex.Message; }
                        }
                        String Name = Remove(outTaskItem);
                    }
                    if (OnProcessExit != null)
                    {
                        Console.WriteLine($"exit {outTaskItem.ProcessName}");
                        if (errorMessage.Length == 0)
                        {
                            OnProcessExit(outTaskItem.ProcessName, $"Exit", outTaskItem?.execItem?.getExitCode().ToString()); // Callback exit function for all tasks
                        }
                        else
                        {
                            OnProcessExit(outTaskItem.ProcessName, $"error:{errorMessage}", outTaskItem?.execItem?.getExitCode().ToString()); // Callback exit function for all tasks
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnProcessExit(outTaskItem.ProcessName, $"error:{ex.Message}", outTaskItem?.execItem?.getExitCode().ToString()); // Callback exit function for all tasks
                    Remove(outTaskItem);
                }
            });
            String _description = description;
            if (_description == null)
            {
                try
                {
                    var fieldInfo = typeof(Task).GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic);
                    Delegate action = fieldInfo.GetValue(task) as Delegate;
                    if (action != null)
                    {
                        var name = action.Method.Name;
                        var type = action.Method.DeclaringType.FullName;
                        _description = $"Method: {name} Type: {type}";
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.Message);
#endif
                    throw;
                }
            }
            var taskItem = new ExecItem
            {
                ProcessName = mProcessName,
                Id = task.Id,
                Task_ = task,
                StartTime = DateTime.Now,
                Description = _description,
                Callback = callBack,
                CurrentStatus = "Started",
                execItem = executer
            };
            List<ExecItem> items = ExecuterContainer.Values.ToList<ExecItem>();
            ExecItem item = items.FirstOrDefault(x => x.ProcessName == mProcessName);
            if (!(item is null)) return false;
            bool ok = ExecuterContainer.TryAdd(taskItem.Id, taskItem);
            if (ok)
            {
                try
                {
                    task.Start();
                    return true;
                }
                catch
                {
                    Remove(item);
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        ///    Remove task from container
        /// </summary>
        /// <param name="taskItem"></param>
        /// <returns>TaskItem</returns>
        public String Remove(ExecItem taskItem)
        {
            ExecItem outItem = null;
            String ProcessName = null;
            if (taskItem == null) return null;
            try
            {
                if (ExecuterContainer.TryRemove(taskItem.Id, out outItem))
                {
                    if (outItem != null)
                    {
                        ProcessName = outItem.ProcessName;
                    }
                }
            }
            catch (Exception)
            {
            }
            return ProcessName;
        }
    }
}
