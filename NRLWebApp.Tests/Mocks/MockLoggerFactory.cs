using Microsoft.Extensions.Logging;
using Moq;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockLoggerFactory
    {
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public static Mock<ILogger> CreateMockLogger()
        {
            return new Mock<ILogger>();
        }
    }
}