using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async void Test()
        {
            try
            {
                await Task.Delay(500);
            }
            catch
            {
            }
        }

        public async void Test2()
        {
            await Task.Delay(500);
        }

        public void Test3()
        {
        }

        public async Task Test4Async()
        {
        }

        public async Task<bool> Something()
        {
            return false;
        }
    }
}