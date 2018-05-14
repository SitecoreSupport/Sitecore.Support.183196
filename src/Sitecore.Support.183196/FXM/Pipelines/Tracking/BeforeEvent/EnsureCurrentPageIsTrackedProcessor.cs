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
        public void Process(ITrackingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(args.TrackerProvider, "TrackerProvider");
            Assert.ArgumentNotNull(args.TrackerProvider.Current, "Current tracker provider");
            Assert.ArgumentNotNull(args.TrackingRequest, "TrackingRequest");
            if (args.CurrentPageVisit != null)
            {
                return;
            }
            if (!args.TrackerProvider.Current.IsActive && args.TrackerProvider.Current.Interaction == null)
            {
                args.TrackerProvider.Current.StartTracking();
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

        protected IPageContext GetCurrentPageInInteraction(CurrentInteraction interaction, Uri trackedPageUrl)
        {
            IPageContext result = interaction.CurrentPage;
            if (interaction.CurrentPage == null)
            {
                List<IPageContext> source = interaction.GetPages().ToList<IPageContext>();
                if (!source.Any<IPageContext>())
                {
                    result = null;
                }
                else
                {
                    result = source.FirstOrDefault((IPageContext p) => string.Format("{0}{1}", p.Url.Path, p.Url.QueryString) == trackedPageUrl.PathAndQuery);
                }
            }
            return result;
        }
    }
}
