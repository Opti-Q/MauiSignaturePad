namespace MauiSignaturePad
{
    public interface ISignaturePadView : IView
    {
        public bool IsBlank { get; set; }
        public string CaptionText { get; set; }
        public Color CaptionTextColor { get; set; }
        public string ClearText { get; set; }
        public Color ClearTextColor { get; set; }
        public string PromptText { get; set; }
        public Color PromptTextColor { get; set; }
        public Color SignatureLineColor { get; set; }
        public float StrokeWidth { get; set; }
        public Color StrokeColor { get; set; }
        public IEnumerable<Point> Points { get; set; }

        public Task<Stream> GetImageStreamAsync(SignatureImageFormat imageFormat);
    }
}
