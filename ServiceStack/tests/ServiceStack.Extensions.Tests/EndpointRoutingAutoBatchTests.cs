#nullable enable
#if NET8_0_OR_GREATER

using Funq;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Extensions.Tests;

public class EndpointRoutingAutoBatchAppHost() : AppHostBase(nameof(EndpointRoutingAutoBatchTests), typeof(EndpointRoutingAutoBatchIndexServices).Assembly)
{


    public override void Configure(Container container)
    {
        GlobalRequestFilters.Add((req, res, dto) =>
        {
            var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();
            if (autoBatchIndex != null)
            {
                res.RemoveHeader("GlobalRequestFilterAutoBatchIndex");
                res.AddHeader("GlobalRequestFilterAutoBatchIndex", autoBatchIndex);
            }
        });

        GlobalResponseFilters.Add((req, res, dto) =>
        {
            var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();

            if (autoBatchIndex != null)
            {
                if (dto is IMeta response)
                {
                    response.Meta = new Dictionary<string, string>
                    {
                        ["GlobalResponseFilterAutoBatchIndex"] = autoBatchIndex
                    };
                }
            }
        });
    }
}

[Route("/GetAutoBatchER")]
public class GetEndpointRoutingAutoBatchIndex : IReturn<GetAutoBatchIndexResponseER>
{
}

[Route("/GetCustomAutoBatchER")]
public class GetCustomAutoBatchIndexER : IReturn<GetAutoBatchIndexResponseER>
{
}

public class GetAutoBatchIndexResponseER : IMeta
{
    public string Index { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public class EndpointRoutingAutoBatchIndexServices : Service
{
    public object Any(GetEndpointRoutingAutoBatchIndex request)
    {
        var autoBatchIndex = Request.GetItem(Keywords.AutoBatchIndex)?.ToString();
        return new GetAutoBatchIndexResponseER
        {
            Index = autoBatchIndex
        };
    }

    public GetAutoBatchIndexResponseER Any(GetCustomAutoBatchIndexER request)
    {
        var autoBatchIndex = Request.GetItem(Keywords.AutoBatchIndex)?.ToString();
        return new GetAutoBatchIndexResponseER
        {
            Index = autoBatchIndex
        };
    }

    public object Any(GetCustomAutoBatchIndexER[] requests)
    {
        var responses = new List<GetAutoBatchIndexResponseER>();

        Request.EachRequest<GetCustomAutoBatchIndexER>(dto =>
        {
            responses.Add(Any(dto));
        });

        return responses;
    }
}

[TestFixture]
public class EndpointRoutingAutoBatchTests
{
    private ServiceStackHost appHost;
    private JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        var contentRootPath = "~/../../../".MapServerPath();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRootPath,
            WebRootPath = contentRootPath,
        });
        var services = builder.Services;

        services.AddServiceStack(typeof(EndpointRoutingAutoBatchIndexServices).Assembly);

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseServiceStack(new EndpointRoutingAutoBatchAppHost(), options => { options.MapEndpoints(use: true, force: true); });
        app.StartAsync(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown() => AppHostBase.DisposeApp();

    [Test]
    public void Single_requests_dont_set_AutoBatchIndex()
    {
        var client = new JsonServiceClient(TestsConfig.ListeningOn)
        {
            UseBasePath = "/api"
        };
        var httpClient = client.HttpClient;
        httpClient.Timeout = TimeSpan.FromMilliseconds(-1); 

        WebHeaderCollection responseHeaders = null;

        client.ResponseFilter = resp => { responseHeaders = resp.Headers; };

        var response = client.Send(new GetEndpointRoutingAutoBatchIndex());

        Assert.Null(response.Index);
        Assert.Null(response.Meta);
        Assert.IsFalse(responseHeaders.AllKeys.Contains("GlobalRequestFilterAutoBatchIndex"));
    }

    [Test]
    public void Multi_requests_set_AutoBatchIndex()
    {
        var client = new JsonServiceClient(TestsConfig.ListeningOn)
        {
            UseBasePath = "/api"
        };
        var httpClient = client.HttpClient;
        httpClient.Timeout = TimeSpan.FromMilliseconds(-1);

        WebHeaderCollection responseHeaders = null;

        client.ResponseFilter = response => { responseHeaders = response.Headers; };

        var responses = client.SendAll(new[]
        {
            new GetEndpointRoutingAutoBatchIndex(),
            new GetEndpointRoutingAutoBatchIndex()
        });

        Assert.AreEqual("0", responses[0].Index);
        Assert.AreEqual("0", responses[0].Meta["GlobalResponseFilterAutoBatchIndex"]);

        Assert.AreEqual("1", responses[1].Index);
        Assert.AreEqual("1", responses[1].Meta["GlobalResponseFilterAutoBatchIndex"]);

        Assert.AreEqual("1", responseHeaders["GlobalRequestFilterAutoBatchIndex"]);
    }

    [Test]
    public void Custom_multi_requests_set_AutoBatchIndex()
    {
        var client = new JsonServiceClient(TestsConfig.ListeningOn)
        {
            UseBasePath = "/api"
        };

        WebHeaderCollection responseHeaders = null;

        client.ResponseFilter = response => { responseHeaders = response.Headers; };

        var responses = client.SendAll(new[]
        {
            new GetCustomAutoBatchIndexER(),
            new GetCustomAutoBatchIndexER()
        });

        Assert.AreEqual("0", responses[0].Index);
        Assert.AreEqual("0", responses[0].Meta["GlobalResponseFilterAutoBatchIndex"]);

        Assert.AreEqual("1", responses[1].Index);
        Assert.AreEqual("1", responses[1].Meta["GlobalResponseFilterAutoBatchIndex"]);

        Assert.AreEqual("1", responseHeaders["GlobalRequestFilterAutoBatchIndex"]);
    }
}

#endif