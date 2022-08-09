using Android.Content;
using Android.Graphics;
using Android.Util;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using View = Android.Views.View;

namespace MauiSignaturePad.Platforms.Android
{
    public class SignatureCanvasView : View
    {
        private Path currentPath;
        private Paint currentPaint;

        public SignatureCanvasView(Context context) :
            base(context)
        {
            Initialize();
        }

        public SignatureCanvasView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public SignatureCanvasView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        void Initialize()
        {
        }

        public Path Path
        {
            set { this.currentPath = value; }
        }

        public Paint Paint
        {
            set { this.currentPaint = value; }
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (this.currentPath == null || this.currentPath.IsEmpty)
                return;

            canvas.DrawPath(this.currentPath, this.currentPaint);
        }
    }
}
