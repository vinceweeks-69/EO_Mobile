using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedArrangementPage : EOBaseTabbedPage
    {
        bool forWorkOrder;
        public bool ForWorkOrder { get { return forWorkOrder; } }
        public TabbedArrangementPage(bool forWorkOrder = false)
        {
            this.forWorkOrder = forWorkOrder;

            Children.Add(new ArrangementPage(this));
            Children.Add(new ImagePage());
            InitializeComponent();

            MessagingCenter.Subscribe<EOImgData>(this, "ImageSaved", async (arg) =>
            {
                ((ImagePage)Children[1]).UpdateImageList(arg);
            });
        }

        public void LoadArrangmentImages(List<ImageResponse> imageResponseList)
        {
            ((ImagePage)Children[1]).AddImages(imageResponseList);
        }

        public void ClearArrangementImages()
        {
            ((ImagePage)Children[1]).ClearImageList();
            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        public void AddInventoryImage(EOImgData imgData)
        {
            ((ImagePage)Children[1]).AddToImageList(imgData);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        private void Help_ArrangementPage_Clicked(object sender, EventArgs e)
        {
            ((ArrangementPage)Children[0]).Help_ArrangementPage_Clicked(sender, e);
        }
    }
}