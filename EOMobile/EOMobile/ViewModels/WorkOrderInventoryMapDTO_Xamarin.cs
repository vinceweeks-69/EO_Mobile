using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace EOMobile.ViewModels
{
    [Serializable]
    public class WorkOrderInventoryMapDTO_Xamarin : INotifyPropertyChanged
    {
        public long WorkOrderInventoryMapId { get; set; }

        public long WorkOrderId { get; set; }

        public long InventoryId { get; set; }

        public string InventoryName { get; set; }

        int quantity;
        public int Quantity
        {
            get
            {
                return quantity;
            }
            set
            {
                quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public string Size { get; set; }

        public long? GroupId { get; set; }

        public string NotInInventoryName { get; set; }

        public string NotInInventorySize { get; set; }

        public decimal NotInInventoryPrice { get; set; }

        private bool shouldShow;
        public bool ShouldShow
        {
            set
            {
                shouldShow = value;
                OnPropertyChanged(nameof(ShouldShow));
            }
            get
            {
                return Quantity == 0 ? false : true;
            }
        }

        public Color BackgroundColor()
        {
            return GroupId == 0 ? Color.White : Color.LightGray;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
