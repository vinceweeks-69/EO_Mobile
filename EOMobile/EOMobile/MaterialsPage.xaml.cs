using Newtonsoft.Json;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using ViewModels.ControllerModels;
using ViewModels.DataModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MaterialsPage : EOBasePage
	{
        public string User
        {
            get { return ((App)App.Current).User; }
            set { ((App)App.Current).User = value; }
        }

        public string Pwd
        {
            get { return ((App)App.Current).Pwd; }
            set { ((App)App.Current).Pwd = value; }
        }

        List<MaterialInventoryDTO> materials = new List<MaterialInventoryDTO>();

        ObservableCollection<MaterialInventoryDTO> list2 = new ObservableCollection<MaterialInventoryDTO>();

        public MaterialsPage ()
		{
			InitializeComponent ();

            ((App)App.Current).GetMaterialTypes().ContinueWith(a => MaterialTypesLoaded(a.Result));

            MaterialName.SelectedIndexChanged += MaterialName_SelectedIndexChanged;

            MaterialSize.SelectedIndexChanged += MaterialSize_SelectedIndexChanged;

            ((App)App.Current).GetMaterials().ContinueWith(a => MaterialsLoaded(a.Result));
           
        }

        private void MaterialTypesLoaded(GetMaterialTypeResponse materialTypes)
        {
            ObservableCollection<KeyValuePair<long, string>> list1 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (MaterialTypeDTO code in materialTypes.MaterialTypes)
            {
                list1.Add(new KeyValuePair<long, string>(code.MaterialTypeId, code.MaterialTypeName));
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                MaterialType.ItemsSource = list1;

                MaterialType.SelectedIndexChanged += MaterialType_SelectedIndexChanged;
            });
        }

        private void MaterialsLoaded(GetMaterialResponse response)
        {
            materials = response.MaterialInventoryList;

            foreach (MaterialInventoryDTO m in materials)
            {
                list2.Add(m);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                materialListView.ItemsSource = list2;
            });
        }

        private void MaterialSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(MaterialSize.SelectedItem != null)
            {

            }
        }

        private void MaterialName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                MaterialSize.SelectedIndex = -1;

                if (MaterialName.SelectedItem != null)
                {
                    long selectedValue = ((KeyValuePair<long, string>)MaterialName.SelectedItem).Key;
                    string selectedMaterialName = ((KeyValuePair<long, string>)MaterialName.SelectedItem).Value;

                    ObservableCollection<MaterialInventoryDTO> mDTO = new ObservableCollection<MaterialInventoryDTO>();

                    foreach (MaterialInventoryDTO m in materials.Where(a => a.Material.MaterialName == selectedMaterialName))
                    {
                        mDTO.Add(m);
                    }

                    materialListView.ItemsSource = mDTO;
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void MaterialType_SelectedIndexChanged(object sender, EventArgs e)
        {
            MaterialName.SelectedIndex = -1;
            MaterialSize.SelectedIndex = -1;

            long selectedValue = ((KeyValuePair<long, string>)MaterialType.SelectedItem).Key;

            ((App)App.Current).GetMaterialByType(selectedValue).ContinueWith(a => MaterialsByTypeLoaded(selectedValue, a.Result));
        }

        private void MaterialsByTypeLoaded(long selectedValue, GetMaterialResponse response)
        {
            materials = response.MaterialInventoryList;

            ObservableCollection<KeyValuePair<long, string>> list2 = new ObservableCollection<KeyValuePair<long, string>>();

            foreach (MaterialInventoryDTO resp in materials)
            {
                list2.Add(new KeyValuePair<long, string>(resp.Material.MaterialId, resp.Material.MaterialName));
            }

            ObservableCollection<MaterialInventoryDTO> mDTO = new ObservableCollection<MaterialInventoryDTO>();

            foreach (MaterialInventoryDTO m in materials.Where(a => a.Material.MaterialTypeId == selectedValue))
            {
                mDTO.Add(m);
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                MaterialName.ItemsSource = list2;

                materialListView.ItemsSource = mDTO;
            });
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
                MaterialInventoryDTO material = (MaterialInventoryDTO)((Button)sender).BindingContext;

                if (material != null)
                {
                    long materialImageId = ((App)App.Current).MissingImageId;
                    if (material.ImageId != 0)
                    {
                        materialImageId = material.ImageId;
                    }

                    EOImgData img = ((App)App.Current).GetImage(materialImageId);

                    if (materialImageId == ((App)App.Current).MissingImageId)
                    {
                        MessagingCenter.Send<MaterialInventoryDTO>(material, "MaterialMissingImage");
                    }

                    ((App)App.Current).GetServiceCodeById(material.Inventory.ServiceCodeId).ContinueWith(a => ShowImage(img, a.Result));
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

        private void Help_MaterialsPage_Clicked(object sender, EventArgs e)
        {
            if (!PageExists(typeof(HelpPage)))
            {
                TaskAwaiter t = Navigation.PushAsync(new HelpPage("MaterialsPage")).GetAwaiter();
            }
        }
    }
}