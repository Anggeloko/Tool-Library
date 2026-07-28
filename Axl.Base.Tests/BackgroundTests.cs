using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Background;
using Moq;
using Axl.Base.Interfaces;
using System;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class BackgroundTests
    {
        [Test]
        public void BackgroundTask_Reentrancy_ShouldPreventMultipleExecutions()
        {
            int executions = 0;
            var task = new BackgroundTask("TestReentrancy", 50)
            {
                WorkSync = () =>
                {
                    Interlocked.Increment(ref executions);
                    Thread.Sleep(200); // Stay busy
                },
                IntervalProvider = () => -1 // No sequential restarts
            };

            // Start the first time
            task.Start();
            Thread.Sleep(50); // Wait for first execution to start and take the lock

            // Try to start again manually while it is still running
            task.Start();
            task.Start();
            
            Thread.Sleep(300); // Wait for the first task to finish. 
            
            task.Stop();
            
            Assert.AreEqual(1, executions, "Should have prevented concurrent executions via TrafficControl");
        }

        [Test]
        public void ServiceManager_StopAll_ShouldWaitForTasks()
        {
            var mockLog = new Mock<ILog>();
            var manager = new BackgroundServiceManager(mockLog.Object);
            bool taskFinished = false;

            var task = new BackgroundTask("SlowTask", 10)
            {
                WorkSync = () =>
                {
                    Thread.Sleep(500); 
                    taskFinished = true;
                }
            };

            manager.AddTask(task);
            manager.StartAll();
            Thread.Sleep(50); // Ensure it started

            var start = DateTime.Now;
            manager.StopAll(2000);
            var duration = (DateTime.Now - start).TotalMilliseconds;

            Assert.IsTrue(taskFinished, "ServiceManager should have waited for the task to complete");
            Assert.GreaterOrEqual(duration, 400, "Shutdown should have taken at least the task duration");
        }
    }
}

