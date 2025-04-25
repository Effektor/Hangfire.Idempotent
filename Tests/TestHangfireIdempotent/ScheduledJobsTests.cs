using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Idempotent;
using Hangfire.MemoryStorage;

namespace TestHangfireIdempotent
{
    public class ScheduledJobsTests
    {
        [Test]
        public async Task ScheduledJob_ShouldNotDuplicate_WhenTriggeredMultipleTimes()
        {
            // Arrange
            
            GlobalConfiguration.Configuration
                .UseMemoryStorage()
                .UseIdempotent(); // your extension

            RecurringJob.AddOrUpdate("test-job", () => ProcessRow(), Cron.Hourly);

            // Act - manually trigger twice
            RecurringJob.TriggerJob("test-job");
            RecurringJob.TriggerJob("test-job");

            // Wait a moment for background workers to pick up
            await Task.Delay(500);

            // Assert
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var enqueued = monitoringApi.EnqueuedJobs("default", 0, 10);

            // You should only find one job enqueued
            Assert.That(enqueued.Count, Is.EqualTo(1));
        }

        public async Task ProcessRow()
        {
            await Task.Delay(1000); // Simulate some work
        }

    }
}
