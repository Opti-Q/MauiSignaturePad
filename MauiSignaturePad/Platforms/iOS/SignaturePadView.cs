using CoreGraphics;
using Foundation;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UIKit;

namespace MauiSignaturePad.Platforms.iOS
{
    public class SignaturePadView : UIView
    {
        const int ThinPad = 3;
        const int ThickPad = 10;
        const int LineHeight = 1;

        UIImageView imageView;
        UIBezierPath currentPath;
        List<UIBezierPath> paths;
        List<CGPoint> currentPoints;
        List<CGPoint[]> points;

        //Used to determine rectangle that needs to be redrawn.
        NFloat minX, minY, maxX, maxY;

        public event EventHandler<bool> IsBlankChanged;

        //Create an array containing all of the points used to draw the signature.  Uses CGPoint.Empty
        //to indicate a new line.
        public CGPoint[] Points
        {
            get
            {
                if (points == null || points.Count() == 0)
                    return new CGPoint[0];

                IEnumerable<CGPoint> pointsList = points[0];

                for (var i = 1; i < points.Count; i++)
                {
                    pointsList = pointsList.Concat(new[] { CGPoint.Empty });
                    pointsList = pointsList.Concat(points[i]);
                }

                return pointsList.ToArray();
            }
        }

        public bool IsBlank
        {
            get { return points == null || points.Count() == 0 || !(points.Where(p => p.Any()).Any()); }
        }

        UIColor strokeColor;
        [Export("StrokeColor"), Browsable(true)]
        public UIColor StrokeColor
        {
            get { return strokeColor; }
            set
            {
                strokeColor = value ?? strokeColor;
                if (!IsBlank)
                    imageView.Image = GetImage(false);
            }
        }

        float strokeWidth;
        [Export("StrokeWidth"), Browsable(true)]
        public float StrokeWidth
        {
            get { return strokeWidth; }
            set
            {
                strokeWidth = value;
                if (!IsBlank)
                    imageView.Image = GetImage(false);
            }
        }

        /// <summary>
        /// The prompt displayed at the beginning of the signature line.
        /// </summary>
        /// <remarks>
        /// Text value defaults to 'X'.
        /// </remarks>
        /// <value>The signature prompt.</value>
        public UILabel SignaturePrompt
        {
            get;
            private set;
        }

        /// <summary>
        /// The caption displayed under the signature line.
        /// </summary>
        /// <remarks>
        /// Text value defaults to 'Sign here.'
        /// </remarks>
        /// <value>The caption.</value>
        public UILabel Caption
        {
            get;
            private set;
        }

        /// <summary>
        /// The color of the signature line.
        /// </summary>
        /// <value>The color of the signature line.</value>
        [Export("SignatureLineColor"), Browsable(true)]
        public UIColor SignatureLineColor
        {
            get { return SignatureLine.BackgroundColor; }
            set { SignatureLine.BackgroundColor = value; }
        }

        /// <summary>
        ///  An image view that may be used as a watermark or as a texture
        ///  for the signature pad.
        /// </summary>
        /// <value>The background image view.</value>
        public UIImageView BackgroundImageView { get; private set; }

        /// <summary>
        ///  An image view that may be used as a watermark or as a texture
        ///  for the signature pad.
        /// </summary>
        /// <value>The background image.</value>
        [Export("BackgroundImage"), Browsable(true)]
        public UIImage BackgroundImage
        {
            get { return BackgroundImageView.Image; }
            set { BackgroundImageView.Image = value; }
        }

        /// <summary>
        ///  An image view that may be used as a watermark or as a texture
        ///  for the signature pad.
        /// </summary>
        /// <value>The background image.</value>
        [Export("BackgroundImageContentMode"), Browsable(true)]
        public UIViewContentMode BackgroundImageContentMode
        {
            get { return BackgroundImageView.ContentMode; }
            set { BackgroundImageView.ContentMode = value; }
        }

        /// <summary>
        ///  The transparency of the watermark.
        /// </summary>
        /// <value>The background image.</value>
        [Export("BackgroundImageAlpha"), Browsable(true)]
        public NFloat BackgroundImageAlpha
        {
            get { return BackgroundImageView.Alpha; }
            set { BackgroundImageView.Alpha = value; }
        }

