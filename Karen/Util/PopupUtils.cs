using Karen.Views.Dialogs;
using System.Threading.Tasks;

namespace Karen.Util;

public static class PopupUtils
{

    public static Task ShowMessageDialog(string title, string content, string button)
    {
        return new GenericDialog(title, content, button).ShowAsync();
    }

}

