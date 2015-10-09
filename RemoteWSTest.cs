using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Kevoree.Library
{
    [TestFixture]
    class RemoteWSTest
    {
        [Test]
        public void runserver()
        {
            var a = new WSGroup();
            a.Start();
        }
    }
}
