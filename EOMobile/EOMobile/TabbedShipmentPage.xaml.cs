using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SharedData;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedShipmentPage : EOBaseTabbedPage
    {
        public TabbedShipmentPage()
        {
            Children.Add(new ShipmentPage(this));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();
        }

        public TabbedShipmentPage(long shipmentId)
        {
            Children.Add(new ShipmentPage(this,shipmentId));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            MessagingCenter.Subscribe<EOImgData>(this, "ImageSaved", async (arg) =>
            {
                ((ImagePage)Children[1]).UpdateImageList(arg);
            });

            AddShipmentImages(((ShipmentPage)Children[0]).LoadImageData());
        }

        public void AddShipmentImages(List<EOImgData> imageData)
        {
            foreach (EOImgData imgData in imageData)
            {
                ((ImagePage)Children[1]).AddToImageList(imgData);
            }
        }

        public void ClearShipmentImages()
        {
            ((ImagePage)Children[1]).ClearImageList();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //((App)App.Current).ClearImageData();
            //((App)App.Current).ClearImageDataList();
        }

        private void Help_ShipmentPage_Clicked(object sender, EventArgs e)
        {

        }
    }
}