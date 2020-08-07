using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class InventoryPage : ContentPage
	{
		public InventoryPage ()
		{
			InitializeComponent ();
        }

        public void OnPlantsClicked(object sender, EventArgs e)
        {
            Plants.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new PlantPage()).GetAwaiter();
            t.OnCompleted(() => { Plants.IsEnabled = true; });
        }

        public void OnFoliageClicked(object sender, EventArgs e)
        {
            Foliage.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new FoliagePage()).GetAwaiter();
            t.OnCompleted(() => { Foliage.IsEnabled = true; });
        }

        public void OnMaterialsClicked(object sender, EventArgs e)
        {
            Materials.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new MaterialsPage()).GetAwaiter();
            t.OnCompleted(() => { Materials.IsEnabled = true; });
        }

        public void OnContainersClicked(object sender, EventArgs e)
        {
            Containers.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new ContainersPage()).GetAwaiter();
            t.OnCompleted(() => { Containers.IsEnabled = true; });
        }

        public void OnArrangementsClicked(object sender, EventArgs e)
        {
            Arrangements.IsEnabled = false;
            TaskAwaiter t = Navigation.PushAsync(new TabbedArrangementPage()).GetAwaiter();
            t.OnCompleted(() => { Arrangements.IsEnabled = true; });
        }

        //public void OnImportClicked(object sender, EventArgs e)
        //{
        //    //Navigation.PushAsync(new PlantPage());
        //}
    }
}