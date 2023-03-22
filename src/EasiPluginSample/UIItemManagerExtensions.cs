using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

using Cvte.EasiNote;
using Cvte.Windows.Localization;

namespace EasiPluginSample;

/// <summary>
/// 对 <see cref="IUIItemManager"/> 的扩展，这是辅助代码
/// </summary>
static class UIItemManagerExtensions
{
    /// <summary>
    /// 追加新的 UI Item 项，追加时带上多语言
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="item"></param>
    /// <param name="attribute"></param>
    /// <param name="langInfoList"></param>
    /// <exception cref="NotSupportedException"></exception>
    public static void AppendWithLang(this IUIItemManager manager, UIItem item,
        UIItemAttribute attribute, IList<UIItemLangInfo> langInfoList)
    {
        manager.Append(c => item, attribute);

        string menuName;
        if (attribute.Purposes[0] == UIItemPurposes.BoardEditMenu)
        {
            menuName = "BoardEditContextMenu";
        }
        else
        {
            throw new NotSupportedException();
        }

        var itemLangKey = item.GetType().Name.Replace("MenuItem", "");

        var langKey = $"Lang.{menuName}.{itemLangKey}";

        // 必须在 UI 线程添加多语言，因为多语言许多是和界面相关的
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            foreach (var langInfo in langInfoList)
            {
                Lang.Sources.Add(new DictionaryLanguageSource
                {
                    [langInfo.CultureInfo] = new Dictionary<string, string>()
                    {
                        { langKey, langInfo.LangText },
                    },
                });
            }
        }, DispatcherPriority.Send);
    }
}