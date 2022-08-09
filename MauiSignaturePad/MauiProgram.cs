using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace MauiSignaturePad
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiCompatibility()
                .ConfigureMauiHandlers((handlers) => {
#if ANDROID
                    handlers.AddHandler(typeof(SignaturePadView), typeof(Platforms.Android.SignaturePadHandler));
#elif IOS
                    handlers.AddHandler(typeof(SignaturePadView), typeof(Platforms.iOS.SignaturePadHandler));
#elif WINDOWS
#endif
                });

            return builder.Build();
        }
    }
}
