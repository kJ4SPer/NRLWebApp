using FirstWebApplication.Controllers;
using FirstWebApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace NRLWebApp.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            var mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(mockLogger.Object);

            // FIX: Simuler en IKKE-innlogget bruker (Gjest)
            // Ved å ikke sende med en "authenticationType" string i konstruktøren, 
            // blir User.Identity.IsAuthenticated = false.
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            // Vi må fortsatt ha TempData for å unngå NullReference hvis kontrolleren bruker det
            _controller.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var result = _controller.Index();
            Assert.IsType<ViewResult>(result);
        }
    }
}