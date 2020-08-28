using ConsoleApplication5;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiceBot.modules.Public
{
    public class Die_Scraper : ModuleBase
    {
        GoogleTest go = new GoogleTest();
        //static string url;
        static string CRarity;
        static string CUrl;
        static string CName;

        [Command("DiceScrape")]
        public async Task dicescraping(string set)
        {
            await ReplyAsync("started");

            string sheet = set;
            sheet = sheet.ToUpper();

            string sheetref = sheet + "!";
            GoogleTest.Main(sheetref);
            var test = GoogleTest._values;

            foreach (var row in test)
            {
                CName = (string)row[1];
                CRarity = (string)row[5];
                CUrl = (string)row[9];

                if (CRarity == "Common")
                {
                    imagesaver(CUrl, set);
                }

            }

            await ReplyAsync("Done");
        }

        private static void imagesaver(string url, string set)
        {
            using (WebClient wc = new WebClient())
            {
                using (Stream s = wc.OpenRead(url))
                {
                    using (Bitmap bmp = new Bitmap(s))
                    {
                        bmp.Save("images/CardImage/working.jpg");
                        bmp.Dispose();
                    }

                    Resize_Picture("images/CardImage/working.jpg", "images/CardImage/working_modified.jpg", 0, 800, 10);

                    using (Bitmap bmp = new Bitmap("images/CardImage/working_modified.jpg"))
                    {
                        Color clr = bmp.GetPixel(35, 752); // Get the color of pixel at position 5,5
                        int red = clr.R;
                        int green = clr.G;
                        int blue = clr.B;

                        Color clr2 = bmp.GetPixel(88, 760);
                        int red2 = clr2.R;
                        int green2 = clr2.G;
                        int blue2 = clr2.B;

                        Color clr3 = bmp.GetPixel(65, 728);
                        int red3 = clr3.R;
                        int green3 = clr3.G;
                        int blue3 = clr3.B;

                        string path = @"TextFiles/" + set + "'s Dice.txt";
                        if (File.Exists(path))
                        {
                            string[] arrline = File.ReadAllLines(path);

                            for (int i = 0; i < arrline.Length; i++) //check if the thing exists
                            {
                                if (!arrline[i].Contains(CName + "'s Die"))
                                {
                                    using (StreamWriter sw = File.AppendText(path))
                                    {
                                        sw.WriteLine(CName + "'s Die : " + red + "," + green + "," + blue + ",   "
                                            + red2 + "," + green2 + "," + blue2 + ",   "
                                            + red3 + "," + green3 + "," + blue3);
                                    }
                                }
                                else
                                {

                                }
                            }
                            //File.WriteAllLines(path, arrline);

                        }
                        else
                        {
                            using (StreamWriter sw = File.CreateText(path))
                            {
                                sw.WriteLine(CName + "'s Die : " + red + "," + green + "," + blue + ",   "
                                            + red2 + "," + green2 + "," + blue2 + ",   "
                                            + red3 + "," + green3 + "," + blue3);
                            }

                        }

                    }
                }
            }

        }
        public static void Resize_Picture(string Org, string Des, int FinalWidth, int FinalHeight, int ImageQuality)
        {
            System.Drawing.Bitmap NewBMP;
            System.Drawing.Graphics graphicTemp;
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(Org);

            int iWidth;
            int iHeight;
            if ((FinalHeight == 0) && (FinalWidth != 0))
            {
                iWidth = FinalWidth;
                iHeight = (bmp.Size.Height * iWidth / bmp.Size.Width);
            }
            else if ((FinalHeight != 0) && (FinalWidth == 0))
            {
                iHeight = FinalHeight;
                iWidth = (bmp.Size.Width * iHeight / bmp.Size.Height);
            }
            else
            {
                iWidth = FinalWidth;
                iHeight = FinalHeight;
            }

            NewBMP = new System.Drawing.Bitmap(iWidth, iHeight);
            graphicTemp = System.Drawing.Graphics.FromImage(NewBMP);
            graphicTemp.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            graphicTemp.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphicTemp.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphicTemp.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphicTemp.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphicTemp.DrawImage(bmp, 0, 0, iWidth, iHeight);
            graphicTemp.Dispose();
            System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
            System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, ImageQuality);
            encoderParams.Param[0] = encoderParam;
            System.Drawing.Imaging.ImageCodecInfo[] arrayICI = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            for (int fwd = 0; fwd <= arrayICI.Length - 1; fwd++)
            {
                if (arrayICI[fwd].FormatDescription.Equals("JPEG"))
                {
                    NewBMP.Save(Des, arrayICI[fwd], encoderParams);
                }
            }

            NewBMP.Dispose();
            bmp.Dispose();
        }

    }
}
