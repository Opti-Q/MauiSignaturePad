using Microsoft.Maui.Controls.Compatibility.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    handlers.AddCompatibilityRenderer(typeof(SignaturePadView), typeof(SignaturePadRenderer));
                });

            return builder.Build();
        }
    }
}
