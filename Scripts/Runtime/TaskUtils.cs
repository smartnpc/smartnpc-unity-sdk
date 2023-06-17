using System;
using System.Threading.Tasks;

namespace SmartNPC
{
    public static class TaskUtils
    {
        public static async Task WaitUntil(Func<bool> predicate, int sleep = 30)
        {
            while (!predicate())
            {
                await Task.Delay(sleep);
            }
        }
    }
}