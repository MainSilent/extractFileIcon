using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ExtractLargeIconFromFile.Shell;

namespace extractFileIcon
{
    class fileIcon
    {
        private const int SHGFI_SMALLICON = 0x1;
        private const int SHGFI_LARGEICON = 0x0;
        private const int SHIL_JUMBO = 0x4;
        private const int SHIL_EXTRALARGE = 0x2;
        private const int WM_CLOSE = 0x0010;

        public enum IconSizeEnum
        {
            SmallIcon16 = SHGFI_SMALLICON,
            MediumIcon32 = SHGFI_LARGEICON,
            LargeIcon48 = SHIL_EXTRALARGE,
            ExtraLargeIcon = SHIL_JUMBO
        }

        [DllImport("user32")]
        private static extern
            IntPtr SendMessage(
            IntPtr handle,
            int Msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("shell32.dll")]
        private static extern int SHGetImageList(
            int iImageList,
            ref Guid riid,
            out IImageList ppv);

        [DllImport("Shell32.dll")]
        public static extern int SHGetFileInfo(
            string pszPath,
            int dwFileAttributes,
            ref SHFILEINFO psfi,
            int cbFileInfo,
            uint uFlags);

        [DllImport("user32")]
        public static extern int DestroyIcon(
            IntPtr hIcon);

        public static System.Drawing.Bitmap GetBitmapFromFilePath(
            string filepath, IconSizeEnum iconsize)
        {
            IntPtr hIcon = GetIconHandleFromFilePath(filepath, iconsize);
            return getBitmapFromIconHandle(hIcon);
        }

        private static System.Drawing.Bitmap getBitmapFromIconHandle(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero) throw new System.IO.FileNotFoundException();
            var myIcon = System.Drawing.Icon.FromHandle(hIcon);
            var bitmap = myIcon.ToBitmap();
            myIcon.Dispose();
            DestroyIcon(hIcon);
            SendMessage(hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return bitmap;
        }

        private static IntPtr GetIconHandleFromFilePath(string filepath, IconSizeEnum iconsize)
        {
            var shinfo = new SHFILEINFO();
            const uint SHGFI_SYSICONINDEX = 0x4000;
            const int FILE_ATTRIBUTE_NORMAL = 0x80;
            uint flags = SHGFI_SYSICONINDEX;
            return getIconHandleFromFilePathWithFlags(filepath, iconsize, ref shinfo, FILE_ATTRIBUTE_NORMAL, flags);
        }

        private static IntPtr getIconHandleFromFilePathWithFlags(
            string filepath, IconSizeEnum iconsize,
            ref SHFILEINFO shinfo, int fileAttributeFlag, uint flags)
        {
            const int ILD_TRANSPARENT = 1;
            var retval = SHGetFileInfo(filepath, fileAttributeFlag, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (retval == 0) throw (new System.IO.FileNotFoundException());
            var iconIndex = shinfo.iIcon;
            var iImageListGuid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            IImageList iml;
            var hres = SHGetImageList((int)iconsize, ref iImageListGuid, out iml);
            var hIcon = IntPtr.Zero;
            hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
            return hIcon;
        }
    }
    class Main 
    {
        public async Task<object> getIcon(string path)
        {
            string base64IMG;
            bool smallIcon = false;
            MemoryStream ms = new MemoryStream();
            // Get the Icon!
            Bitmap icon = fileIcon.GetBitmapFromFilePath(
                path,
                fileIcon.IconSizeEnum.ExtraLargeIcon
            );

            Action smallIconCheck = delegate
            {
                for (int i = 50; i < icon.Width; i++)
                {
                    for (int j = 50; j < icon.Height; j++)
                    {
                        if (icon.GetPixel(i, j).A != 0)
                        {
                            smallIcon = true;
                            return;
                        }
                    }
                }
            };
            smallIconCheck();
            // i just reversed smallicon value, cuz for loop above, i thought it's better not change the name of smallIcon
            if (!smallIcon)
            {
                Bitmap newIcon = fileIcon.GetBitmapFromFilePath(
                    path,
                    fileIcon.IconSizeEnum.LargeIcon48
                );
                newIcon.Save(ms, ImageFormat.Png);
                byte[] byteImage = ms.ToArray();
                base64IMG = Convert.ToBase64String(byteImage);
            }
            else
            {
                icon.Save(ms, ImageFormat.Png);
                byte[] byteImage = ms.ToArray();
                base64IMG = Convert.ToBase64String(byteImage);
            }

            await base64IMG;
        }
    }
}

