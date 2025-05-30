using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Karen.Services
{
    public static class Service
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static void BuildServices()
        {
            var collection = new ServiceCollection();

            collection.AddSingleton<Settings>();
            collection.AddSingleton<Server>();

            collection.AddTransient<MainWindowViewModel>();
            collection.AddTransient<KarenPopupViewModel>();

            Services = collection.BuildServiceProvider();
        }

        public static Settings Settings => Services.GetRequiredService<Settings>();
        public static Server Server => Services.GetRequiredService<Server>();
    }
}
