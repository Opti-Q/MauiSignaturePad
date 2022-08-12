using Microsoft.Maui.Handlers;

namespace MauiSignaturePad.Platforms.Android
{
    public class SignaturePadHandler : ViewHandler<ISignaturePadView, SignaturePadView>
    {
        public SignaturePadHandler(IPropertyMapper mapper, CommandMapper commandMapper = null) : base(mapper, commandMapper)
        {
        }

        protected override SignaturePadView CreatePlatformView() => new(Context);
    }
}
