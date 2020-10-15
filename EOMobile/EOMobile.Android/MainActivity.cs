using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using Android.Content;
using Android.Media;
using Android.Graphics;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;
using EOMobile.Interfaces;
using Android.Provider;
using Android.Net;
using Java.IO;
using static Android.Provider.MediaStore;
using Android.Database;
using LibVLCSharp.Forms.Shared;
using Android.Net.Wifi;
using SharedData;

namespace EOMobile.Droid
{
    [Activity(Label = "EOMobile", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        //https://stackoverflow.com/questions/47353986/xamarin-forms-forms-context-is-obsolete
        internal static Context ActivityContext { get; private set; }

        public MainActivity()
        {
            IntentFilter filter = new IntentFilter();
            filter.AddAction(WifiManager.NetworkStateChangedAction);
            Android.App.Application.Context.RegisterReceiver(new NetworkBroadcastReceiver(), filter);
        }

        public override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
            {
                // Do something if there are some pages in the `PopupStack`
            }
            else
            {
                // Do something if there are not any pages in the `PopupStack`
                base.OnBackPressed();
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
                       
            base.OnCreate(savedInstanceState);

            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);

            LibVLCSharpFormsRenderer.Init();

            ActivityContext = this;
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
         
            LoadApplication(new App());
        }

        public Uri getImageUri(Bitmap inImage)
        {
            Uri filePath;
            System.IO.MemoryStream bytes = new System.IO.MemoryStream();
            inImage.Compress(Bitmap.CompressFormat.Jpeg, 100, bytes);
            String path = Images.Media.InsertImage(this.ContentResolver, inImage, "Title", null);
            filePath = Uri.Parse(path);
            return filePath;
        }

        public String getRealPathFromURI(Uri uri)
        {
            String temp = "";
            String path = "";
            if (this.ContentResolver != null)
            {
                ICursor cursor = this.ContentResolver.Query(uri, null, null, null, null);
                if (cursor != null)
                {
                    cursor.MoveToFirst();
                    int idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Data);
                    path = cursor.GetString(idx);

                    idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.BucketDisplayName);
                    temp = cursor.GetString(idx);

                    idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.DisplayName);
                    temp = cursor.GetString(idx);

                    idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Id);
                    temp = cursor.GetString(idx);