        /// <summary>
        /// The text for the prompt displayed at the beginning of the signature line.
        /// </summary>
        /// <remarks>
        /// Text value defaults to 'X'.
        /// </remarks>
        /// <value>The signature prompt.</value>
        [Export("SignaturePromptText"), Browsable(true)]
        public string SignaturePromptText
        {
            get { return SignaturePrompt.Text; }
            set
            {
                SignaturePrompt.Text = value;
                SetNeedsLayout();
            }
        }

        /// <summary>
        /// The text for the caption displayed under the signature line.
        /// </summary>
        /// <remarks>
        /// Text value defaults to 'Sign here.'
        /// </remarks>
        /// <value>The caption.</value>
        [Export("CaptionText"), Browsable(true)]
        public string CaptionText
        {
            get { return Caption.Text; }
            set { Caption.Text = value; }
        }

        /// <summary>
        /// Gets the text for the label that clears the pad when clicked.
        /// </summary>
        /// <value>The clear label.</value>
        [Export("ClearLabelText"), Browsable(true)]
        public string ClearLabelText
        {
            get { return ClearLabel.Title(UIControlState.Normal); }
            set
            {
                ClearLabel.SetTitle(value, UIControlState.Normal);
                SetNeedsLayout();
            }
        }

        /// <summary>
        /// Gets the label that clears the pad when clicked.
        /// </summary>
        /// <value>The clear label.</value>
        public UIButton ClearLabel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the horizontal line that goes in the lower part of the pad.
        /// </summary>
        /// <value>The signature line.</value>
        public UIView SignatureLine
        {
            get;
            private set;
        }

        public SignaturePadView()
        {
            Initialize();
        }

        public SignaturePadView(NSCoder coder) : base(coder)
        {
            Initialize(/* ? baseProperties: false ? */);
        }

        public SignaturePadView(IntPtr ptr) : base(ptr)
        {
            Initialize(false);
        }

        public SignaturePadView(CGRect frame)
        {
            Frame = frame;
            Initialize();
        }

        void Initialize(bool baseProperties = true)
        {
            if (baseProperties)
            {
                BackgroundColor = UIColor.FromRGB(225, 225, 225);
            }
            strokeColor = UIColor.Black;
            StrokeWidth = 2f;

            Layer.ShadowColor = UIColor.Black.CGColor;
            Layer.ShadowOffset = new CGSize(2, 2);
            Layer.ShadowOpacity = 1f;
            Layer.ShadowRadius = 2f;

            #region Add Subviews
            BackgroundImageView = new UIImageView();
            AddSubview(BackgroundImageView);

            //Add an image that covers the entire signature view, used to display already drawn
            //elements instead of having to redraw them every time the user touches the screen.
            imageView = new UIImageView();
            AddSubview(imageView);

            Caption = new UILabel();
            Caption.Text = "Sign here.";
            Caption.Font = UIFont.BoldSystemFontOfSize(11f);
            Caption.BackgroundColor = UIColor.Clear;
            Caption.TextColor = UIColor.Gray;
            Caption.TextAlignment = UITextAlignment.Center;
            AddSubview(Caption);

            //Display the base line for the user to sign on.
            SignatureLine = new UIView();
            SignatureLine.BackgroundColor = UIColor.Gray;
            AddSubview(SignatureLine);

            //Display the X on the left hand side of the line where the user signs.
            SignaturePrompt = new UILabel();
            SignaturePrompt.Text = "X";
            SignaturePrompt.Font = UIFont.BoldSystemFontOfSize(20f);
            SignaturePrompt.BackgroundColor = UIColor.Clear;
            SignaturePrompt.TextColor = UIColor.Gray;
            AddSubview(SignaturePrompt);

            ClearLabel = UIButton.FromType(UIButtonType.Custom);
            ClearLabel.SetTitle("Clear", UIControlState.Normal);
            ClearLabel.TitleLabel.Font = UIFont.BoldSystemFontOfSize(11f);
            ClearLabel.BackgroundColor = UIColor.Clear;
            ClearLabel.SetTitleColor(UIColor.Gray, UIControlState.Normal);
            //btn_clear.SetBackgroundImage (UIImage.FromFile ("Images/closebox.png"), UIControlState.Normal);
            //btn_clear.SetBackgroundImage (UIImage.FromFile ("Images/closebox_pressed.png"), 
            //                             UIControlState.Selected);
            ClearLabel.TouchUpInside += (sender, e) => {
                Clear();
            };
            AddSubview(ClearLabel);
            ClearLabel.Hidden = true;
            #endregion

            paths = new List<UIBezierPath>();
            points = new List<CGPoint[]>();
            currentPoints = new List<CGPoint>();
        }

