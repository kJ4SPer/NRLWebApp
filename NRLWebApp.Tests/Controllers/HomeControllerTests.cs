using FirstWebApplication.Controllers;
using FirstWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace NRLWebApp.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(mockLogger.Object);
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var result = _controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewWithRequestId()
        {
            var result = _controller.Error();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }
    }
}