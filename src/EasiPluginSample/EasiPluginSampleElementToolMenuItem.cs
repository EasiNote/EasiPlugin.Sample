using System;
using System.Threading;
using System.Threading.Tasks;
using Cvte.EasiNote;
using Cvte.Windows.Input;
using dotnetCampus.EasiPlugins;

namespace EasiPluginSample
{
    /// <summary>
    /// 右键菜单的示例
    /// </summary>
    public class EasiPluginSampleElementToolMenuItem : BoardEditMenuItem
    {
        public EasiPluginSampleElementToolMenuItem()
        {
            // 菜单的排序状态
            SortHint = 50;
            // 菜单执行的命令
            Command = new DelegateCommand(() =>
            {
                // 上报埋点内容，上报的内容只用于希沃后台分析
                SafeEN.Collection.ReportEvent(EventId.SampleEventId, "");

                // 开启 Loading 框，执行耗时任务时，开启 Loading 框即可用来告诉用户正在后台执行耗时任务，需要进行等待
                EN.Notification.DoWithLoadingAsync(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));

                }, "工作中。。。", new CancellationTokenSource());
            });
            // 菜单可见的条件
            Predicate = /*选中的元素*/ elements => elements.Count == 0;
        }
    }
}