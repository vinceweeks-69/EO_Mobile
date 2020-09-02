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
    public partial class TabbedWorkOrderReportPage : EOBaseTabbedPage
    {
        //during the creation of a work order that has a saved customer associated,
        //or from the customer page when a customer has been selected, support the 
        //returning of any work order history associated with the selected customer
        public long CustomerId { get; set; }
        public TabbedWorkOrderReportPage()
        {
            Children.Add(new WorkOrderReportPage(this));
            Children.Add(new ImagePage());
            InitializeComponent();
        }

        public TabbedWorkOrderReportPage(long customerId) :  this()
        {
            CustomerId = customerId;    
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

        private void Help_WorkOrderReportPage_Clicked(object sender, EventArgs e)
        {

        }
    }
}