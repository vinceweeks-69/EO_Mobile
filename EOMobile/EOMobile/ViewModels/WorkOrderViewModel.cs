using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using ViewModels.DataModels;

namespace EOMobile.ViewModels
{
    public class WorkOrderViewModel : INotifyPropertyChanged
    {
        public WorkOrderViewModel()
        { 
        }

        public WorkOrderViewModel(WorkOrderInventoryItemDTO dto)
        {
            WorkOrderId = dto.WorkOrderId;
            InventoryId = dto.InventoryId;
            InventoryName = dto.InventoryName;
            Quantity = dto.Quantity;
            ImageId = dto.ImageId;
            Size = dto.Size;
            GroupId = dto.GroupId;
        }
            
        public long WorkOrderId { get; set; }

        public long InventoryId { get; set; }

        public string InventoryName { get; set; }

        public int Quantity { get; set; }

        public long ImageId { get; set; }

        public string Size { get; set; }

        public long GroupId { get; set; }

        private bool shouldShow;
        public bool ShouldShow
        {
            set {
                shouldShow = value;
                OnPropertyChanged(nameof(ShouldShow));
            }
            get { 
                return Quantity == 0 ? false : true; 
            }
        }

        public  Color BackgroundColor()
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