        //Delete the current signature.
        public void Clear()
        {
            var wasBlank = IsBlank;
            paths = new List<UIBezierPath>();
            currentPath = UIBezierPath.Create();
            points = new List<CGPoint[]>();
            currentPoints.Clear();
            imageView.Image = null;
            ClearLabel.Hidden = true;

            SetNeedsDisplay();

            if (wasBlank != IsBlank)
                OnIsBlankChanged(IsBlank);
        }

        //Create a UIImage of the currently drawn signature with default colors.
        public UIImage GetImage(bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear,
                             getSizeFromScale(UIScreen.MainScreen.Scale, Bounds),
                             UIScreen.MainScreen.Scale, shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(CGSize size, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear, size, getScaleFromSize(size, Bounds), shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(NFloat scale, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear, getSizeFromScale(scale, Bounds), scale, shouldCrop, keepAspectRatio);
        }

        //Create a UIImage of the currently drawn signature with the specified Stroke color.
        public UIImage GetImage(UIColor strokeColor, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear,
                             getSizeFromScale(UIScreen.MainScreen.Scale, Bounds),
                             UIScreen.MainScreen.Scale, shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(UIColor strokeColor, CGSize size, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear, size, getScaleFromSize(size, Bounds), shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(UIColor strokeColor, NFloat scale, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, UIColor.Clear, getSizeFromScale(scale, Bounds), scale, shouldCrop, keepAspectRatio);
        }

        //Create a UIImage of the currently drawn signature with the specified Stroke and Fill colors.
        public UIImage GetImage(UIColor strokeColor, UIColor fillColor, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, fillColor,
                             getSizeFromScale(UIScreen.MainScreen.Scale, Bounds),
                             UIScreen.MainScreen.Scale, shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(UIColor strokeColor, UIColor fillColor, CGSize size, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, fillColor, size, getScaleFromSize(size, Bounds), shouldCrop, keepAspectRatio);
        }

        public UIImage GetImage(UIColor strokeColor, UIColor fillColor, NFloat scale, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return GetImage(strokeColor, fillColor, getSizeFromScale(scale, Bounds), scale, shouldCrop, keepAspectRatio);
        }

        UIImage GetImage(UIColor strokeColor, UIColor fillColor, CGSize size, NFloat scale, bool shouldCrop = true, bool keepAspectRatio = true)
        {
            if (size.Width == 0 || size.Height == 0 || scale <= 0 || strokeColor == null || fillColor == null)
                return null;

            NFloat uncroppedScale;
            CGRect croppedRectangle = new CGRect();

            CGPoint[] cachedPoints;

            if (shouldCrop && (cachedPoints = Points).Any())
            {
                croppedRectangle = getCroppedRectangle(cachedPoints);
                croppedRectangle.Width /= scale;
                croppedRectangle.Height /= scale;
                if (croppedRectangle.X >= 5)
                {
                    croppedRectangle.X -= 5;
                    croppedRectangle.Width += 5;
                }
                if (croppedRectangle.Y >= 5)
                {
                    croppedRectangle.Y -= 5;
                    croppedRectangle.Height += 5;
                }
                if (croppedRectangle.X + croppedRectangle.Width <= size.Width - 5)
                    croppedRectangle.Width += 5;
                if (croppedRectangle.Y + croppedRectangle.Height <= size.Height - 5)
                    croppedRectangle.Height += 5;

                NFloat scaleX = croppedRectangle.Width / Bounds.Width;
                NFloat scaleY = croppedRectangle.Height / Bounds.Height;
                uncroppedScale = 1 / (NFloat)Math.Max(scaleX, scaleY);
            }
            else
            {
                uncroppedScale = scale;
            }

            //Make sure the image is scaled to the screen resolution in case of Retina display.
            if (keepAspectRatio)
                UIGraphics.BeginImageContext(size);
            else
                UIGraphics.BeginImageContext(new CGSize(croppedRectangle.Width * uncroppedScale, croppedRectangle.Height * uncroppedScale));

            //Create context and set the desired options
            CGContext context = UIGraphics.GetCurrentContext();
            context.SetFillColor(fillColor.CGColor);
            context.FillRect(new CGRect(0, 0, size.Width, size.Height));
            context.SetStrokeColor(strokeColor.CGColor);
            context.SetLineWidth(StrokeWidth);
            context.SetLineCap(CGLineCap.Round);
            context.SetLineJoin(CGLineJoin.Round);
            context.ScaleCTM(uncroppedScale, uncroppedScale);

            //Obtain all drawn paths from the array
            foreach (var bezierPath in paths)
            {
                var tempPath = (UIBezierPath)bezierPath.Copy();
                if (shouldCrop)
                    tempPath.ApplyTransform(CGAffineTransform.MakeTranslation(-croppedRectangle.X, -croppedRectangle.Y));
                CGPath path = tempPath.CGPath;
                context.AddPath(path);
                tempPath = null;
            }
            context.StrokePath();

            UIImage image = UIGraphics.GetImageFromCurrentImageContext();

            UIGraphics.EndImageContext();

            return image;
        }

        CGRect getCroppedRectangle(CGPoint[] cachedPoints)
        {
            var xMin = cachedPoints.Where(point => !point.IsEmpty).Min(point => point.X) - strokeWidth / 2;
            var xMax = cachedPoints.Where(point => !point.IsEmpty).Max(point => point.X) + strokeWidth / 2;
            var yMin = cachedPoints.Where(point => !point.IsEmpty).Min(point => point.Y) - strokeWidth / 2;
            var yMax = cachedPoints.Where(point => !point.IsEmpty).Max(point => point.Y) + strokeWidth / 2;

            xMin = (NFloat)Math.Max(xMin, 0);
            xMax = (NFloat)Math.Min(xMax, Bounds.Width);
            yMin = (NFloat)Math.Max(yMin, 0);
            yMax = (NFloat)Math.Min(yMax, Bounds.Height);

            return new CGRect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        NFloat getScaleFromSize(CGSize size, CGRect rectangle)
        {
            NFloat scaleX = size.Width / rectangle.Width;
            NFloat scaleY = size.Height / rectangle.Height;

            return (NFloat)Math.Min(scaleX, scaleY);
        }

        CGSize getSizeFromScale(NFloat scale, CGRect rectangle)
        {
            NFloat width = rectangle.Width * scale;
            NFloat height = rectangle.Height * scale;

            return new CGSize(width, height);
        }

        //Allow the user to import an array of points to be used to draw a signature in the view, with new
        //lines indicated by a CGPoint.Empty in the array.
        public void LoadPoints(CGPoint[] loadedPoints)
        {
            if (loadedPoints == null || loadedPoints.Count() == 0)
                return;

            var wasBlank = IsBlank;

            var startIndex = 0;
            var emptyIndex = loadedPoints.ToList().IndexOf(CGPoint.Empty);

            if (emptyIndex == -1)
                emptyIndex = loadedPoints.Count();

            //Clear any existing paths or points.
            paths = new List<UIBezierPath>();
            points = new List<CGPoint[]>();

            do
            {
                //Create a new path and set the line options
                currentPath = UIBezierPath.Create();
                currentPath.LineWidth = StrokeWidth;
                currentPath.LineJoinStyle = CGLineJoin.Round;

                currentPoints = new List<CGPoint>();

                //Move to the first point and add that point to the current_points array.
                currentPath.MoveTo(loadedPoints[startIndex]);
                currentPoints.Add(loadedPoints[startIndex]);

                //Iterate through the array until an empty point (or the end of the array) is reached,
                //adding each point to the current_path and to the current_points array.
                for (var i = startIndex + 1; i < emptyIndex; i++)
                {
                    currentPath.AddLineTo(loadedPoints[i]);
                    currentPoints.Add(loadedPoints[i]);
                }

                //Add the current_path and current_points list to their respective Lists before
                //starting on the next line to be drawn.
                paths.Add(currentPath);
                points.Add(currentPoints.ToArray());

                //Obtain the indices for the next line to be drawn.
                startIndex = emptyIndex + 1;
                if (startIndex < loadedPoints.Count() - 1)
                {
                    emptyIndex = loadedPoints.ToList().IndexOf(CGPoint.Empty, startIndex);

                    if (emptyIndex == -1)
                        emptyIndex = loadedPoints.Count();
                }
                else
                    emptyIndex = startIndex;
            } while (startIndex < emptyIndex);

            //Obtain the image for the imported signature and display it in the image view.
            imageView.Image = GetImage(false);
            //Display the clear button.
            ClearLabel.Hidden = false;
            SetNeedsDisplay();

            if (wasBlank != IsBlank)
                OnIsBlankChanged(IsBlank);
        }

        //Update the bounds for the rectangle to be redrawn if necessary for the given point.
        void updateBounds(CGPoint point)
        {
            if (point.X < minX + 1)
                minX = point.X - 1;
            if (point.X > maxX - 1)
                maxX = point.X + 1;
            if (point.Y < minY + 1)
                minY = point.Y - 1;
            if (point.Y > maxY - 1)
                maxY = point.Y + 1;
        }

        //Set the bounds for the rectangle that will need to be redrawn to show the drawn path.
        void resetBounds(CGPoint point)
        {
            minX = point.X - 1;
            maxX = point.X + 1;
            minY = point.Y - 1;
            maxY = point.Y + 1;
        }

        /*
		 *Obtain a smoothed path with the specified granularity from the current path using Catmull-Rom spline.  
		 *Implemented using a modified version of the code in the solution at 
		 *http://stackoverflow.com/questions/8702696/drawing-smooth-curves-methods-needed.
		 *Also outputs a List of the points corresponding to the smoothed path.
		 */
        UIBezierPath smoothedPathWithGranularity(int granularity, out List<CGPoint> smoothedPoints)
        {
            List<CGPoint> pointsArray = currentPoints;
            smoothedPoints = new List<CGPoint>();

            //Not enough points to smooth effectively, so return the original path and points.
            if (pointsArray.Count < 4)
            {
                smoothedPoints = pointsArray;
                return currentPath;
            }

            //Create a new bezier path to hold the smoothed path.
            UIBezierPath smoothedPath = UIBezierPath.Create();
            smoothedPath.LineWidth = StrokeWidth;
            smoothedPath.LineJoinStyle = CGLineJoin.Round;

            //Duplicate the first and last points as control points.
            pointsArray.Insert(0, pointsArray[0]);
            pointsArray.Add(pointsArray[pointsArray.Count - 1]);

            //Add the first point
            smoothedPath.MoveTo(pointsArray[0]);
            smoothedPoints.Add(pointsArray[0]);

            for (var index = 1; index < pointsArray.Count - 2; index++)
            {
                CGPoint p0 = pointsArray[index - 1];
                CGPoint p1 = pointsArray[index];
                CGPoint p2 = pointsArray[index + 1];
                CGPoint p3 = pointsArray[index + 2];

                //Add n points starting at p1 + dx/dy up until p2 using Catmull-Rom splines
                for (var i = 1; i < granularity; i++)
                {
                    float t = (float)i * (1f / (float)granularity);
                    float tt = t * t;
                    float ttt = tt * t;

                    //Intermediate point
                    CGPoint mid = default(CGPoint);
                    mid.X = 0.5f * (2f * p1.X + (p2.X - p0.X) * t +
                        (2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * tt +
                        (3f * p1.X - p0.X - 3f * p2.X + p3.X) * ttt);
                    mid.Y = 0.5f * (2 * p1.Y + (p2.Y - p0.Y) * t +
                        (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * tt +
                        (3 * p1.Y - p0.Y - 3 * p2.Y + p3.Y) * ttt);

                    smoothedPath.AddLineTo(mid);
                    smoothedPoints.Add(mid);
                }

                //Add p2
                smoothedPath.AddLineTo(p2);
                smoothedPoints.Add(p2);
            }

            //Add the last point
            smoothedPath.AddLineTo(pointsArray[pointsArray.Count - 1]);
            smoothedPoints.Add(pointsArray[pointsArray.Count - 1]);

            return smoothedPath;
        }

        #region Touch Events
        public override void TouchesBegan(Foundation.NSSet touches, UIEvent evt)
        {
            //Create a new path and set the options.
            currentPath = UIBezierPath.Create();
            currentPath.LineWidth = StrokeWidth;
            currentPath.LineJoinStyle = CGLineJoin.Round;

            currentPoints.Clear();

            UITouch touch = touches.AnyObject as UITouch;

            //Obtain the location of the touch, move the path to that position and add it to the
            //current_points array.
            CGPoint touchLocation = touch.LocationInView(this);
            currentPath.MoveTo(touchLocation);
            currentPoints.Add(touchLocation);

            resetBounds(touchLocation);

            ClearLabel.Hidden = false;
        }

        public override void TouchesMoved(Foundation.NSSet touches, UIEvent evt)
        {
            UITouch touch = touches.AnyObject as UITouch;

            //Obtain the location of the touch and add it to the current path and current_points array.
            CGPoint touchLocation = touch.LocationInView(this);
            currentPath.AddLineTo(touchLocation);
            currentPoints.Add(touchLocation);

            updateBounds(touchLocation);
            SetNeedsDisplayInRect(new CGRect(minX, minY, (NFloat)Math.Abs(maxX - minX), (NFloat)Math.Abs(maxY - minY)));
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            var wasBlank = IsBlank;
            UITouch touch = touches.AnyObject as UITouch;

            //Obtain the location of the touch and add it to the current path and current_points array.
            CGPoint touchLocation = touch.LocationInView(this);
            currentPath.AddLineTo(touchLocation);
            currentPoints.Add(touchLocation);

            //Obtain the smoothed path and the points array for that path.
            currentPath = smoothedPathWithGranularity(40, out currentPoints);
            //Add the smoothed path and points array to their Lists.
            paths.Add(currentPath);
            points.Add(currentPoints.ToArray());

            //Obtain the image for the imported signature and display it in the image view.
            imageView.Image = GetImage(false);
            updateBounds(touchLocation);
            SetNeedsDisplay();

            if (wasBlank != IsBlank)
                OnIsBlankChanged(IsBlank);
        }
        #endregion

        public override void Draw(CGRect rect)
        {
            if (currentPath == null || currentPath.Empty)
                return;

            strokeColor.SetStroke();
            currentPath.Stroke();
        }

        public override void LayoutSubviews()
        {
            var w = Frame.Width;
            var h = Frame.Height;

            SignaturePrompt.SizeToFit();
            ClearLabel.SizeToFit();

            var captionHeight = Caption.SizeThatFits(Caption.Frame.Size).Height;
            var clearButtonHeight = (int)(ClearLabel.TitleLabel.Font).LineHeight + 1;

            var rect = new CGRect(0, 0, w, h);
            imageView.Frame = rect;
            BackgroundImageView.Frame = rect;

            var top = h;
            top = top - ThinPad - captionHeight;
            Caption.Frame = new CGRect(ThickPad, top, w - ThickPad - ThickPad, captionHeight);
            top = top - ThinPad - SignatureLine.Frame.Height;
            SignatureLine.Frame = new CGRect(ThickPad, top, w - ThickPad - ThickPad, LineHeight);
            top = top - ThinPad - SignaturePrompt.Frame.Height;
            SignaturePrompt.Frame = new CGRect(ThickPad, top, SignaturePrompt.Frame.Width, SignaturePrompt.Frame.Height);
            ClearLabel.Frame = new CGRect(w - ThickPad - ClearLabel.Frame.Width, ThickPad, ClearLabel.Frame.Width, clearButtonHeight);
            imageView.Image = GetImage(false);
            SetNeedsDisplay();
        }

        private void OnIsBlankChanged(bool isblank)
        {
            IsBlankChanged?.Invoke(this, isblank);
        }
    }
}
