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
	public partial class InventoryPage : EOBasePage
	{
		public InventoryPage ()
		{
			InitializeComponent ();
        }

        public void OnPlantsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(PlantPage)))
            {
                Plants.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new PlantPage()).GetAwaiter();
                t.OnCompleted(() => { Plants.IsEnabled = true; });
            }
        }

        public void OnFoliageClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(FoliagePage)))
            {
                Foliage.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new FoliagePage()).GetAwaiter();
                t.OnCompleted(() => { Foliage.IsEnabled = true; });
            }
        }

        public void OnMaterialsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(MaterialsPage)))
            {
                Materials.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new MaterialsPage()).GetAwaiter();
                t.OnCompleted(() => { Materials.IsEnabled = true; });
            }
        }

        public void OnContainersClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(ContainersPage)))
            {
                Containers.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new ContainersPage()).GetAwaiter();
                t.OnCompleted(() => { Containers.IsEnabled = true; });
            }
        }

        public void OnArrangementsClicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(TabbedArrangementPage)))
            {
                Arrangements.IsEnabled = false;
                TaskAwaiter t = Navigation.PushAsync(new TabbedArrangementPage()).GetAwaiter();
                t.OnCompleted(() => { Arrangements.IsEnabled = true; });
            }
        }
    }
}