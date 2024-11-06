
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
#if (DEBUGFORMS || RELEASEFORMS)
using System.Windows.Forms;
#endif

namespace ECommons;
public static partial class GenericHelpers
{
    /// <summary>
    /// Copies text into user's clipboard using WinForms. Does not throws exceptions.
    /// </summary>
    /// <param name="text">Text to copy</param>
    /// <param name="silent">Whether to display success/failure popup</param>
    /// <returns>Whether operation succeeded</returns>
#if !(DEBUGFORMS || RELEASEFORMS)
    [Obsolete("You have selected not to use Windows Forms; copying will be done via ImGui. This has been known to cause serious issues in past. If you are working with clipboard a lot, consider enabling Windows Forms.")]
#endif
    public static bool Copy(string text, bool silent = false)
    {
        try
        {
            if(text.IsNullOrEmpty())
            {
#if (DEBUGFORMS || RELEASEFORMS)
                Clipboard.Clear();
#else
                ImGui.SetClipboardText("");
#endif
                if(!silent) Notify.Success("剪贴板已清空");
            }
            else
            {
#if (DEBUGFORMS || RELEASEFORMS)
                Clipboard.SetText(text);
#else
                ImGui.SetClipboardText(text);
#endif
                if(!silent) Notify.Success("文本已复制到剪贴板");
            }
            return true;
        }
        catch(Exception e)
        {
            if(!silent)
            {
                Notify.Error($"复制到剪贴板时出错:\n{e.Message}\n请重试");
            }
            PluginLog.Warning($"复制到剪贴板时出错:");
            e.LogWarning();
            return false;
        }
    }

    /// <summary>
    /// Reads text from user's clipboard
    /// </summary>
    /// <param name="silent">Whether to display popup when error occurs.</param>
    /// <returns>Contents of the clipboard; null if clipboard couldn't be read.</returns>
#if !(DEBUGFORMS || RELEASEFORMS)
    [Obsolete("You have selected not to use Windows Forms; pasting will be done via ImGui. This has been known to cause serious issues in past. If you are working with clipboard a lot, consider enabling Windows Forms.")]
#endif
    public static string? Paste(bool silent = false)
    {
        try
        {
#if (DEBUGFORMS || RELEASEFORMS)
            return Clipboard.GetText();
#else
            return ImGui.GetClipboardText();
#endif
        }
        catch(Exception e)
        {
            if(!silent)
            {
                Notify.Error($"从剪贴板粘贴时出错:\n{e.Message}\n请重试");
            }
            PluginLog.Warning($"从剪贴板粘贴时出错d:");
            e.LogWarning();
            return null;
        }
    }

#if (DEBUGFORMS || RELEASEFORMS)

    /// <summary>
    /// Checks if a key is pressed via winapi.
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>Whether the key is currently pressed</returns>
    public static bool IsKeyPressed(Keys key) => IsKeyPressed((int)key);


    public static bool IsKeyPressed(IEnumerable<Keys> keys)
    {
        foreach(var x in keys)
        {
            if(IsKeyPressed(x)) return true;
        }
        return false;
    }
#endif
}