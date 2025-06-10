using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;

namespace TestHangfireIdempotent;

[TestFixture]
public class DuplicateJobDetectorTests
{
    [Test]
    public void IsDuplicateJob_HandlesNullValues_WithoutThrowing()
    {
        var monitoringApiMock = new Mock<IMonitoringApi>();

        monitoringApiMock.Setup(m => m.EnqueuedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<EnqueuedJobDto?>([
                new KeyValuePair<string, EnqueuedJobDto?>("job1", null ),
                new KeyValuePair<string, EnqueuedJobDto?>("job2", new EnqueuedJobDto { Job = null } ),
                new KeyValuePair<string, EnqueuedJobDto?>("job3", new EnqueuedJobDto { Job = Job.FromExpression(() => Console.WriteLine("Test")) } )
            ]));
            
        monitoringApiMock.Setup(m => m.ScheduledJobs(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<ScheduledJobDto?>([
                new KeyValuePair<string, ScheduledJobDto?>("job1", null),
                new KeyValuePair<string, ScheduledJobDto?>("job2", new ScheduledJobDto { Job = null }),
                new KeyValuePair<string, ScheduledJobDto?>("job3", new ScheduledJobDto { Job = Job.FromExpression(() => Console.WriteLine("Test")) } )
            ]));
            
        monitoringApiMock.Setup(m => m.FetchedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<FetchedJobDto?>([
                new KeyValuePair<string, FetchedJobDto?>("job1", null),
                new KeyValuePair<string, FetchedJobDto?>("job2", new FetchedJobDto { Job = null }),
                new KeyValuePair<string, FetchedJobDto?>("job3", new FetchedJobDto { Job = Job.FromExpression(() => Console.WriteLine("Test")) } )
            ]));
        
        var config = new IdempotentConfiguration { MaxRetrievals = 100 };
        var detector = new DuplicateJobDetector(monitoringApiMock.Object, config);
        var targetJob = Job.FromExpression(() => Console.WriteLine("Different job"));
        
        Assert.DoesNotThrow(() => detector.IsDuplicateJob(targetJob));
    }
    
    [Test]
    public void IsDuplicateJob_FindsDuplicate_ReturnsTrue()
    {
        var targetJob = Job.FromExpression(() => Console.WriteLine("Test"));
        var duplicateJob = Job.FromExpression(() => Console.WriteLine("Test"));
        var monitoringApiMock = new Mock<IMonitoringApi>();

        monitoringApiMock.Setup(m => m.EnqueuedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<EnqueuedJobDto?>([
                new KeyValuePair<string, EnqueuedJobDto?>("job1", new EnqueuedJobDto { Job = duplicateJob })
            ]));
            
        monitoringApiMock.Setup(m => m.ScheduledJobs(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<ScheduledJobDto>([]));
            
        monitoringApiMock.Setup(m => m.FetchedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<FetchedJobDto>([]));
        
        var config = new IdempotentConfiguration { MaxRetrievals = 100 };
        var detector = new DuplicateJobDetector(monitoringApiMock.Object, config);
        
        var result = detector.IsDuplicateJob(targetJob);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public void IsDuplicateJob_NoDuplicateFound_ReturnsFalse()
    {
        var targetJob = Job.FromExpression(() => Console.WriteLine("Test"));
        var differentJob = Job.FromExpression(() => Console.WriteLine("Different"));
        var monitoringApiMock = new Mock<IMonitoringApi>();

        monitoringApiMock.Setup(m => m.EnqueuedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<EnqueuedJobDto>([
                new KeyValuePair<string, EnqueuedJobDto>("job1", new EnqueuedJobDto { Job = differentJob })
            ]));
            
        monitoringApiMock.Setup(m => m.ScheduledJobs(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<ScheduledJobDto>([]));
            
        monitoringApiMock.Setup(m => m.FetchedJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<FetchedJobDto>([]));
        
        var config = new IdempotentConfiguration { MaxRetrievals = 100 };
        var detector = new DuplicateJobDetector(monitoringApiMock.Object, config);
        
        var result = detector.IsDuplicateJob(targetJob);
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsDuplicateJob_DuplicateOnNamedQueue_ReturnsFalse()
    {
        var targetJob = Job.FromExpression(() => Console.WriteLine("Test"), "critical");
        var duplicateJob = Job.FromExpression(() => Console.WriteLine("Test"), "critical");
        
        var monitoringApiMock = new Mock<IMonitoringApi>();
        
        monitoringApiMock.Setup(m => m.EnqueuedJobs("default", It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<EnqueuedJobDto>([]));
        
        // The "critical" queue has our duplicate job
        monitoringApiMock.Setup(m => m.EnqueuedJobs("critical", It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<EnqueuedJobDto?>([
                new KeyValuePair<string, EnqueuedJobDto?>("job1", new EnqueuedJobDto { Job = duplicateJob })
            ]));
            
        monitoringApiMock.Setup(m => m.ScheduledJobs(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<ScheduledJobDto>([]));
            
        monitoringApiMock.Setup(m => m.FetchedJobs("default", It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<FetchedJobDto>([]));
        
        monitoringApiMock.Setup(m => m.FetchedJobs("critical", It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new JobList<FetchedJobDto>([]));
        
        var config = new IdempotentConfiguration { MaxRetrievals = 100 };
        var detector = new DuplicateJobDetector(monitoringApiMock.Object, config);
        
        var result = detector.IsDuplicateJob(targetJob);
        
        Assert.That(result, Is.True);
    }
}
