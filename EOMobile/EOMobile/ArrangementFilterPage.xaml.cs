using Rg.Plugins.Popup.Extensions;
using SharedData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    public partial class ArrangementFilterPage : EOBasePage
    {
        //if in WorkOrder mode, use this value to pick customer container
        PersonAndAddressDTO customer = new PersonAndAddressDTO();

        List<InventoryTypeDTO> inventoryTypeList = new List<InventoryTypeDTO>();

        List<PlantInventoryDTO> plants = new List<PlantInventoryDTO>();
        List<PlantTypeDTO> plantTypes = new List<PlantTypeDTO>();
        List<PlantNameDTO> plantNames = new List<PlantNameDTO>();
        List<KeyValuePair<long, string>> plantSizes = new List<KeyValuePair<long, string>>();

        List<FoliageInventoryDTO> foliage = new List<FoliageInventoryDTO>();
        List<FoliageTypeDTO> foliageTypes = new List<FoliageTypeDTO>();
        List<FoliageNameDTO> foliageNames = new List<FoliageNameDTO>();
        List<KeyValuePair<long, string>> foliageSizes = new List<KeyValuePair<long, string>>();


        List<MaterialInventoryDTO> materials = new List<MaterialInventoryDTO>();
        List<MaterialTypeDTO> materialTypes = new List<MaterialTypeDTO>();
        List<MaterialNameDTO> materialNames = new List<MaterialNameDTO>();
        List<KeyValuePair<long, string>> materialSizes = new List<KeyValuePair<long, string>>();

        List<ContainerTypeDTO> containerTypes = new List<ContainerTypeDTO>();
        List<ContainerNameDTO> containerNames = new List<ContainerNameDTO>();
        List<ContainerInventoryDTO> containers = new List<ContainerInventoryDTO>();
        List<KeyValuePair<long, string>> containerSizes = new List<KeyValuePair<long, string>>();

        ContentPage Initiator;

        bool showArrangements;
        public ArrangementFilterPage(ContentPage initiator, bool showArrangements = true)
        {
            Initiator = initiator;

            if(initiator is WorkOrderPage)
            {
                customer = ((WorkOrderPage)initiator).Customer;
            }

            this.showArrangements = showArrangements;

            InitializeComponent();

            Type.SelectedIndexChanged += TypeCombo_SelectionChanged;

            Name.SelectedIndexChanged += NameCombo_SelectionChanged;

            Size.SelectedIndexChanged += SizeCombo_SelectionChanged;

            ArrangementInventoryList.ItemSelected += ArrangementInventoryList_ItemSelected;

            ((App)App.Current).GetInventoryTypes().ContinueWith(a => ShowTypes(a.Result));
        }

        private void ShowTypes(GetInventoryTypeResponse response)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                inventoryTypeList = response.InventoryType;

                ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();
                foreach (InventoryTypeDTO type in inventoryTypeList)
                {
                    if (type.InventoryTypeName == "Arrangements" && !showArrangements)
                    {
                        continue;
                    }

                    list1.Add(new KeyValuePair<long, string>(type.InventoryTypeId, type.InventoryTypeName));
                }

                InventoryType.ItemsSource = list1;

                InventoryType.SelectedIndexChanged += InventoryType_SelectedIndexChanged;
            });
        }

        private void LoadSizes(long inventoryTypeId, bool showSizes = false)
        {
            GenericGetRequest request = new GenericGetRequest("GetSizeByInventoryType", "inventoryTypeId", inventoryTypeId);

            List<KeyValuePair<long, string>> sizeList = new List<KeyValuePair<long, string>>();

            long index = 1;
            switch (inventoryTypeId)
            {
                case 1:  //orchids
                    if (plantSizes.Count == 0)
                    {
                        ((App)App.Current).GetRequest<GetSizeResponse>(request).ContinueWith(a => StoreSizes(a.Result, plantSizes, showSizes));
                    }
                    else
                    {
                        sizeList = plantSizes;  
                    }
                    break;

                case 2:  //containers
                    if (containerSizes.Count == 0)
                    {
                        ((App)App.Current).GetRequest<GetSizeResponse>(request).ContinueWith(a => StoreSizes(a.Result, containerSizes, showSizes));
                    }
                    else
                    {
                        sizeList = containerSizes;
                    }
                    break;

                case 4:  //foliage
                    if (foliageSizes.Count == 0)
                    { 
                        ((App)App.Current).GetRequest<GetSizeResponse>(request).ContinueWith(a => StoreSizes(a.Result, foliageSizes, showSizes));
                    }
                    else
                    {
                        sizeList = foliageSizes;
                    }
                    break;

                case 5:  //materials
                    if (materialSizes.Count == 0)
                    {
                        ((App)App.Current).GetRequest<GetSizeResponse>(request).ContinueWith(a => StoreSizes(a.Result, materialSizes, showSizes));
                    }
                    else
                    {
                        sizeList = materialSizes;
                    }
                    break;
            }

            if (sizeList.Count > 0)
            {
                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                list2.Add(new KeyValuePair<long, string>(0, "All sizes"));

                foreach (KeyValuePair<long, string> kvp in sizeList)
                {
                    list2.Add(kvp);
                }

                Size.ItemsSource = list2;
            }
        }

        private void StoreSizes(GetSizeResponse response, List<KeyValuePair<long,string>> sizeList, bool showSizes = false )
        {
            sizeList.Clear();

            //store the inventory type id value in the Key val of the size collection
            foreach(string s in response.Sizes)
            {
                sizeList.Add(new KeyValuePair<long, string>(response.InventoryTypeId, s));
            }

            if (showSizes)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ObservableCollection<KeyValuePair<long,string>> list2 = new ObservableCollection<KeyValuePair<long,string>>();

                    list2.Add(new KeyValuePair<long, string>(0, "All sizes"));

                    foreach (KeyValuePair<long,string> kvp in sizeList)
                    {
                        list2.Add(kvp);
                    }

                    Size.ItemsSource = list2;
                });
            }
        }

        private void ArrangementInventoryList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ArrangementInventoryFilteredItem item = (sender as ListView).SelectedItem as ArrangementInventoryFilteredItem;

            if (item != null)
            {
                if (Initiator is ShipmentPage)
                {
                    ShipmentInventoryItemDTO so = new ShipmentInventoryItemDTO(0, item.Id, item.Name, item.Size, 0);

                    MessagingCenter.Send<ShipmentInventoryItemDTO>(so, "SearchShipmentInventory");
                }
                else if (Initiator is ArrangementPage)
                {
                    ArrangementInventoryItemDTO ao = new ArrangementInventoryItemDTO(0, item.Id, item.Name,  0);

                    MessagingCenter.Send<ArrangementInventoryItemDTO>(ao, "SearchArrangementInventory");
                }
                else
                {
                    WorkOrderInventoryItemDTO wo = new WorkOrderInventoryItemDTO(0, item.Id, item.Name, item.Size, 0);

                    MessagingCenter.Send<WorkOrderInventoryItemDTO>(wo, "SearchInventory");
                }
            }

            Navigation.PopAsync();
        }

        private void AddNotInInventory_Clicked(object sender, EventArgs e)
        {
            String msg = String.Empty;
            if (NotInInventoryName.Text == String.Empty)
            {
                msg += "Please add a name for the item not in inventory. \n";
            }

            if (NotInInventorySize.Text == String.Empty)
            {
                msg += "Please add a size for the item not in inventory. \n";
            }

            if (NotInInventoryPrice.Text == String.Empty)
            {
                msg += "Please add a price for the item not in inventory. \n";
            }

            if (msg != String.Empty)
            {
                DisplayAlert("Error", msg, "Ok");
            }
            else
            {
                NotInInventoryDTO dto = new NotInInventoryDTO();

                dto.WorkOrderId = 0;
                dto.ArrangementId = 0;
                dto.NotInInventoryName = NotInInventoryName.Text;
                dto.NotInInventoryQuantity = 1;
                dto.NotInInventorySize = NotInInventorySize.Text;
                dto.NotInInventoryPrice = Convert.ToDecimal(NotInInventoryPrice.Text);

                if (Initiator is ArrangementPage)
                {
                    MessagingCenter.Send<NotInInventoryDTO>(dto, "ArrangementNotInInventory");
                }
                else if (Initiator is WorkOrderPage)
                {
                    MessagingCenter.Send<NotInInventoryDTO>(dto, "WorkOrderNotInInventory");
                }

                Navigation.PopAsync();
            }
        }

        private void InventoryType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                long selectedValue = ((KeyValuePair<long, string>)InventoryType.SelectedItem).Key;

                //clear names list
                Name.ItemsSource = null;

                //clear any previous selected 
                ArrangementInventoryList.ItemsSource = null;

                //get types for the current inventory type selection
                Picker p = sender as Picker;

                if (p != null)
                {
                    Type.SelectedIndex = -1;
                    Name.SelectedIndex = -1;
                    Size.SelectedIndex = -1;

                    KeyValuePair<long, string> kvp = (KeyValuePair<long, string>)p.SelectedItem;

                    ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

                    ObservableCollection<KeyValuePair<long, string>> sizeList = new ObservableCollection<KeyValuePair<long, string>>();

                    switch (kvp.Value)
                    {
                        case "Orchids":
                            if (plantTypes.Count == 0)
                            {
                                ((App)App.Current).GetPlantTypes().ContinueWith(a => PlantTypesLoaded(kvp.Key, list1,a.Result));
                            }
                            else
                            {
                                ShowPlantData(plants);

                                foreach (PlantTypeDTO plantType in plantTypes)
                                {
                                    list1.Add(new KeyValuePair<long, string>(plantType.PlantTypeId, plantType.PlantTypeName));
                                }

                                foreach(KeyValuePair<long, string> size in plantSizes)
                                {
                                    sizeList.Add(size);
                                }
                            }
                            break;

                        case "Foliage":
                            if (foliageTypes.Count == 0)
                            {
                                ((App)App.Current).GetFoliageTypes().ContinueWith(a => FoliageTypesLoaded(kvp.Key,list1,a.Result));
                            }
                            else
                            {
                                ShowFoliageData(foliage);

                                foreach (FoliageTypeDTO foliageType in foliageTypes)
                                {
                                    list1.Add(new KeyValuePair<long, string>(foliageType.FoliageTypeId, foliageType.FoliageTypeName));
                                }

                                foreach (KeyValuePair<long, string> size in foliageSizes)
                                {
                                    sizeList.Add(size);
                                }
                            }
                            break;

                        case "Arrangements":
                            {
                                if (!PageExists(typeof(TabbedArrangementPage)))
                                {
                                    Navigation.PushAsync(new TabbedArrangementPage(true, customer));
                                }
                            }
                            break;

                        case "Materials":
                            if (materialTypes.Count == 0)
                            {
                               ((App)App.Current).GetMaterialTypes().ContinueWith(a => MaterialTypesLoaded(kvp.Key,list1,a.Result));
                            }
                            else
                            {
                                foreach (MaterialTypeDTO materialType in materialTypes)
                                {
                                    list1.Add(new KeyValuePair<long, string>(materialType.MaterialTypeId, materialType.MaterialTypeName));
                                }

                                foreach (KeyValuePair<long, string> size in materialSizes)
                                {
                                    sizeList.Add(size);
                                }
                            }
                            break;

                        case "Containers":
                            if (containerTypes.Count == 0)
                            {
                                ((App)App.Current).GetContainerTypes().ContinueWith(a => ContainerTypesLoaded(kvp.Key, list1,a.Result));
                            }
                            else
                            {
                                foreach (ContainerTypeDTO containerType in containerTypes)
                                {
                                    list1.Add(new KeyValuePair<long, string>(containerType.ContainerTypeId, containerType.ContainerTypeName));
                                }

                                foreach (KeyValuePair<long, string> size in containerSizes)
                                {
                                    sizeList.Add(size);
                                }
                            }
                            break;
                    }

                    if (list1.Count > 0)
                    {
                        Type.ItemsSource = list1;

                        Size.ItemsSource = sizeList;
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void TypeCombo_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                //get names for the current inventory type selection
                Picker cb = sender as Picker;

                if (cb != null && cb.SelectedItem != null)
                {
                    Name.SelectedIndex = -1; //clear previous choices
                    Size.SelectedIndex = -1;

                    string inventoryType = ((KeyValuePair<long, string>)InventoryType.SelectedItem).Value;

                    KeyValuePair<long, string> kvp = (KeyValuePair<long, string>)cb.SelectedItem;

                    ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

                    switch (inventoryType)
                    {
                        case "Orchids":
                            {
                                ((App)App.Current).GetPlantNamesByType(kvp.Key).ContinueWith(a => PlantNamesLoaded(list1, a.Result));
                            }
                            break;

                        case "Foliage":
                            {
                                ((App)App.Current).GetFoliageNamesByType(kvp.Key).ContinueWith(a => FoliageNamesLoaded(list1, a.Result));
                            }
                            break;

                        case "Materials":
                            {
                                ((App)App.Current).GetMaterialNamesByType(kvp.Key).ContinueWith(a => MaterialNamesLoaded(list1, a.Result));
                            }
                            break;

                        case "Containers":
                            {
                                ((App)App.Current).GetContainerNamesByType(kvp.Key).ContinueWith(a => ContainerNamesLoaded(list1, a.Result));
                            }
                            break;
                    }

                    if (list1.Count > 0)
                    {
                        Name.ItemsSource = list1;

                        SearchButton_Click(null, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void NameCombo_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                Picker cb = sender as Picker;

                if (cb != null && cb.SelectedItem != null)
                {
                    Size.SelectedIndex = -1;

                    string inventoryType = ((KeyValuePair<long, string>)InventoryType.SelectedItem).Value;

                    KeyValuePair<long, string> kvp = (KeyValuePair<long, string>)cb.SelectedItem;

                    SearchButton_Click(sender, e);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void SizeCombo_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                Picker cb = sender as Picker;

                if (cb != null && cb.SelectedItem != null)
                {
                    if (Type.SelectedItem != null)
                    {
                        KeyValuePair<long, string> kvp = (KeyValuePair<long, string>)cb.SelectedItem;

                        string selectedSize = ((KeyValuePair<long, string>)Size.SelectedItem).Value;

                        SearchButton_Click(sender, e);
                    }
                    else
                    {
                        Size.SelectedIndexChanged -= SizeCombo_SelectionChanged;

                        DisplayAlert("Error", "Please pick a type", "OK");

                        Size.SelectedIndex = -1;
                        Size.SelectedIndexChanged += SizeCombo_SelectionChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                int debug = 1;
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            if (Type.SelectedItem != null)
            {
                string inventoryType = ((KeyValuePair<long, string>)InventoryType.SelectedItem).Value;

                long typeId = ((KeyValuePair<long, string>)Type.SelectedItem).Key;

                string name = String.Empty;

                if (Name.SelectedItem != null)
                {
                    name = ((KeyValuePair<long, string>)Name.SelectedItem).Value;
                }

                string size = String.Empty;
                if (Size.SelectedItem != null)
                {
                    if (((KeyValuePair<long, string>)Size.SelectedItem).Key != 0)  //0 == "All sizes"
                    {
                        size = ((KeyValuePair<long, string>)Size.SelectedItem).Value;
                    }
                }

                List<ArrangementInventoryFilteredItem> list1 = new List<ArrangementInventoryFilteredItem>();

                switch (inventoryType)
                {
                    case "Orchids":
                        {
                            foreach (PlantInventoryDTO p in plants)
                            {
                                list1.Add(p.ToFilteredItem());
                            }
                        }
                        break;

                    case "Foliage":
                        {
                            foreach (FoliageInventoryDTO f in foliage)
                            {
                                list1.Add(f.ToFilteredItem());
                            }
                        }
                        break;

                    case "Materials":
                        {
                            foreach (MaterialInventoryDTO m in materials)
                            {
                                list1.Add(m.ToFilteredItem());
                            }
                        }
                        break;

                    case "Containers":
                        {
                            foreach (ContainerInventoryDTO c in containers)
                            {
                                list1.Add(c.ToFilteredItem());
                            }
                        }
                        break;
                }

                ObservableCollection<ArrangementInventoryFilteredItem> filtered = new ObservableCollection<ArrangementInventoryFilteredItem>();

                if (list1.Count > 0)
                {

                    if (typeId > 0)
                    {
                        list1 = list1.Where(a => a.TypeId == typeId).ToList();
                    }

                    if (!String.IsNullOrEmpty(name))
                    {
                        list1 = list1.Where(a => a.Name.Contains(name)).ToList();
                    }

                    if (!String.IsNullOrEmpty(size))
                    {
                        list1 = list1.Where(a => a.Size.Equals(size)).ToList();
                    }



                    foreach (ArrangementInventoryFilteredItem fi in list1)
                    {
                        filtered.Add(fi);
                    }
                }

                ArrangementInventoryList.ItemsSource = filtered;
            }
        }

        private void PlantTypesLoaded(long inventoryTypeId, ObservableCollection<KeyValuePair<long, string>> list1, GetPlantTypeResponse types)
        {
            plantTypes = types.PlantTypes;

            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (PlantTypeDTO plantType in plantTypes)
                {
                    list1.Add(new KeyValuePair<long, string>(plantType.PlantTypeId, plantType.PlantTypeName));
                }

                Type.ItemsSource = list1;

                LoadListData(inventoryTypeId);
            });
        }

        private void LoadListData(long inventoryTypeId)
        {
            switch (inventoryTypeId)
            {
                case 1: //orchids
                    if (plants.Count == 0)
                    {
                        ((App)App.Current).GetPlants().ContinueWith(a => ShowPlantData(a.Result.PlantInventoryList, true));
                    }
                    else
                    {
                        ShowPlantData(plants);
                    }
                    break;

                case 2: // containers
                    if (containers.Count == 0)
                    {
                        ((App)App.Current).GetContainers().ContinueWith(a => ShowContainerData(a.Result.ContainerInventoryList, true));
                    }
                    else
                    {
                        ShowFoliageData(foliage);
                    }
                    break;

                case 3: //arrangements

                    break;

                case 4: // foliage
                    if(foliage.Count == 0)
                    {
                        ((App)App.Current).GetFoliage().ContinueWith(a => ShowFoliageData(a.Result.FoliageInventoryList, true));
                    }
                    else
                    {
                        ShowFoliageData(foliage);
                    }
                    break;

                case 5: //materials
                    if(materials.Count == 0)
                    {
                        ((App)App.Current).GetMaterials().ContinueWith(a => ShowMaterialData(a.Result.MaterialInventoryList, true));
                    }
                    else
                    {
                        ShowMaterialData(materials);
                    }
                    break;
            }
        }

        private void ShowPlantData(List<PlantInventoryDTO> plantData, bool isAsync = false)
        {
            plants = plantData;

            ObservableCollection<ArrangementInventoryFilteredItem> list1 = new ObservableCollection<ArrangementInventoryFilteredItem>();

            foreach (PlantInventoryDTO p in plantData)
            {
                list1.Add(p.ToFilteredItem());
            }

            if (isAsync)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ArrangementInventoryList.ItemsSource = list1;

                    LoadSizes(1,true);
                });
            }
            else
            {
                ArrangementInventoryList.ItemsSource = list1;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach(KeyValuePair<long,string> kvp in plantSizes)
                {
                    list2.Add(kvp);
                }

                Size.ItemsSource = list2;
            }
        }

        private void ShowContainerData(List<ContainerInventoryDTO> containerData, bool isAsync = false)
        {
            containers = containerData;

            ObservableCollection<ArrangementInventoryFilteredItem> list1 = new ObservableCollection<ArrangementInventoryFilteredItem>();

            foreach (ContainerInventoryDTO p in containerData)
            {
                list1.Add(p.ToFilteredItem());
            }

            if (isAsync)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ArrangementInventoryList.ItemsSource = list1;

                    LoadSizes(2, true);
                });
            }
            else
            {
                ArrangementInventoryList.ItemsSource = list1;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach (KeyValuePair<long, string> kvp in containerSizes)
                {
                    list2.Add(kvp);
                }

                Size.ItemsSource = list2;
            }
        }

        private void ShowFoliageData(List<FoliageInventoryDTO> foliageData, bool isAsync = false)
        {
            foliage = foliageData;

            ObservableCollection<ArrangementInventoryFilteredItem> list1 = new ObservableCollection<ArrangementInventoryFilteredItem>();

            foreach (FoliageInventoryDTO p in foliageData)
            {
                list1.Add(p.ToFilteredItem());
            }

            if (isAsync)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ArrangementInventoryList.ItemsSource = list1;

                    LoadSizes(4, true);
                });
            }
            else
            {
                ArrangementInventoryList.ItemsSource = list1;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach (KeyValuePair<long, string> kvp in foliageSizes)
                {
                    list2.Add(kvp);
                }

                Size.ItemsSource = list2;
            }
        }

        private void ShowMaterialData(List<MaterialInventoryDTO> materialData, bool isAsync = false)
        {
            materials = materialData;

            ObservableCollection<ArrangementInventoryFilteredItem> list1 = new ObservableCollection<ArrangementInventoryFilteredItem>();

            foreach (MaterialInventoryDTO p in materialData)
            {
                list1.Add(p.ToFilteredItem());
            }

            if (isAsync)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ArrangementInventoryList.ItemsSource = list1;

                    LoadSizes(5, true);
                });
            }
            else
            {
                ArrangementInventoryList.ItemsSource = list1;

                ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

                foreach (KeyValuePair<long, string> kvp in materialSizes)
                {
                    list2.Add(kvp);
                }

                Size.ItemsSource = list2;
            }
        }

        private void MaterialTypesLoaded(long inventoryTypeId, ObservableCollection<KeyValuePair<long, string>> list1, GetMaterialTypeResponse types)
        {
            materialTypes = types.MaterialTypes;
            
            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (MaterialTypeDTO materialType in materialTypes)
                {
                    list1.Add(new KeyValuePair<long, string>(materialType.MaterialTypeId, materialType.MaterialTypeName));
                }

                Type.ItemsSource = list1;

                LoadListData(inventoryTypeId);
            });
        }

        private void FoliageTypesLoaded(long inventoryTypeId, ObservableCollection<KeyValuePair<long, string>> list1, GetFoliageTypeResponse types)
        {
            foliageTypes = types.FoliageTypes;
            
            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (FoliageTypeDTO foliage in foliageTypes)
                {
                    list1.Add(new KeyValuePair<long, string>(foliage.FoliageTypeId, foliage.FoliageTypeName));
                }

                Type.ItemsSource = list1;

                LoadListData(inventoryTypeId);
            });
        }

        private void ContainerTypesLoaded(long inventoryTypeId,ObservableCollection<KeyValuePair<long, string>> list1, GetContainerTypeResponse types)
        {
            containerTypes = types.ContainerTypeList;
            
            Device.BeginInvokeOnMainThread(() =>
            {
                foreach (ContainerTypeDTO container in containerTypes)
                {
                    list1.Add(new KeyValuePair<long, string>(container.ContainerTypeId, container.ContainerTypeName));
                }

                Type.ItemsSource = list1;

                LoadListData(inventoryTypeId);
            });
        }

        private void PlantNamesLoaded(ObservableCollection<KeyValuePair<long, string>> list1, GetPlantNameResponse names)
        {
            plantNames = names.PlantNames;

            foreach (PlantNameDTO plantName in plantNames)
            {
                list1.Add(new KeyValuePair<long, string>(plantName.PlantNameId, plantName.PlantName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Name.ItemsSource = list1;

                SearchButton_Click(null, new EventArgs());
            });
        }

        private void MaterialNamesLoaded(ObservableCollection<KeyValuePair<long, string>> list1, GetMaterialNameResponse names)
        {
            materialNames = names.MaterialNames;

            foreach (MaterialNameDTO materialName in materialNames)
            {
                list1.Add(new KeyValuePair<long, string>(materialName.MaterialNameId, materialName.MaterialName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Name.ItemsSource = list1;

                SearchButton_Click(null, new EventArgs());
            });
        }

        private void FoliageNamesLoaded(ObservableCollection<KeyValuePair<long, string>> list1, GetFoliageNameResponse names)
        {
            foliageNames = names.FoliageNames;

            foreach (FoliageNameDTO foliageName in foliageNames)
            {
                list1.Add(new KeyValuePair<long, string>(foliageName.FoliageNameId, foliageName.FoliageName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Name.ItemsSource = list1;

                SearchButton_Click(null, new EventArgs());
            });
        }

        private void ContainerNamesLoaded(ObservableCollection<KeyValuePair<long, string>> list1, List<ContainerNameDTO> names)
        {
            containerNames = names;

            foreach (ContainerNameDTO containerName in containerNames)
            {
                list1.Add(new KeyValuePair<long, string>(containerName.ContainerNameId, containerName.ContainerName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Name.ItemsSource = list1;

                SearchButton_Click(null, new EventArgs());
            });
           
        }

        private void PlantsByTypeLoaded(ObservableCollection<ArrangementInventoryFilteredItem> list1, string name, string size, GetPlantResponse response)
        {
            plants = response.PlantInventoryList;

            List<PlantInventoryDTO> filtered = new List<PlantInventoryDTO>();

            filtered.AddRange(plants);

            if (!String.IsNullOrEmpty(name))
            {
                filtered = plants.Where(a => a.Plant.PlantName.Contains(name)).ToList();
            }

            if (!String.IsNullOrEmpty(size))
            {
                filtered = plants.Where(a => a.Plant.PlantSize.Equals(size)).ToList();
            }

            foreach (PlantInventoryDTO p in filtered)
            {
                list1.Add(new ArrangementInventoryFilteredItem()
                {
                    Id = p.Inventory.InventoryId,
                    Type = p.Inventory.InventoryName,
                    InventoryTypeId = p.Inventory.InventoryTypeId,
                    Name = p.Plant.PlantName,
                    Size = p.Plant.PlantSize,
                    ServiceCodeId = p.Inventory.ServiceCodeId,
                    ServiceCode = p.Inventory.ServiceCodeName,
                    ImageId = p.ImageId
                });
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                ArrangementInventoryList.ItemsSource = list1;
            });
        }

        private void MaterialsByTypeLoaded(ObservableCollection<ArrangementInventoryFilteredItem> list1, string name, string size, GetMaterialResponse response)
        {
            materials = response.MaterialInventoryList;

            List<MaterialInventoryDTO> filtered = new List<MaterialInventoryDTO>();

            filtered.AddRange(materials);

            if (!String.IsNullOrEmpty(name))
            {
                filtered = materials.Where(a => a.Material.MaterialName.Contains(name)).ToList();
            }

            foreach (MaterialInventoryDTO m in filtered)
            {
                list1.Add(new ArrangementInventoryFilteredItem()
                {
                    Id = m.Inventory.InventoryId,
                    Type = m.Inventory.InventoryName,
                    InventoryTypeId = m.Inventory.InventoryTypeId,
                    Name = m.Material.MaterialName,
                    Size = m.Material.MaterialSize,
                    ServiceCodeId = m.Inventory.ServiceCodeId,
                    ServiceCode = m.Inventory.ServiceCodeName,
                    ImageId = m.ImageId
                });
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                ArrangementInventoryList.ItemsSource = list1;
            });
        }

        private void FoliageByTypeLoaded(ObservableCollection<ArrangementInventoryFilteredItem> list1, string name, string size, GetFoliageResponse response)
        {
            foliage = response.FoliageInventoryList;

            List<FoliageInventoryDTO> filtered = new List<FoliageInventoryDTO>();

            filtered.AddRange(foliage);

            if (!String.IsNullOrEmpty(name))
            {
                filtered = foliage.Where(a => a.Foliage.FoliageName.Contains(name)).ToList();
            }

            foreach (FoliageInventoryDTO f in filtered)
            {
                list1.Add(new ArrangementInventoryFilteredItem()
                {
                    Id = f.Inventory.InventoryId,
                    Type = f.Inventory.InventoryName,
                    InventoryTypeId = f.Inventory.InventoryTypeId,
                    Name = f.Foliage.FoliageName,
                    Size = f.Foliage.FoliageSize,
                    ServiceCodeId = f.Inventory.ServiceCodeId,
                    ServiceCode = f.Inventory.ServiceCodeName,
                    ImageId = f.ImageId
                });
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                ArrangementInventoryList.ItemsSource = list1;
            });
        }

        private void ContainersByTypeLoaded(ObservableCollection<ArrangementInventoryFilteredItem> list1, string name, string size, GetContainerResponse response)
        {
            containers = response.ContainerInventoryList;

            List<ContainerInventoryDTO> filtered = new List<ContainerInventoryDTO>();

            filtered.AddRange(containers);

            if (!String.IsNullOrEmpty(name))
            {
                filtered = containers.Where(a => a.Container.ContainerName.Contains(name)).ToList();
            }

            foreach (ContainerInventoryDTO c in filtered)
            {
                list1.Add(new ArrangementInventoryFilteredItem()
                {
                    Id = c.Inventory.InventoryId,
                    Type = c.Container.ContainerTypeName,
                    InventoryTypeId = c.Inventory.InventoryTypeId,
                    Name = c.Inventory.InventoryName,
                    Size = c.Container.ContainerSize,
                    ServiceCodeId = c.Inventory.ServiceCodeId,
                    ServiceCode = c.Inventory.ServiceCodeName,
                    ImageId = c.ImageId
                });
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                ArrangementInventoryList.ItemsSource = list1;
            });
        }

        private void ArrangementFilterCancel_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private void ViewImage_Clicked(object sender, EventArgs e)
        {
            IReadOnlyList<Rg.Plugins.Popup.Pages.PopupPage> popupStack = Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack;

            //One at a time, please
            if (popupStack != null && popupStack.Count > 0)
            {
                return;
            }

            Button b = sender as Button;
            b.IsEnabled = false;

            try
            {
                ArrangementInventoryFilteredItem item = (ArrangementInventoryFilteredItem)((Button)sender).BindingContext;

                if (item != null)
                {
                    long itemImageId = ((App)App.Current).MissingImageId;
                    if (item.ImageId != 0)
                    {
                        itemImageId = item.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(itemImageId);

                    ((App)App.Current).GetServiceCodeById(item.ServiceCodeId).ContinueWith(a => ShowImage(img, a.Result));
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                b.IsEnabled = true;
            }
        }

        private void ShowImage(EOImgData img, ServiceCodeDTO serviceCode)
        {
            string price = string.Empty;
            if (serviceCode.ServiceCodeId > 0)
            {
                price = (serviceCode.Price.HasValue ? serviceCode.Price.Value.ToString("C2", CultureInfo.CurrentCulture) : String.Empty);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                PopupImagePage popup = new PopupImagePage(img, price);

                Navigation.PushPopupAsync(popup);
            });
        }
    }
}