using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateMockDemo
{
    public class test
    {
        public bool SomeProperties { get; set; }

        public bool SomeProperties2 { get; }

        public void SomeMethods()
        {
            SomeMethod3();
        }

        private async void SomeMethods2()
        {
            SomeMethods();
        }

        public string SomeMethod3()
        {
            SomeProperties = true;

            return "Test";
        }

        public void A(int a, int b, double c)
        {

        }
    }
}
