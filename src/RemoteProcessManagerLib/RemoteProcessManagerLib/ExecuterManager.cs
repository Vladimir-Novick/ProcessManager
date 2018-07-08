using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Diagnostics;

/*
Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick , 

    vlad.novick@gmail.com

    http://www.sgcombo.com

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

*/


namespace RemoteProcessManagerLib.Runner
{
    /// <summary>
    ///    Running multiple processes asynchronously
    /// </summary>
    public class ExecuterManager
    {

        public ConcurrentDictionary<int, ExecItem> ExecuterContainer = new ConcurrentDictionary<int, ExecItem>();


        public String RunExec(String ProcessID, String exec, String param = "", 
                      String description = null, Func<String ,String, bool> callBack = null, String workingDir = null, int timeoout = 0)
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
                    } else
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
            if (!this.TryAdd(task, executer, ProcessID,  description, callBack)) { return "Failed to start. (app already running)"; }

            return error_Message;

        }

        private Boolean OnExitFunction(String ProcessID,String Message)
        {
            return true;
        }

        public void Abort(string ProcessName)
        {
            ExecItem task = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == ProcessName);
            if (task != null && task.runItem != null)
            {
                try
                {
                    task.runItem.getProcess().Kill();
                    Console.WriteLine($"Process {ProcessName} is killed by user request");
                    OnProcessExit(ProcessName, $"Process is killed by user request");
                } catch ( Exception ex)
                {
                    Console.WriteLine($"Failed Kill Process {ProcessName} ");
                    OnProcessExit(ProcessName, $"Failed Kill Process {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($" Process {ProcessName} is not exist");
                OnProcessExit(ProcessName, $" Process  is not exist");
            }
        }

        public ExecuterManager()
        {
            OnProcessExit = OnExitFunction;
        }

        ///    Add Task OnExit Function
        /// </summary>
        /// <param name="CallBackExit"></param>
        public Func<String,String, bool> OnProcessExit { private get; set; }


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
        ///    Get process by process ID
        /// </summary>
        /// <param name="processID"></param>
        /// <returns></returns>
        public Process GetProcess(string processID)
        {
            ExecItem task = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == processID);
            if (task != null && task.runItem != null)
            {
                try
                {
                    return task.runItem.getProcess();
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
        public double GetProcessMemory(String processID)
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
        ///   Wait all task by specifications list
        /// </summary>
        /// <param name="processNames"></param>
        public void WaitAll(List<String> processNames)
        {
            try
            {
                List<Task> TaskList = new List<Task>();
                foreach (ExecItem item in ExecuterContainer.Values)
                {
                    try
                    {
                        var ok = processNames.Find(m => m == item.ProcessName);
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
        /// <param name="processName"></param>
        /// <param name="CurrentStatus"></param>
        public void SetCurrentStatus(string processName, String CurrentStatus)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == processName);
                if (item != null)
                {
                    item.CurrentStatus = CurrentStatus;
                }
            }
        }
        /// <summary>
        ///    Get current task status string
        /// </summary>
        /// <param name="processName"></param>
        public string GetCurrentStatus(string processName)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == processName);
                if (item != null)
                {
                    return item.CurrentStatus;
                }
            }
            return "Finished";
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
        ///   Wait Any Task
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
        ///    Get Task Count
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return ExecuterContainer.Count();
        }
        /// <summary>
        ///   Check specific task is completed
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public bool IsCompleted(String processName)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.ProcessName == processName);
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
        /// <param name="processName"></param>
        /// <returns></returns>
        public TaskStatus Status(String processName)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    List<ExecItem> items = ExecuterContainer.Values.ToList<ExecItem>();
                    ExecItem item = items.FirstOrDefault(x => x.ProcessName == processName);
                    if (item != null)
                    {
                        Task task = item.Task_;
                        if (task == null) return TaskStatus.RanToCompletion;
                        return task.Status;
                    }
                }
            }
            catch { }
            return TaskStatus.RanToCompletion;
        }




        /// <summary>
        ///    Add a task to container
        /// </summary>
        /// <param name="task"></param>
        /// <param name="ProcessID"></param>
        /// <param name="description"></param>
        /// <param name="callBack">   bool myCallBack(string processName ) </param>
        /// <returns></returns>
        public bool TryAdd(Task task, ExecRunItem executer,String processName = null, String description = null, Func<String,String, bool> callBack = null)
        {

            String mProcessName = "";

            String errorMessage = "";
            
            if (processName == null || processName == "")
            {
                mProcessName = Guid.NewGuid().ToString();
            } else
            {
                mProcessName = processName;
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
                                Console.WriteLine($"exit {outTaskItem.ProcessName}");
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
                            OnProcessExit(outTaskItem.ProcessName, $"Exit"); // Callback exit function for all tasks
                        } else
                        {
                            OnProcessExit(outTaskItem.ProcessName, $"error:{errorMessage}"); // Callback exit function for all tasks
                        }
                    }
                } catch ( Exception ex)
                {
                    if (ExecuterContainer.TryGetValue(currentTask.Id, out outTaskItem))
                    {
                        Remove(outTaskItem);
                    }
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
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);

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
                runItem = executer
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
        /// <param name="task"></param>
        /// <returns>TaskItem</returns>
        private String Remove(ExecItem taskItem)
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
