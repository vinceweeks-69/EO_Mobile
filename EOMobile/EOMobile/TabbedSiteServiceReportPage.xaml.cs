using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModels.ControllerModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedSiteServiceReportPage : EOBaseTabbedPage
    {
        public TabbedSiteServiceReportPage()
        {
            Children.Add(new SiteServiceReportPage(this));
            Children.Add(new ImagePage());
            InitializeComponent();
        }

        public void LoadArrangmentImages(List<ImageResponse> imageResponseList)
        {
            ((ImagePage)Children[1]).AddImages(imageResponseList);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ((App)App.Current).ClearImageData();
            ((App)App.Current).ClearImageDataList();
        }

        private void Help_SiteServiceReportPage_Clicked(object sender, EventArgs e)
        {

        }
    }
}