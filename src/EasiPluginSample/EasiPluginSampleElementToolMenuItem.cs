using System;
using System.Threading;
using System.Threading.Tasks;
using Cvte.EasiNote;
using Cvte.Windows.Input;
using dotnetCampus.EasiPlugins;

namespace EasiPluginSample
{
    public class EasiPluginSampleElementToolMenuItem : BoardEditMenuItem
    {
        public EasiPluginSampleElementToolMenuItem()
        {
            SortHint = 50;
            Command = new DelegateCommand(() =>
            {
                // 上报埋点内容，上报的内容只用于希沃后台分析
                SafeEN.Collection.ReportEvent(EventId.SampleEventId, "");

                EN.Notification.DoWithLoadingAsync(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));

                }, "工作中。。。", new CancellationTokenSource());
            });
            Predicate = elements => elements.Count == 0;
        }
    }
}