using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace TaskSchedulerExTest
{
    public partial class Form1 : Form
    {
        private TaskSchedulerEx _taskEx = new TaskSchedulerEx(0, 20);

        public Form1()
        {
            InitializeComponent();
            ThreadPool.SetMinThreads(20, 20);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_taskEx != null)
            {
                _taskEx.Dispose(); //释放资源
            }
        }

        #region Log
        private void Log(string log)
        {
            if (!this.IsDisposed)
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        textBox1.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + " " + log + "\r\n\r\n");
                    }));
                }
                else
                {
                    textBox1.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + " " + log + "\r\n\r\n");
                }
            }
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            _taskEx.Run(() =>
            {
                Log("简单测试");
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Log("==== 开始 ========");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            _taskEx.Run(() =>
            {
                int n = 100;
                int count = 0;
                List<Task> taskList = new List<Task>();
                for (int i = 0; i < n; i++)
                {
                    Task task = _taskEx.Run((obj) =>
                    {
                        Interlocked.Increment(ref count);
                        int k = (int)obj;
                        Thread.Sleep(100); //模拟耗时
                        Log("测试 " + k.ToString("000"));
                    }, i);
                    taskList.Add(task);
                }

                Task.WaitAll(taskList.ToArray());

                Log($"==== 结束，count={count}，耗时：{stopwatch.Elapsed.TotalSeconds.ToString("0.000")} 秒 ========");
                stopwatch.Stop();
            });
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            Log("==== 开始 ========");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int n = 100;
            int count = 0;
            List<Task> taskList = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                Task task = _taskEx.Run((obj) =>
                {
                    Interlocked.Increment(ref count);
                    int k = (int)obj;
                    Thread.Sleep(100); //模拟耗时
                    Log("测试 " + k.ToString("000"));
                }, i);
                taskList.Add(task);
            }

            foreach (Task tsk in taskList)
            {
                await tsk;
            }

            Log($"==== 结束，count={count}，耗时：{stopwatch.Elapsed.TotalSeconds.ToString("0.000")} 秒 ========");
            stopwatch.Stop();
        }

        //使用C#原生Task类测试
        private void button4_Click(object sender, EventArgs e)
        {
            Log("==== 开始 ========");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Task.Run(() =>
            {
                int n = 100;
                int count = 0;
                List<Task> taskList = new List<Task>();
                for (int i = 0; i < n; i++)
                {
                    Task task = Task.Factory.StartNew((obj) =>
                    {
                        Interlocked.Increment(ref count);
                        int k = (int)obj;
                        Thread.Sleep(100); //模拟耗时
                        Log("测试 " + k.ToString("000"));
                    }, i);
                    taskList.Add(task);
                }

                Task.WaitAll(taskList.ToArray());

                Log($"==== 结束，count={count}，耗时：{stopwatch.Elapsed.TotalSeconds.ToString("0.000")} 秒 ========");
                stopwatch.Stop();
            });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Task.Run(() =>
                {
                    Log("==== 开始 ========");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    int count = 0;
                    List<Task> taskList = new List<Task>();
                    for (int i = 0; i < 1000; i++)
                    {
                        Task task = _taskEx.Run((obj) =>
                        {
                            Interlocked.Increment(ref count);
                            int k = (int)obj;
                            if (k % 100 == 0)
                            {
                                Log("测试 " + k.ToString("000"));
                            }
                        }, i);
                        taskList.Add(task);
                        if (i % 10 == 0)
                        {
                            Thread.Sleep(1);
                        }
                    }

                    Task.WaitAll(taskList.ToArray());

                    Log($"==== 结束，count={count}，耗时：{stopwatch.Elapsed.TotalSeconds.ToString("0.000")} 秒 ========");
                    stopwatch.Stop();
                });

                Thread.Sleep(20100);

                Task.Run(() =>
                {
                    Log("==== 开始 ========");
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    int count = 0;
                    List<Task> taskList = new List<Task>();
                    for (int i = 0; i < 20; i++)
                    {
                        Task task = _taskEx.Run((obj) =>
                        {
                            Interlocked.Increment(ref count);
                            int k = (int)obj;
                            Log("测试 " + k.ToString("000"));
                        }, i);
                        taskList.Add(task);
                        Thread.Sleep(500);
                    }

                    Task.WaitAll(taskList.ToArray());

                    Log($"==== 结束，count={count}，耗时：{stopwatch.Elapsed.TotalSeconds.ToString("0.000")} 秒 ========");
                    stopwatch.Stop();
                });
            });
        }
    }
}
