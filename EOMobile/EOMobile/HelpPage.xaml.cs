using LibVLCSharp.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HelpPage : ContentPage
    {
        LibVLC _libvlc;
        string youTubeAuthKey = "AIzaSyCmEfWFHFwn30smv4trs_pXVISTrt7ws4w";

        string getVideo = "https://www.googleapis.com/youtube/v3/videos?id=7lCDEYXw3mM&key=YOUR_API_KEY" +
            "&part=snippet,contentDetails,statistics,status";

        public HelpPage(string pageName)
        {
            InitializeComponent();
            Core.Initialize();
            _libvlc = new LibVLC();

            Title.Text = pageName;

            //load from db
            switch(pageName)
            {
                case "ArrangementPage":
                {
                    HelpContent.Text = "This is the page where Elegant Orchids floral artists create new and modify existing arrangements";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1CRqMoTia8an4G6bWOVeSNg941EEOG4Q6", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "ContainersPage":
                {
                    HelpContent.Text = "This is the page where you select containers based on search criteria.";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1VEoFvLw1HF1-8t7fSa0Vp6g1e_OAvcQr", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "CustomerPage":
                {
                    HelpContent.Text = "This is the page where you  create new and modify existing customer information";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1b_3WEmpKEQPIQZtta4ovSrrz3oNyvmc2", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "FoliagePage":
                {
                    HelpContent.Text = "This is the page where you select foliage based on search criteria.";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1VEoFvLw1HF1-8t7fSa0Vp6g1e_OAvcQr", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "MaterialsPage":
                {
                    HelpContent.Text = "This is the page where you select materials based on search criteria.";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1VEoFvLw1HF1-8t7fSa0Vp6g1e_OAvcQr", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "PaymentPage":
                {
                    HelpContent.Text = "This is the page where you select the customer's method of payment.";
                }
                break;

                case "PlantsPage":
                {
                    HelpContent.Text = "This is the page where you select plants based on search criteria.";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1VEoFvLw1HF1-8t7fSa0Vp6g1e_OAvcQr", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "SchedulerPage":
                {
                    HelpContent.Text = "This is the page where Elegant Orchids floral artists schedule upcoming and recurring events." +
                        "You can send email reminders to yourself, send messages to other people, schedule site service requests, work orders, etc.";

                    //string linkToViewer = "https://drive.google.com/file/d/1En0JS8lsYHbv_-vrO7wy_Iz5q53JQw8O/view?usp=sharing";

                    //string download = "https://drive.google.com/uc?export=download&id=1En0JS8lsYHbv_-vrO7wy_Iz5q53JQw8O";

                    //go to gmail google drive elegantorchids.com for Thom - EO Videos\Mobile App\Help to get down link for new videos

  
                    //TODO: show spinner in control area while data loading
                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1ouhHLSgTfQzk9s3cogEIMvUJOpCCsm8Z", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "ShipmentPage":
                {
                    HelpContent.Text = "This is the page where you record the inventory recieved in a shipment.";

                    //TODO: show spinner in control area while data loading
                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1T8jPl8eyjiqMPLzIdjCP-H_1u5hdrNuJ", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
            break;

                case "SiteServicePage":
                {
                    HelpContent.Text = "This is the page where you record the detail for a Site Service work order.";

                     //TODO: show spinner in control area while data loading
                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1m0UN9gxzEKZEGddagZ-j03YHohACPexI", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
            break;

                case "WorkOrderPage":
                {
                    HelpContent.Text = "This is the page where you record the detail for a work order.";

                    //TODO: show spinner in control area while data loading
                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1gPXaswn60GF7o8WqZz5YzZ5-v0yMv2qr", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;

                case "VendorPage":
                {
                    HelpContent.Text = "This is the page where you select vendors based on search criteria.";

                    Media m = new Media(_libvlc, "https://drive.google.com/uc?export=download&id=1b_3WEmpKEQPIQZtta4ovSrrz3oNyvmc2", FromType.FromLocation);
                    mediaPlayer.MediaPlayer = new MediaPlayer(m) { EnableHardwareDecoding = true };
                    mediaPlayer.MediaPlayer.Play();
                }
                break;
            }
        }

        protected override void OnDisappearing()
        {
            mediaPlayer.MediaPlayer.Stop();

            base.OnDisappearing();
        }
    }
}