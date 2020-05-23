using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class ContinueWIth
    {
        public static async Task Main(string[] args)
        {
            
            Task<DayOfWeek> taskA = Task.Run(() => {
                Console.WriteLine( $"[{Thread.CurrentThread.ManagedThreadId}]"); return DateTime.Today.DayOfWeek; });

            await taskA.ContinueWith(antecedent => Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Today is {0}.", antecedent.Result));

            Console.ReadLine();
        }
    }
}
