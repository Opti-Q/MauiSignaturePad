using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Widget;

namespace MauiSignaturePad.Platforms.Android
{
    public class ClearingImageView : ImageView
    {
        private Bitmap imageBitmap = null;

        public ClearingImageView(Context context)
            : base(context)
        {
        }

        public ClearingImageView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        public ClearingImageView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
        }

        public override void SetImageBitmap(Bitmap bm)
        {
            base.SetImageBitmap(bm);
            if (imageBitmap != null)
            {
                imageBitmap.Recycle();
                imageBitmap.Dispose();
            }
            imageBitmap = bm;
            GC.Collect();
        }
    }
}
