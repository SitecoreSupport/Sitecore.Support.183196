using Sitecore.Abstractions;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using Sitecore.FXM.Abstractions;
using Sitecore.FXM.Pipelines.Tracking;
using Sitecore.FXM.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.FXM.Pipelines.Tracking.BeforeEvent
{
    public class EnsureCurrentPageIsTrackedProcessor : ITrackingProcessor, ITrackingProcessor<ITrackingArgs>
    {
        protected IPageContext GetCurrentPageInInteraction(CurrentInteraction interaction, Uri trackedPageUrl)
        {
            Func<IPageContext, bool> predicate = null;
            IPageContext currentPage = interaction.CurrentPage;
            if (interaction.CurrentPage != null)
            {
                return currentPage;
            }
            List<IPageContext> source = interaction.GetPages().ToList<IPageContext>();
            if (!source.Any<IPageContext>())
            {
                return null;
            }
            if (predicate == null)
            {
                predicate = p => string.Format("{0}{1}", p.Url.Path, p.Url.QueryString) == trackedPageUrl.PathAndQuery;
            }
            return source.FirstOrDefault<IPageContext>(predicate);
        }

        public void Process(ITrackingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(args.TrackerProvider, "TrackerProvider");
            Assert.ArgumentNotNull(args.TrackerProvider.Current, "Current tracker provider");
            Assert.ArgumentNotNull(args.TrackingRequest, "TrackingRequest");
            if (args.CurrentPageVisit == null)
            {
                if (!args.TrackerProvider.Current.IsActive && (args.TrackerProvider.Current.Interaction == null))
                {
                    #region---modified part of the code to track page visit/event directly via FXM trackingManager
                    TrackingManager trackingManager = new TrackingManager(new CorePipelineWrapper(), new TrackerProviderWrapper(), new SitecoreContextWrapper());
                    HttpRequestBase httpRequestBase = new HttpRequestWrapper(HttpContext.Current.Request);
                    HttpResponseBase httpResponseBase = new HttpResponseWrapper(HttpContext.Current.Response);
                    SpoofedHttpRequestBase spoofedHttpRequestBase = new SpoofedHttpRequestBase(httpRequestBase);
                    PageVisitParameters pageVisitParameters = new PageVisitParameters(args.TrackingRequest.Url, spoofedHttpRequestBase.UrlReferrer, args.TrackerProvider.Current.Contact.ContactId.ToString());
                    trackingManager.TrackPageVisit(httpRequestBase, httpResponseBase, pageVisitParameters);
                    #endregion
                }
                if (args.TrackerProvider.Current.Interaction != null)
                {
                    IPageContext currentPageInInteraction = this.GetCurrentPageInInteraction(args.TrackerProvider.Current.Interaction, args.TrackingRequest.Url);
                    if (currentPageInInteraction != null)
                    {
                        args.CurrentPageVisit = currentPageInInteraction;
                    }
                }
                if (args.CurrentPageVisit == null)
                {
                    args.AbortAndFailPipeline("the current page has not been tracked in the current session.", TrackingResultCode.CurrentPageMustBeTracked);
                }
            }
        }
    }
}
