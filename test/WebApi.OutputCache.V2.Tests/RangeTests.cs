using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Moq;
using NUnit.Framework;
using WebApi.OutputCache.Core;
using WebApi.OutputCache.Core.Cache;
using WebApi.OutputCache.V2.Tests.TestControllers;

namespace WebApi.OutputCache.V2.Tests
{
    [TestFixture]
    public class RangeTests
    {
        private HttpServer _server;
        private string _url = "http://www.strathweb.com/api/range/";
        private Mock<IApiOutputCache> _cache;

        [SetUp]
        public void init()
        {
            Thread.CurrentPrincipal = null;

            _cache = new Mock<IApiOutputCache>();

            var conf = new HttpConfiguration();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_cache.Object);

            conf.DependencyResolver = new AutofacWebApiDependencyResolver(builder.Build());
            conf.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            _server = new HttpServer(conf);
        }

        [Test]
        public void not_cache_when_invalid_range_unit()
        {
            var client = new HttpClient(_server);

            var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get1000_c100_s100");
            req.Headers.Range = new RangeHeaderValue(0, 10) { Unit = "Item" };

            var result = client.SendAsync(req).Result;

            _cache.Verify(s => s.Contains(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:application/json; charset=utf-8")), Times.Never());
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), null), Times.Never());
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:application/json; charset=utf-8"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get1000_c100_s100")), Times.Never());
        }

        [Test]
        public void cache_when_no_range_hearder()
        {
            var client = new HttpClient(_server);

            var result = client.GetAsync(_url + "Get1000_c100_s100").Result;

            _cache.Verify(s => s.Contains(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:application/json; charset=utf-8")), Times.Exactly(2));
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), null), Times.Once());
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:application/json; charset=utf-8"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100")), Times.Once());
        }

        [Test]
        public void cache_when_range_headers()
        {
            var client = new HttpClient(_server);

            var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get1000_c100_s100");
            req.Headers.Range = new RangeHeaderValue(0, 10) { Unit = EnableRangeAttribute.EnityRangeUnit };

            var result = client.SendAsync(req).Result;

            _cache.Verify(s => s.Contains(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=0-10:application/json; charset=utf-8")), Times.Exactly(2));
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), null), Times.Once());
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=0-10:application/json; charset=utf-8"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100")), Times.Once());
        }

        [Test]
        public void cache_when_diff_range()
        {
            var client = new HttpClient(_server);

            var req1 = new HttpRequestMessage(HttpMethod.Get, _url + "Get1000_c100_s100");
            req1.Headers.Range = new RangeHeaderValue(0, 10) { Unit = EnableRangeAttribute.EnityRangeUnit };
            var result1 = client.SendAsync(req1).Result;

            var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Get1000_c100_s100");
            req2.Headers.Range = new RangeHeaderValue(10, 20) { Unit = EnableRangeAttribute.EnityRangeUnit };
            var result2 = client.SendAsync(req2).Result;

            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), null), Times.Exactly(2));

            _cache.Verify(s => s.Contains(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=0-10:application/json; charset=utf-8")), Times.Exactly(2));
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=0-10:application/json; charset=utf-8"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100")), Times.Once());

            _cache.Verify(s => s.Contains(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=10-20:application/json; charset=utf-8")), Times.Exactly(2));
            _cache.Verify(s => s.Add(It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100:x-entity=10-20:application/json; charset=utf-8"), It.IsAny<object>(), It.Is<DateTimeOffset>(x => x <= DateTime.Now.AddSeconds(100)), It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.rangecontroller-get1000_c100_s100")), Times.Once());
        }





        [TearDown]
        public void fixture_dispose()
        {
            if (_server != null) _server.Dispose();
        }
    }
}