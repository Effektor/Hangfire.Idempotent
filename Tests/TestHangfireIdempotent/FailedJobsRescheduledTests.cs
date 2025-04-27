using Hangfire.Common;
using Hangfire.MemoryStorage;
using Hangfire.Storage;
using System.Diagnostics;

namespace TestHangfireIdempotent;

[TestFixture]
public class FailedJobRescheduleTests
{

    /*
     The BackgroundJobClient will cache the MemoryStorage on first use and there is no facility to clear the memory or swap out the
     */

    private BackgroundJobServer _server;
    private static MemoryStorage _store;
    private BackgroundJobClient _jobClient;

    static FailedJobRescheduleTests()
    {
        _store = new MemoryStorage();
        // Initialize Hangfire configuration
        GlobalConfiguration.Configuration
            .UseStorage(_store)
            .UseIdempotent();
    }
   
    [SetUp]
    public void Setup()
    {
        _store = new MemoryStorage();
        GlobalConfiguration.Configuration.UseStorage(_store);
        _server = new BackgroundJobServer();
        JobStorage.Current = _store;
        _jobClient = new BackgroundJobClient(_store);
    }

    [TearDown]
    public void TearDown()
    {
        _server.Dispose();
    }

    [Test]
    public async Task ErrorJob_ShouldBeRetried()
    {
        var service = new FailSucceedJobService();
        var job = _jobClient.Enqueue(() => service.MightFail());

        JobData jobData;

        do
        {
            await Task.Delay(10);
            jobData = JobStorage.Current.GetConnection().GetJobData(job);
        } while (jobData.State == "Enqueued" ||jobData.State == "Scheduled");
        
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        Assert.That(
            monitoringApi.FailedCount() + monitoringApi.ScheduledCount() + monitoringApi.ProcessingCount(),
            Is.GreaterThan(0),
            "Job should be either failed, scheduled for retry, or currently processing"
        );

        long succeeded;
        do
        {
            await Task.Yield();
            await Task.Delay(10);
            succeeded = monitoringApi.SucceededListCount();
        } while (succeeded == 0);
        
        // After retry, check that the job eventually succeeds
        Assert.That(succeeded, Is.EqualTo(1), "Job should have eventually succeeded after retries");
    }


    
    public class FailSucceedJobService
    {
        private static int _attemptCount = 0;

        [AutomaticRetry(Attempts = 10,DelaysInSeconds = new[] { 1,1,1,1,1 })]
        [IdempotentJob]
        public async Task MightFail()
        {            
            await Task.Delay(50);
            _attemptCount++;
            if (_attemptCount < 2)
            {
                throw new TestException("Simulated failure");
            }
        }
    }


    public class TestException : Exception
    {
        public TestException(string message) : base(message) { }
    }



    [Test]
    public async Task ErroJobs_ShouldBeDedupedWhileRetrying()
    {
        var service = new FailSucceedJobService();
        var job = _jobClient.Enqueue(() => service.MightFail());

        JobData jobData;

        do
        {
            await Task.Delay(10);
            jobData = JobStorage.Current.GetConnection().GetJobData(job);
        } while (jobData.State == "Enqueued" || jobData.State == "Scheduled");

        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        Assert.That(
            monitoringApi.FailedCount() + monitoringApi.ScheduledCount() + monitoringApi.ProcessingCount(),
            Is.GreaterThan(0),
            "Job should be either failed, scheduled for retry, or currently processing"
        );

        _jobClient.Enqueue(() => service.MightFail());
        _jobClient.Enqueue(() => service.MightFail());

        long succeeded;
        do
        {
            await Task.Yield();
            await Task.Delay(10);
            succeeded = monitoringApi.SucceededListCount();
        } while (succeeded == 0);

        // After retry, check that the job eventually succeeds and no other jobs are created
        Assert.That(
            monitoringApi.FailedCount() + monitoringApi.ScheduledCount() + monitoringApi.ProcessingCount(),
            Is.EqualTo(0),
            "No other jobs should be created while retrying"
        );
        Assert.That(succeeded, Is.EqualTo(1), "Job should have eventually succeeded after retries");
    }


    [Test]
    public async Task ErroJobs_ShouldNotBeDedupedAfterFail()
    {
        var service = new ConfigFailService();
        var job = _jobClient.Enqueue(() => service.MightFail());

        Assert.That(job, Is.Not.Null, "Job should not be null");

        JobData jobData;

        do
        {
            await Task.Delay(10);
            jobData = JobStorage.Current.GetConnection().GetJobData(job);
        } while (jobData.State == "Enqueued" || jobData.State == "Scheduled");

        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        Assert.That(
            monitoringApi.FailedCount() + monitoringApi.ScheduledCount() + monitoringApi.ProcessingCount(),
            Is.GreaterThan(0),
            "Job should be either failed, scheduled for retry, or currently processing"
        );

        job = _jobClient.Enqueue(() => service.MightFail());
        Assert.That(job, Is.Null);

        long failed;
        do
        {
            await Task.Yield();
            await Task.Delay(10);
            failed = monitoringApi.FailedCount();
        } while (failed == 0);
        ConfigFailService.AllowSucceed = true;
        ConfigFailService.AttemptCount = 0; // Reset attempt count to allow the job to succeed
        job = _jobClient.Enqueue(() => service.MightFail());
        Assert.That(job, Is.Not.Null, "Job should not be null");
        await Task.Delay(2000);

        // After first job is failed state, allow second job to succeed        
        Assert.That(monitoringApi.FailedCount(), Is.EqualTo(1), "Job should have eventually succeeded after retries");
        Assert.That(monitoringApi.SucceededListCount(), Is.EqualTo(1), "Last job should succeed.");

    }

    public class ConfigFailService
    {
        public static bool AllowSucceed = false;
        public  static int AttemptCount = 0;

        [AutomaticRetry(Attempts = 1, DelaysInSeconds = new[] { 1, 1, 1, 1, 1 })]
        [IdempotentJob]
        public async Task MightFail()
        {
            Console.WriteLine($"Attempt: {AttemptCount++}");
            System.Diagnostics.Debug.WriteLine($"Attempt: {AttemptCount}");
            await Task.Delay(50);
            if(!AllowSucceed) throw new TestException("Simulated failure");
        }
    }
}
