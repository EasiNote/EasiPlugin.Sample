using System;
using System.Globalization;
using System.Threading.Tasks;
using Cvte.Composition;
using Cvte.EasiNote;

namespace EasiPluginSample
{
    class Program : dotnetCampus.EasiPlugins.EasiPlugin
    {
        protected override Task OnRunningAsync()
        {
            // 当插件被希沃白板加载时，此处的代码将会执行
            if (EN.CommandOptions.IsCloud)
            {
                // 云课件逻辑
            }
            else
            {
                // Shell 逻辑
                if (EN.App.IsReady)
                {
                    Run();
                }
                else
                {
                    EN.App.Ready += App_Ready;
                }
            }

            return Task.CompletedTask;
        }

        private void App_Ready(object? sender, EventArgs e)
        {
            EN.App.Ready -= App_Ready;
            Run();
        }

        private async void Run()
        {
            // 无需立刻执行的逻辑，可选延迟
            await Task.Delay(TimeSpan.FromSeconds(3));

            // 导出 UI 项
            ExportUIItems();
        }

        private void ExportUIItems()
        {
            // 添加 UI 扩展。
            var manager = Container.Current.Get<IUIItemManager>();
            manager.AppendWithLang(new EasiPluginSampleElementToolMenuItem(),
                new UIItemAttribute(UIItemPurposes.BoardEditMenu), new[]
                {
                    // 为 UI 扩展添加语言项。
                    new UIItemLangInfo(new CultureInfo("zh-CHS"), "演示工具")
                });
        }
    }
}