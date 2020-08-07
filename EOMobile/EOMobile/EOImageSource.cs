using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace EOMobile
{
    public class EOImageSource
    {
        public long ImageId { get; set; }

        public byte[] Image { get; set; }

        ImageSource _imgSource;
        public ImageSource ImgSource
        {
            get
            {
                ////May need to scale the image
                //MemoryStream ms = new MemoryStream(this.Image);
                //ms.Position = 0;
                //return ImageSource.FromStream(() => ms);
                return _imgSource;
            }
            set
            {
                _imgSource = value;
            }
        }
    }
}
