using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SharedData;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedWorkOrderPage : EOBaseTabbedPage
    {
        public TabbedWorkOrderPage()
        {
            Children.Add(new WorkOrderPage(this));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();    
        }

        public TabbedWorkOrderPage(long workOrderId)
        {
            Children.Add(new WorkOrderPage(this,workOrderId));
            Children.Add(new ImagePage());
            InitializeComponent();
            Initialize();

            LoadWorkOrderImages(workOrderId);
        }

        private void Initialize()
        {
            MessagingCenter.Subscribe<EOImgData>(this, "ImageSaved", async (arg) =>
            {
                ((ImagePage)Children[1]).UpdateImageList(arg);
            });
        }
        public void AddInventoryImage(EOImgData imageData)
        {
            ((ImagePage)Children[1]).AddToImageList(imageData);
        }

        public List<EOImageSource> GetImages()
        {
            return ((ImagePage)Children[1]).GetImages();
        }

        public void LoadWorkOrderImages(long workOrderId)
        {
            List<EOImageSource> images = ((App)App.Current).GetWordOrderImages(workOrderId);

            foreach(EOImageSource src in images)
            {
                ((ImagePage)Children[1]).AddToImageList(new EOImgData() 
                { 
                    ImageId = src.ImageId,
                    imgData = src.Image
                });
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            //((App)App.Current).ClearImageData();
            //((App)App.Current).ClearImageDataList();
        }

        private void Help_WorkOrderPage_Clicked(object sender, EventArgs e)
        {
            if (!Navigation.NavigationStack.Any(p => p is HelpPage))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("WorkOrderPage")).GetAwaiter();
            }
        }
    }
}