                    idx = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Description);
                    temp = cursor.GetString(idx);

                    cursor.Close();
                }
            }
            return path;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            Bundle a;
            string message = String.Empty;
            EOImgData result = null;

            if (requestCode == 0)
            {
                Task.Run(() =>
                {                    
                    if (data != null)
                    {
                        if (data.Extras != null)
                        {
                            Bitmap bitmap = (Bitmap)data.Extras.Get("data");

                            // CALL THIS METHOD TO GET THE URI FROM THE BITMAP
                            Uri tempUri = getImageUri(bitmap);

                            // CALL THIS METHOD TO GET THE ACTUAL PATH
                            Java.IO.File finalFile = new Java.IO.File(getRealPathFromURI(tempUri));

                            string fName = finalFile.Name;

                            MemoryStream stream = new MemoryStream();

                            //Console.WriteLine("About to compress");
                            //Compressing by 50%, feel free to change if file size is not a factor
                            bitmap.Compress(Bitmap.CompressFormat.Jpeg, 50, stream);

                            //Console.WriteLine("About to convert to byte array");
                            byte[] bitmapBytes = stream.ToArray();
                                                       

                            var documentsDirectry = ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                            string pngFilename = System.IO.Path.Combine(documentsDirectry.AbsolutePath, App.EOImageId + fName);

                            //if (System.IO.File.Exists(pngFilename))
                            if (System.IO.File.Exists(finalFile.AbsolutePath))
                            {
                                //Java.IO.File file = new Java.IO.File(documentsDirectry, App.EOImageId + "." + FileFormatEnum.JPEG.ToString());
                                //Android.Net.Uri uri = Android.Net.Uri.FromFile(file);
                                Android.Net.Uri uri = Android.Net.Uri.FromFile(finalFile);

                                //Read the meta data of the image to determine what orientation the image should be in
                                //var originalMetadata = new ExifInterface(pngFilename);
                                var originalMetadata = new ExifInterface(finalFile.AbsolutePath);
                                int orientation = GetRotation(originalMetadata);

                                string wtf = GetMetadata(originalMetadata);

                                //var fileName = App.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString();
                                var fileName = App.ImageIdToSave + fName;

                                result = HandleBitmap(uri, orientation, fileName, requestCode).Result;

                                message = "ImageSaved";

                                try
                                {
                                    ContentResolver contentResolver = MainActivity.ActivityContext.ApplicationContext.ContentResolver;

                                    Java.IO.File tempFile = new Java.IO.File(tempUri.Path);
                                    MainActivity.ActivityContext.ApplicationContext.ContentResolver.Delete(tempUri, null, null);
                                    bool fileDeleted = tempFile.Delete();

                                    DeleteFromGallery(fName);

                                }
                                catch (Exception ex)
                                {
                                    int debug = 1;
                                }
                            }
                        }
                    }
                }).GetAwaiter().GetResult();

                if(!String.IsNullOrEmpty(message) && result != null)
                {
                    MessagingCenter.Send<EOImgData>(result, message);
                }
            }
            else if (requestCode == 1)
            {
                if (resultCode == Result.Ok)
                {
                    if (data.Data != null)
                    {
                        //Grab the Uri which is holding the path to the image
                        Android.Net.Uri uri = data.Data;

                        string fileName = null;

                        if (App.EOImageId != null)
                        {
                            fileName = App.EOImageId + "." + FileFormatEnum.JPEG.ToString();
                            //Path and camera created filename - not the EO_date_ndx  name of the file saved to"external" storage
                            var pathToImage = GetPathToImage(uri);
                            var originalMetadata = new ExifInterface(pathToImage);
                            int orientation = GetRotation(originalMetadata);

                            //EOImgData result = HandleBitmap(uri, orientation, fileName, requestCode).Result;

                            //MessagingCenter.Send<string>("WTF?", "XXX");

                            //MessagingCenter.Send<byte[]>(result.imgData, "ImageSaved");
                        }
                    }
                }
            }
        }

        private void DeleteFromGallery(string fileName)
        {
            ContentResolver contentResolver = MainActivity.ActivityContext.ApplicationContext.ContentResolver;

            int index = fileName.LastIndexOf(".");
            fileName = fileName.Substring(0, index - 1);

            long ticks = Convert.ToInt64(fileName);
            DateTime dt = new DateTime(ticks);

            Java.IO.File file = new Java.IO.File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim), "/Camera");

            var images = file.ListFiles();

            foreach(Java.IO.File f in file.ListFiles())
            {
                f.Delete();
                MediaScannerConnection.ScanFile(MainActivity.ActivityContext.ApplicationContext, new String[] { f.Path }, null, null);
                //contentResolver.Delete(Uri.FromFile(f), null, null);
            }

            //var file2 = new Java.IO.File(file.AbsolutePath, fileName);

            //bool deleted = file2.Delete();
        }



        private string GetPathToImage(Android.Net.Uri uri)
        {
            string doc_id = "";
            using (var c1 = ContentResolver.Query(uri, null, null, null, null))
            {
                c1.MoveToFirst();
                String document_id = c1.GetString(0);
                doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
            }

            string path = null;

            // The projection contains the columns we want to return in our query.
            string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? ";
            using (var cursor = ManagedQuery(Android.Provider.MediaStore.Images.Media.ExternalContentUri, null, selection, new string[] { doc_id }, null))
            {
                if (cursor == null) return path;
                var columnIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
                cursor.MoveToFirst();
                path = cursor.GetString(columnIndex);
            }
            return path;
        }

        public string GetMetadata(ExifInterface exif)
        {
            //var wtf = (Android.Media.Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

            var wtf = exif.GetAttribute("ThumbnailImage");

            wtf = exif.GetAttribute("DateTime");

            wtf = exif.GetAttribute("DateTimeOriginal");

            wtf = exif.GetAttribute("DateTimeDigitized");

            int debug = 1;

            return wtf;
        }

        public int GetRotation(ExifInterface exif)
        {
            try
            {
                var orientation = (Android.Media.Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

                switch (orientation)
                {
                    case Android.Media.Orientation.Rotate90:
                        return 90;
                    case Android.Media.Orientation.Rotate180:
                        return 180;
                    case Android.Media.Orientation.Rotate270:
                        return 270;
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex);
                return 0;
            }
        }

        public async Task<EOImgData> HandleBitmap(Android.Net.Uri uri, int orientation, string imageId, int operation)
        {
            EOImgData imgData = new EOImgData();

            try
            {
                Bitmap mBitmap = Android.Provider.MediaStore.Images.Media.GetBitmap(this.ContentResolver, uri);
                Bitmap myBitmap = null;
                string pngFileName = String.Empty;

                if (mBitmap != null)
                {
                    //In order to rotate the image we create a Matrix object, rotate if the image is not already in it's correct orientation
                    Matrix matrix = new Matrix();
                    if (orientation != 0)
                    {
                        matrix.PreRotate(orientation);
                    }

                    //Console.WriteLine("About to rotate");
                    myBitmap = Bitmap.CreateBitmap(mBitmap, 0, 0, mBitmap.Width, mBitmap.Height, matrix, true);

                    MemoryStream stream = new MemoryStream();

                    //Console.WriteLine("About to compress");
                    //Compressing by 50%, feel free to change if file size is not a factor
                    myBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                    //Console.WriteLine("About to convert to byte array");
                    byte[] bitmapData = stream.ToArray();

                    //Send image byte array back to UI
                    //Console.WriteLine("About to send Image back to UI");

                    if (imageId != null && imageId != "")
                    {
                        pngFileName = SavePictureToDisk(myBitmap, imageId);
                    }

                    imgData.imgData = bitmapData;
                    imgData.fileName = pngFileName;
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }

            return imgData;
        }

        public string SavePictureToDisk(Bitmap source, string imageName)
        {
            string pngFileName = String.Empty;

            try
            {
                Task.Run(() =>
                {
                    var documentsDirectry = ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                    
                    pngFileName = System.IO.Path.Combine(documentsDirectry.AbsolutePath, imageName);

                    //If the image already exists, delete, and make way for the updated one
                    if (System.IO.File.Exists(pngFileName))
                    {
                        System.IO.File.Delete(pngFileName);
                    }

                    using (FileStream fs = new FileStream(pngFileName, FileMode.OpenOrCreate))
                    {
                        source.Compress(Bitmap.CompressFormat.Jpeg, 50, fs);
                        fs.Close();
                    }

                    //Console.WriteLine("Saved photo");
                }).Wait();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }

            return pngFileName;
        }
    }
}