using Karen.Views.Dialogs;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Karen.Util;

public static class WinUIUtils
{

    public static Task ShowMessageDialog(string title, string content, string button)
    {
        return new GenericDialog(title, content, button).ShowAsync();
    }

    public static void ShowNotification(string title, string content)
    {
        var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(content)
                    .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

}

