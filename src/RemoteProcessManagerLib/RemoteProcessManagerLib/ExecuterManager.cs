using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime;
using System.Reflection;
using System.Threading;
using System.IO;

/*
Copyright (C) 2016-2018 by Vladimir Novick http://www.linkedin.com/in/vladimirnovick , 

    vlad.novick@gmail.com

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
    ///    Running multiple tasks asynchronously
    /// </summary>
    public class ExecuterManager
    {

        public ConcurrentDictionary<int, ExecItem> ExecuterContainer = new ConcurrentDictionary<int, ExecItem>();


        public String RunExec(String TaskID, String exec, String param = "", 
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
                    executer.RunProcess(TaskID, exec1, param, callBack, timeoout);

                }
                catch (Exception ex)
                {
                    error_Message = ex.Message;
                   
                }
                executer = null;

            });
            if (!this.TryAdd(task, executer, TaskID,  description, callBack)) { return "Failed to start. (app already running)"; }

            return error_Message;

        }

        // String description = null, Func<String, bool> callBack



        private Boolean OnExitFunction(String TaskID,String Message)
        {
            return true;
        }

        public void Abort(string TaskName)
        {
            ExecItem task = ExecuterContainer.Values.FirstOrDefault(x => x.TaskName == TaskName);
            if (task != null && task.runItem != null)
            {
                try
                {
                    task.runItem.getProcess().Kill();
                    Console.WriteLine($"Process {TaskName} is killed by user request");
                    OnTaskExit(TaskName, $"Process is killed by user request");
                } catch ( Exception ex)
                {
                    Console.WriteLine($"Failed Kill Process {TaskName} ");
                    OnTaskExit(TaskName, $"Failed Kill Process {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($" Process {TaskName} is not exist");
                OnTaskExit(TaskName, $" Process  is not exist");
            }
        }

        public ExecuterManager()
        {
            OnTaskExit = OnExitFunction;
        }

        ///    Add Task OnExit Function
        /// </summary>
        /// <param name="CallBackExit"></param>
        public Func<String,String, bool> OnTaskExit { private get; set; }


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
        ///   Wait all task by specifications list
        /// </summary>
        /// <param name="taskNames"></param>
        public void WaitAll(List<String> taskNames)
        {
            try
            {
                List<Task> TaskList = new List<Task>();
                foreach (ExecItem item in ExecuterContainer.Values)
                {
                    try
                    {
                        var ok = taskNames.Find(m => m == item.TaskName);
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
        /// <param name="taskName"></param>
        /// <param name="CurrentStatus"></param>
        public void SetCurrentStatus(string taskName, String CurrentStatus)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.TaskName == taskName);
                if (item != null)
                {
                    item.CurrentStatus = CurrentStatus;
                }
            }
        }
        /// <summary>
        ///    Get current task status string
        /// </summary>
        /// <param name="taskName"></param>
        public string GetCurrentStatus(string taskName)
        {
            if (ExecuterContainer.Count > 0)
            {
                ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.TaskName == taskName);
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
                            Name = item.TaskName,
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
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsCompleted(String name)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    ExecItem item = ExecuterContainer.Values.FirstOrDefault(x => x.TaskName == name);
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
        /// <param name="TaskName"></param>
        /// <returns></returns>
        public TaskStatus Status(String TaskName)
        {
            try
            {
                if (ExecuterContainer.Count > 0)
                {
                    List<ExecItem> items = ExecuterContainer.Values.ToList<ExecItem>();
                    ExecItem item = items.FirstOrDefault(x => x.TaskName == TaskName);
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
        /// <param name="TaskID"></param>
        /// <param name="description"></param>
        /// <param name="callBack">   bool myCallBack(string taskName ) </param>
        /// <returns></returns>
        public bool TryAdd(Task task, ExecRunItem executer,String ParamTaskName = null, String description = null, Func<String,String, bool> callBack = null)
        {

            String mTaskName = "";

            String errorMessage = "";
            
            if (ParamTaskName == null || ParamTaskName == "")
            {
                mTaskName = Guid.NewGuid().ToString();
            } else
            {
                mTaskName = ParamTaskName;
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
                                Console.WriteLine($"exit {outTaskItem.TaskName}");
                                outTaskItem.Callback(outTaskItem.TaskName, $"Exit:{outTaskItem.TaskName}"); // callback function for specific task
                            }
                            catch (Exception ex) { errorMessage = ex.Message; }
                        }

                         String Name = Remove(outTaskItem);
                    }

                    if (OnTaskExit != null)
                    {
                        Console.WriteLine($"exit {outTaskItem.TaskName}");
                        if (errorMessage.Length == 0)
                        {
                            OnTaskExit(outTaskItem.TaskName, $"Exit"); // Callback exit function for all tasks
                        } else
                        {
                            OnTaskExit(outTaskItem.TaskName, $"error:{errorMessage}"); // Callback exit function for all tasks
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
                TaskName = mTaskName,
                Id = task.Id,
                Task_ = task,
                StartTime = DateTime.Now,
                Description = _description,
                Callback = callBack,
                CurrentStatus = "Started",
                runItem = executer
            };
            List<ExecItem> items = ExecuterContainer.Values.ToList<ExecItem>();
            ExecItem item = items.FirstOrDefault(x => x.TaskName == mTaskName);
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
        public String Remove(ExecItem taskItem)
        {
            ExecItem outItem = null;
            String TaskName = null;
            if (taskItem == null) return null;
            try
            {
                if (ExecuterContainer.TryRemove(taskItem.Id, out outItem))
                {
                    if (outItem != null)
                    {
                        TaskName = outItem.TaskName;
                    }
                }
            }
            catch (Exception)
            {
            }
            return TaskName;
        }
    }
}
