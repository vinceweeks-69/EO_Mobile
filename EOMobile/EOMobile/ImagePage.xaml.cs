using Android.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImagePage : ContentPage
    {
        //public List<ImageResponse> Images = new List<ImageResponse>();

        public List<EOImageSource> ImageSourceList = new List<EOImageSource>();

        //public List<ImageSource> sourceList = new List<ImageSource>();

        public ObservableCollection<EOImageSource> ImageSourceListOC = new ObservableCollection<EOImageSource>();

        public ImagePage()
        {
            InitializeComponent();

            BindingContext = this;
        }

        public List<EOImageSource> GetImages()
        {
            return ImageSourceList;
        }

        public void AddImages(List<ImageResponse> imageResponseList)
        {
            try
            {
                if (imageResponseList != null)
                {
                    foreach (ImageResponse imgData in imageResponseList)
                    {
                        EOImageSource imgSource = new EOImageSource();
                        imgSource.ImageId = imgData.ImageId;
                        imgSource.Image = imgData.Image;
                        imgSource.ImgSource = ImageSource.FromStream(() => new MemoryStream(imgData.Image));

                        ImageSourceList.Add(imgSource);
                    }
                }

                ImageSourceListOC = new ObservableCollection<EOImageSource>(ImageSourceList);

                imageList.ItemsSource = ImageSourceListOC;
            }
            catch (Exception ex)
            {

            }
        }

        public void AddToImageList(EOImgData imageData)
        {
            EOImageSource imgSource = new EOImageSource();

            imgSource.ImageId = imageData.ImageId;
            imgSource.Image = imageData.imgData;
            imgSource.ImgSource = ImageSource.FromStream(() => new MemoryStream(imageData.imgData));

            ImageSourceList.Add(imgSource);
            ImageSourceListOC = new ObservableCollection<EOImageSource>(ImageSourceList);
            imageList.ItemsSource = ImageSourceListOC;
        }

        public void AddToImageList(List<EOImgData> imageData)
        {
            try
            {
                foreach (EOImgData imgData in imageData)
                {
                    EOImageSource imgSource = new EOImageSource();
                    imgSource.ImageId = imgData.ImageId;
                    imgSource.Image = imgData.imgData;
                    imgSource.ImgSource = ImageSource.FromStream(() => new MemoryStream(imgData.imgData));

                    ImageSourceList.Add(imgSource);
                }

                ImageSourceListOC = new ObservableCollection<EOImageSource>(ImageSourceList);

                imageList.ItemsSource = ImageSourceListOC;
            }
            catch(Exception ex)
            {

            }
        }

        public void UpdateImageList(EOImgData imageData)
        {
            try
            {
                EOImageSource imgSource = new EOImageSource();
                imgSource.ImageId = imageData.ImageId;
                imgSource.Image = imageData.imgData;
                imgSource.ImgSource = ImageSource.FromStream(() => new MemoryStream(imageData.imgData));

                ImageSourceList.Add(imgSource);

                ImageSourceListOC = new ObservableCollection<EOImageSource>(ImageSourceList);

                imageList.ItemsSource = ImageSourceListOC;
            }
            catch(Exception ex)
            {
                int debug = 1;
            }
        }

        public void ClearImageList()
        {
            try
            {
                ImageSourceList.Clear();
                imageList.ItemsSource = null;
            }
            catch(Exception ex)
            {
                int debug = 1;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}