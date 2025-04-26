using Hangfire.MemoryStorage;

namespace TestHangfireIdempotent
{
    internal class BackgroundJobsTests
    {
        private BackgroundJobServer server;

        static BackgroundJobsTests()
        {
            // Initialize Hangfire configuration
            GlobalConfiguration.Configuration
                .UseMemoryStorage()
                .UseIdempotent();
        }

        [SetUp]
        public void Setup()
        {
            server = new BackgroundJobServer();
        }

        [Test]
        public void TestAddJob()
        {
            // Act
            var job = BackgroundJob.Enqueue(() => Console.WriteLine());
            var jobData = JobStorage.Current.GetConnection().GetJobData(job);


            // Assert
            Assert.That(job, Is.Not.Null);
            Assert.That(jobData, Is.Not.Null);
        }

        [Test]
        public void TestAddJobWithIdempotent()
        {
            // Act
            var job = BackgroundJob.Enqueue(() => IdempotentJob());
            // Assert
            Assert.Pass();
        }

        [Test]
        public void TestAddDuplicateJobWithIdempotent()
        {

            // Act
            var job1 = BackgroundJob.Enqueue(() => IdempotentJob());
            Assert.That(job1, Is.Not.Null);
            var job2 = BackgroundJob.Enqueue(() => IdempotentJob());

            // Assert
            Assert.That(job2, Is.Null);
        }

        [Test]
        public async Task TestAwaitAddDuplicateJobWithIdempotent()
        {

            // Act
            var job1 = BackgroundJob.Enqueue(() => IdempotentJob());

            var connection = JobStorage.Current.GetConnection();

            var jobData = connection.GetJobData(job1);
            Assert.That(jobData, Is.Not.Null);

            // Wait for the job to complete
            string state = "";
            do
            {
                await Task.Delay(5000);

                state = connection.GetJobData(job1).State;
            }
            while (state != "Succeeded");


            var job2 = BackgroundJob.Enqueue(() => IdempotentJob());

            //server.WaitForShutdown(TimeSpan.FromSeconds(1));            

            // Assert
            Assert.That(job2, Is.Not.Null);
        }



        [IdempotentJob]
        public void IdempotentJob()
        {
            Task.Delay(50).Wait();
            Console.WriteLine("Executing Idempotent Job");
        }

        [TearDown]
        public void TearDown()
        {
            server.WaitForShutdown(TimeSpan.FromSeconds(1));
            server.Dispose();
        }
    }
}
