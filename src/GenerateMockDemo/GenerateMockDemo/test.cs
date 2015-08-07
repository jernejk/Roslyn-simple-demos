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

        public void SomeMethods2()
        {
            SomeMethods();
        }

        public void SomeMethod3()
        {
            SomeProperties = true;
        }
    }
}
