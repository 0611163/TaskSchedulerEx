# TaskSchedulerEx
一个自定义的C# TaskScheduler，独立线程池

## 特点

1. 使用独立线程池，线程池中线程分为核心线程和辅助线程，辅助线程会动态增加和释放，且总线程数不大于参数_maxThreadCount

2. 无缝兼容Task，使用上和Task一样，可以用它来实现异步，参见：C# async await 异步执行方法封装 替代 BackgroundWorker

3. 队列中尚未执行的任务可以取消

4. 通过扩展类TaskHelper实现任务分组

5. 和SmartThreadPool对比，优点是无缝兼容Task类，和Task类使用没有区别，因为它本身就是对Task、TaskScheduler的扩展，所以Task类的ContinueWith、WaitAll等方法它都支持，以及兼容async、await异步编程

6. 代码量相当精简，TaskSchedulerEx类只有260多行代码

7. 池中的线程数量会根据负载自动增减，支持，但没有SmartThreadPool智能，为了性能，使用了比较笨的方式实现，不知道大家有没有既智能，性能又高的方案，我有一个思路，在定时器中计算每个任务执行平均耗时，然后使用公式(线程数 = CPU核心数 * ( 本地计算时间 + 等待时间 ) / 本地计算时间)来计算最佳线程数，然后按最佳线程数来动态创建线程，但这个计算过程可能会牺牲性能

## 使用示例

```C#
using System;
using System.Windows.Forms;
using Utils;

/**
 * TaskSchedulerEx 使用示例
 */

namespace TaskSchedulerExTest
{
    public partial class Form1 : Form
    {
        private TaskSchedulerEx _taskEx = new TaskSchedulerEx(20, 20);

        public Form1()
        {
            InitializeComponent();
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

        //基本用法
        private void button1_Click(object sender, EventArgs e)
        {
            _taskEx.Run(() =>
            {
                Log("简单测试");
            });
        }

        //传递参数
        private void button2_Click(object sender, EventArgs e)
        {
            _taskEx.Run((obj) =>
            {
                Log("输入的参数是：" + obj ?? obj.ToString());
            }, "参数1");
        }

        //异步用法
        private async void button3_Click(object sender, EventArgs e)
        {
            for (int i = 1; i <= 10; i++)
            {
                await _taskEx.Run((obj) =>
                {
                    int k = (int)obj;
                    Log("异步测试，i=" + k);
                }, i);
            }
        }

        //返回值测试
        private async void button4_Click(object sender, EventArgs e)
        {
            string result = await _taskEx.Run<string>(() =>
            {
                return "返回值测试";
            });
            Log(result);
        }
    }
}
```
