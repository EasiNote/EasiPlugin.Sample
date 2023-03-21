using System.Globalization;

namespace EasiPluginSample;

/// <summary>
/// UI 项的多语言信息
/// </summary>
/// <param name="CultureInfo">语言文化，如 <code>new CultureInfo("zh-CHS")</code> 文化</param>
/// <param name="LangText">语言文化的对应内容</param>
readonly record struct UIItemLangInfo(CultureInfo CultureInfo, string LangText)
{
}