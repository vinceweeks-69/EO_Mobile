using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedSiteServicePage : TabbedPage
    {
        public TabbedSiteServicePage()
        {
            Children.Add(new SiteServicePage(this));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();
        }

        public TabbedSiteServicePage(long workOrderId)
        {
            Children.Add(new SiteServicePage(this, workOrderId));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            ((ImagePage)Children[1]).AddToImageList(((SiteServicePage)Children[0]).LoadImageData());


            MessagingCenter.Subscribe<EOImgData>(this, "PictureTaken", (arg) =>
            {
                PictureTaken(arg);
            });
        }

        public void PictureTaken(EOImgData imgData)
        {
            ((ImagePage)Children[1]).AddToImageList(imgData);
        }

        public List<EOImageSource> GetImages()
        {
            return ((ImagePage)Children[1]).GetImages();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //((App)App.Current).ClearImageData();
            //((App)App.Current).ClearImageDataList();
        }

        private void Help_SiteServicePage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("SiteServicePage")).GetAwaiter();
        }
    }
}