using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace src.selfTest
{
    internal interface ITestClass
    {
        /// <summary>
        /// Returns a list of tests as (TestName, Func<Task>)
        /// </summary>
        IEnumerable<(string TestName, Func<Task> Action)> GetTests();
    }
}
