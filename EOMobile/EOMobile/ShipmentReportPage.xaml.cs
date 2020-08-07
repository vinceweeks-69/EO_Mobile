using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	public partial class ShipmentReportPage : ContentPage
	{
        public List<ShipmentInventoryDTO> shipmentList;

        public List<ShipmentDTO> shipment = new List<ShipmentDTO>();

        public List<List<ShipmentInventoryMapDTO>> inventoryList = new List<List<ShipmentInventoryMapDTO>>();

        TabbedShipmentReportPage TabParent = null;

        public ShipmentReportPage (TabbedShipmentReportPage tabParent)
		{
            TabParent = tabParent;
			InitializeComponent ();
		}

        public void OnShowReportsClicked(object sender, EventArgs e)
        {
            ShipmentFilter filter = new ShipmentFilter();
            filter.FromDate = this.ShipmentFromDate.Date;
            filter.ToDate = this.ShipmentToDate.Date;

            shipmentList = ((App)App.Current).GetShipments(filter);

            ObservableCollection<ShipmentDTO> list1 = new ObservableCollection<ShipmentDTO>();

            foreach (ShipmentInventoryDTO s in shipmentList)
            {
                shipment.Add(s.Shipment);

                inventoryList.Add(s.ShipmentInventoryMap);

                list1.Add(s.Shipment);
            }

            ShipmentList.ItemsSource = list1;
        }

        private void ShowInventory_Clicked(object sender, EventArgs e)
        {
            //the  user has clicked on a line item shipment record from the top list
            //get the list of inventory items for the shipment that was clicked on

            Button button = sender as Button;

            if (button != null)
            {
                ShipmentDTO s = (ShipmentDTO)(button).CommandParameter;

                if (s != null)
                {
                    List<ShipmentInventoryMapDTO> inventory = shipmentList.Where(a => a.Shipment.ShipmentId == s.ShipmentId).Select(b => b.ShipmentInventoryMap).First();

                    ObservableCollection<ShipmentInventoryMapDTO> list1 = new ObservableCollection<ShipmentInventoryMapDTO>();

                    foreach (ShipmentInventoryMapDTO i in inventory)
                    {
                        list1.Add(i);
                    }

                    InventoryList.ItemsSource = list1;
                }
            }
        }

        private void EditShipment_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                long shipmentId = (long)(button).CommandParameter;

                Navigation.PushAsync(new TabbedShipmentPage(shipmentId));
            }
        }

        private void Vendor_Focused(object sender, FocusEventArgs e)
        {

        }

        private void Help_ShipmentReportPage_Clicked(object sender, EventArgs e)
        {

        }
    }
}