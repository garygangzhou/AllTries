using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class TaskCompletionSource
    {        
        static void Main(string[] args)
        {
            TaskCompletionSource p = new TaskCompletionSource();
            p.RunTask();

            Console.WriteLine("DONE");
            Console.ReadLine();
        }

        Action<string> Printout;
        private void RunTask()
        {
            Printout = this.Print;
            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                tcs1.SetResult(15);
            });

            // The attempt to get the result of t1 blocks the current thread until the completion source gets signaled.
            // It should be a wait of ~1000 ms.
            Stopwatch sw = Stopwatch.StartNew();
            int result = t1.Result;
            sw.Stop();

            Printout(string.Format("(ElapsedTime={0}): t1.Result={1} (expected 15) ", sw.ElapsedMilliseconds, result));          

        }

        private int Square(int i)
        {
            Random rd = new Random();
            int t = rd.Next(100, 8000);
            Thread.Sleep(t);
            Printout($"ThID: {Thread.CurrentThread.ManagedThreadId} Sleep {t}");

            return i * i;
        }

        private void Print(String str)
        {
            Debug.WriteLine(str);
            Console.WriteLine(str);
        }
    }
}
