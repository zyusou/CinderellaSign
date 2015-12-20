using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;

namespace CinderellaSign
{
    class CinderellaSignGenerator
    {
        private IplImage srcImage = new IplImage();
        private IplImage dstImage = new IplImage();
        private IplImage attributeBackGroundImage = new IplImage();

        private const int ErodingValue = 8;
        private const int noizeCancellingValue = 2;
        private Color Passion = Color.FromArgb(252, 198, 38);
        private Color Cool = Color.FromArgb(87, 207, 237);
        private Color Cute = Color.FromArgb(237, 111, 201);

        /// <summary>
        /// パスの画像をIplImageでロードする。
        /// </summary>
        /// <param name="path"></param>
        public void LoadSrcImage(string path)
        {
            try
            {
                srcImage = Cv.LoadImage(path);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.Write(exception.StackTrace);
            }
        }

        /// <summary>
        /// 属性を取得して、それにあった変数で背景画像を生成する
        /// 例外処理とかめんどくさくてやってない。普通はこの関数内でループするべきなんだろうけども。
        /// </summary>
        /// <param name="getValue">属性を文字列で入力</param>
        public void GetAttibuteValue(string getValue)
        {
            var lower = getValue.ToLower();

            switch (lower)
            {
                case "cute":
                    Console.WriteLine(getValue);
                    GenerateBackGroundImage(Cute, Cool);
                    break;

                case "cool":
                    Console.WriteLine(getValue);
                    GenerateBackGroundImage(Cool, Passion);
                    break;

                case "passion":
                    Console.WriteLine(getValue);
                    GenerateBackGroundImage(Passion, Cute);
                    break;

                default:
                    Console.WriteLine("バーカ");
                    break;
            }
        }

        /// <summary>
        /// 背景のグラデーション画像を作成する
        /// </summary>
        /// <param name="startColor">グラデーションが開始する色</param>
        /// <param name="endColor">グラデーションが終了する色</param>
        private void GenerateBackGroundImage(Color startColor, Color endColor)
        {
            var attributeBitmap = new Bitmap(srcImage.Width, srcImage.Height);


            using (var g = Graphics.FromImage(attributeBitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;

                LinearGradientBrush lgb = new LinearGradientBrush(
                    new System.Drawing.Point(0, 0), new System.Drawing.Point(0, srcImage.Height),
                    startColor, endColor);
                ColorBlend cb = new ColorBlend();
                cb.Colors = new Color[] {startColor, endColor};
                cb.Positions = new float[] {0f, 1f};

                lgb.InterpolationColors = cb;

                g.FillRectangle(lgb, g.VisibleClipBounds);
            }

            attributeBackGroundImage = (OpenCvSharp.IplImage) BitmapConverter.ToIplImage(attributeBitmap);
        }

        public void ShowDstImage()
        {
            CvWindow.ShowImages(dstImage);
        }

        /// <summary>
        /// Cinderellaの画像を生成します。
        /// </summary>
        internal void GenerateSignImage()
        {
            var graySrcImage = Cv.CreateImage(new CvSize(srcImage.Width, srcImage.Height), BitDepth.U8, 1);
            var grayErodedImage = Cv.CreateImage(new CvSize(srcImage.Width, srcImage.Height),BitDepth.U8, 1);
            var srcMaskImage = Cv.CreateImage(new CvSize(srcImage.Width, srcImage.Height),BitDepth.U8, 1);
            var erodedMaskImage = Cv.CreateImage(new CvSize(srcImage.Width, srcImage.Height),BitDepth.U8, 1);


            // グレースケール化とノイズ除去、膨張処理を行い、グラデーションのマスク画像を作成する
            Cv.CvtColor(srcImage, graySrcImage, ColorConversion.RgbToGray);

            for (int i = 0; i < noizeCancellingValue; i++)
            {
                Cv.Dilate(graySrcImage, graySrcImage, null, 1);
            }
            CvWindow.ShowImages(graySrcImage);

            for (int i = 0; i < noizeCancellingValue; i++)
            {
                Cv.Erode(graySrcImage, graySrcImage,null, 1);
            }
            
            CvWindow.ShowImages(graySrcImage);

            Cv.Erode(graySrcImage, grayErodedImage, null, ErodingValue);
            Cv.Threshold(grayErodedImage, grayErodedImage, 0, 255, ThresholdType.ToZero);
            Cv.Not(grayErodedImage, erodedMaskImage);

            dstImage = new IplImage(new CvSize(srcImage.Width, srcImage.Height), BitDepth.U8, attributeBackGroundImage.ElemChannels);

            // マスク画像の領域のみグラデーション画像をコピーする
            Cv.Copy(attributeBackGroundImage, dstImage, erodedMaskImage);

            Cv.Smooth(dstImage, dstImage, SmoothType.Blur, 6);

            //元画像を二値化して反転、マスク画像を作成する
            Cv.Threshold(graySrcImage, graySrcImage, 0, 255, ThresholdType.Otsu);
            Cv.Not(graySrcImage, srcMaskImage);

            var temp = Cv.CreateImage(new CvSize(srcImage.Width, srcImage.Height), BitDepth.U8, 4);
            Cv.CvtColor(srcMaskImage, temp, ColorConversion.GrayToRgb);

            Cv.Copy(temp, dstImage, srcMaskImage);

            CvWindow.ShowImages(dstImage);

            Cv.SaveImage(@"Image\dst.png", dstImage);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var SignGenerator = new CinderellaSignGenerator();
            string attributeValue;

            SignGenerator.LoadSrcImage(@"Image\src.jpg");
            
            Console.WriteLine("属性を入力してください。Cute,Cool,Passion");
            attributeValue = Console.ReadLine();

            SignGenerator.GetAttibuteValue(attributeValue);

            SignGenerator.GenerateSignImage();

        }
    }
}
