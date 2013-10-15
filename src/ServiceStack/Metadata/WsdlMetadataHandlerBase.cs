using System;
using System.Web;
using ServiceStack.Host;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public abstract class WsdlMetadataHandlerBase : HttpHandlerBase
    {
        private readonly ILog log = LogManager.GetLogger(typeof(WsdlMetadataHandlerBase));

        protected abstract WsdlTemplateBase GetWsdlTemplate();

        public override void Execute(HttpContext context)
        {
            HostContext.AppHost.AssertFeatures(Feature.Metadata);

            context.Response.ContentType = "text/xml";

            var baseUri = context.Request.GetParentBaseUrl();
            var optimizeForFlash = context.Request.QueryString["flash"] != null;
            var operations = new XsdMetadata(
                HostContext.Metadata, flash: optimizeForFlash);

            try
            {
                var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, context.Request.GetBaseUrl(), HostContext.Config.SoapServiceName);
                context.Response.Write(wsdlTemplate.ToString());
            }
            catch (Exception ex)
            {
                log.Error("Autogeneration of WSDL failed.", ex);

                context.Response.Write("Autogenerated WSDLs are not supported "
                    + (Env.IsMono ? "on Mono" : "with this configuration"));
            }
        }

        public void Execute(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            HostContext.AppHost.AssertFeatures(Feature.Metadata);

            httpRes.ContentType = "text/xml";

            var baseUri = httpReq.GetParentBaseUrl();
            var optimizeForFlash = httpReq.QueryString["flash"] != null;
            var operations = new XsdMetadata(HostContext.Metadata, flash: optimizeForFlash);

            try
            {
                var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, httpReq.ResolveBaseUrl(), HostContext.Config.SoapServiceName);
                httpRes.Write(wsdlTemplate.ToString());
            }
            catch (Exception ex)
            {
                log.Error("Autogeneration of WSDL failed.", ex);

                httpRes.Write("Autogenerated WSDLs are not supported "
                    + (Env.IsMono ? "on Mono" : "with this configuration"));
            }
        }

        public WsdlTemplateBase GetWsdlTemplate(XsdMetadata operations, string baseUri, bool optimizeForFlash, string rawUrl, string serviceName)
        {
            var xsd = new XsdGenerator
            {
                OperationTypes = operations.GetAllTypes(),
                OptimizeForFlash = optimizeForFlash,
            }.ToString();

            var soapFormat = GetType().Name.StartsWith("Soap11", StringComparison.OrdinalIgnoreCase)
                ? Format.Soap11 : Format.Soap12;

            var wsdlTemplate = GetWsdlTemplate();
            wsdlTemplate.Xsd = xsd;
            wsdlTemplate.ServiceName = serviceName;
            wsdlTemplate.ReplyOperationNames = operations.GetReplyOperationNames(soapFormat);
            wsdlTemplate.OneWayOperationNames = operations.GetOneWayOperationNames(soapFormat);

            if (rawUrl.ToLower().StartsWith(baseUri))
            {
                wsdlTemplate.ReplyEndpointUri = rawUrl;
                wsdlTemplate.OneWayEndpointUri = rawUrl;
            }
            else
            {
                var suffix = soapFormat == Format.Soap11 ? "soap11" : "soap12";
                wsdlTemplate.ReplyEndpointUri = baseUri + suffix;
                wsdlTemplate.OneWayEndpointUri = baseUri + suffix;
            }

            return wsdlTemplate;
        }
    }
}