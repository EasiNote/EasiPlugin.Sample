using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cvte.EasiNote;
using Cvte.Paint.Features.Elements;
using Cvte.Paint.Features.Elements.Texts;
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
                    GetTextBounds();

                    // 模拟执行耗时任务
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }, "工作中。。。", new CancellationTokenSource());
            });
            // 菜单可见的条件
            Predicate = /*选中的元素*/ elements => elements.Count == 0;
        }

        /// <summary>
        /// 获取文本里面某个字符的相对于 Slide 页面的范围
        /// </summary>
        private void GetTextBounds()
        {
            // 获取备课下的当前页面的首个文本元素
            var currentSlide = EN.EditingBoardApi.CurrentSlide;
            // 如需获取所有页面，请使用 EN.EditingBoardApi.Slides 属性

            // 文本元素对应的是 TextElement 类型
            var firstTextElement = currentSlide.Elements.OfType<TextElement>().FirstOrDefault();

            if (firstTextElement is not null)
            {
                // 如果能拿到文本元素，先获取文本里面有多少个字符
                // 文本有两个坐标系，一个是字符坐标，一个是光标坐标
                // 文本坐标系是通用的文本知识，还请自行了解。遍历整个文本的所有字符，获取每个字符的范围，可使用如下代码
                var charCount = firstTextElement.TextEditor.CharCount;
                Point newTextElementPosition = default;
                for (int i = 0; i < charCount; i++)
                {
                    // 获取每个字符相对于文本元素的坐标
                    var bounds = firstTextElement.TextEditor.GetRunBoundsByDocumentOffset(i);

                    // 通过 WPF 的坐标系转换方法可以转换为页面坐标系
                    var charTopLeftInSlide = firstTextElement.TextEditor.TranslatePoint(bounds.TopLeft,currentSlide);

                    // 在页面坐标系基础上，修改当前元素的坐标
                    // 先使用有趣的算法算出新的坐标，以下代码是随便写的算法
                    newTextElementPosition = charTopLeftInSlide with {X = (charTopLeftInSlide.X + newTextElementPosition.X)/2 };
                }

                // 设置当前元素的新范围
                firstTextElement.Bounds = new Rect(newTextElementPosition, firstTextElement.Bounds.Size);
            }

            foreach (var groupElement in currentSlide.Elements.OfType<GroupElement>())
            {
                foreach (var textElement in groupElement.Elements.OfType<TextElement>())
                {
                    // 这是获取组合内元素的例子
                }
            }
        }
    }
}