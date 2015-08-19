using NUnit.Framework;
using System;
using PSTesting;

namespace Tests
{
    [TestFixture]
    public class Test : TestBase
    {
        [Test]
        public void PostAndPreCommandsWork()
        {
            Shell.SetPostExecutionCommands("4", "5");
            Shell.SetPreExecutionCommands("1", "2");
            var res = Shell.Execute("3");
            Assert.That(res, Is.EqualTo(new [] {1, 2, 3, 4, 5}));
        }
    }
}

