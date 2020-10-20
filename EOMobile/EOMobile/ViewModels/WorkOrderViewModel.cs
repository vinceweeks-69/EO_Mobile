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

        public WorkOrderViewModel(NotInInventoryDTO dto)
        {
            WorkOrderId = dto.WorkOrderId;
            InventoryId = 387;    //temp constant
            InventoryTypeId = 0;        //we don't know - should they have to add this?
            NotInInventoryId = dto.NotInInventoryId;
            UnsavedId = dto.UnsavedId;
            InventoryName = dto.NotInInventoryName;
            Quantity = dto.NotInInventoryQuantity;
            ImageId = 0; 
            Size = dto.NotInInventorySize;
            GroupId = dto.ArrangementId;
        }

        public WorkOrderViewModel(WorkOrderInventoryItemDTO dto)
        {
            WorkOrderId = dto.WorkOrderId;
            InventoryId = dto.InventoryId;
            InventoryTypeId = dto.InventoryTypeId;
            InventoryName = dto.InventoryName;
            Quantity = dto.Quantity;
            ImageId = dto.ImageId;
            Size = dto.Size;
            GroupId = dto.GroupId;
        }

        public WorkOrderViewModel(WorkOrderInventoryMapDTO dto)
        {
            WorkOrderId = dto.WorkOrderId;
            WorkOrderInventoryMapId = dto.WorkOrderInventoryMapId;
            InventoryId = dto.InventoryId;
            InventoryTypeId = dto.InventoryTypeId;
            InventoryName = dto.InventoryName;
            Quantity = dto.Quantity;
            //ImageId = dto.ImageId;
            Size = dto.Size;
            GroupId = dto.GroupId;
        }

        public WorkOrderViewModel(ArrangementInventoryItemDTO dto, long workOrderId)
        {
            WorkOrderId = workOrderId;
            InventoryId = dto.InventoryId;
            InventoryTypeId = dto.InventoryTypeId;
            InventoryName = dto.InventoryName;
            Quantity = dto.Quantity;
            ImageId = dto.ImageId;
            Size = dto.Size;
            GroupId = dto.ArrangementId;
        }

        public long WorkOrderId { get; set; }

        public long WorkOrderInventoryMapId { get; set; }

        public long InventoryId { get; set; }

        public long InventoryTypeId { get; set; }

        public long NotInInventoryId { get; set; }

        public long UnsavedId { get; set; }

        public string InventoryName { get; set; }

        int quantity;
        public int Quantity
        {
            get {
                return quantity;
            }
            set {
                quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public long ImageId { get; set; }

        public string Size { get; set; }

        public long? GroupId { get; set; }

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

        bool shouldEnable;
        public bool ShouldEnable
        {
            set
            {
                shouldEnable = value;
                OnPropertyChanged(nameof(ShouldEnable));
            }
            get
            {
                return shouldEnable;
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
