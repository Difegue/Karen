using Karen.Services;
using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Karen.Views.Content
{
    public sealed partial class PopupContent : UserControl
    {

        private KarenPopupViewModel Data;

        public PopupContent()
        {
            InitializeComponent();
            Data = Service.Services.GetRequiredService<KarenPopupViewModel>();
        }
    }
}
