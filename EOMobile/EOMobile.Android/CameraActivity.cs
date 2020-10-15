using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using EOMobile.Interfaces;
using EOMobile.XamarinFormsCamera;
using SharedData;

namespace EOMobile.Droid
{
    [Activity(Label = "CameraActivity")]
    public class CameraActivity :  global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        //https://stackoverflow.com/questions/47353986/xamarin-forms-forms-context-is-obsolete
        internal static Context ActivityContext { get; set; }

        public CameraActivity()
        {
            ActivityContext = this;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            //TabLayoutResource = Resource.Layout.Tabbar;
            //ToolbarResource = Resource.Layout.Toolbar;

            if (savedInstanceState != null)
            {
                base.OnCreate(savedInstanceState);

                global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            }

            ActivityContext = this;

            //LoadApplication(new CameraApp());
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            //Since we set the request code to 1 for both the camera and photo gallery, that's what we need to check for
            if (requestCode == 0)
            {
                if (resultCode == Result.Ok)
                {
                    Task.Run(() =>
                    {
                        if (CameraApp.ImageIdToSave != null)
                        {
                            var documentsDirectry = ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                            string pngFilename = System.IO.Path.Combine(documentsDirectry.AbsolutePath, CameraApp.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString());

                            if (File.Exists(pngFilename))
                            {
                                Java.IO.File file = new Java.IO.File(documentsDirectry, CameraApp.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString());
                                Android.Net.Uri uri = Android.Net.Uri.FromFile(file);

                                //Read the meta data of the image to determine what orientation the image should be in
                                var originalMetadata = new ExifInterface(pngFilename);
                                int orientation = GetRotation(originalMetadata);

                                var fileName = CameraApp.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString();
                                HandleBitmap(uri, orientation, fileName);
                            }
                        }
                    });
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

                        if (CameraApp.ImageIdToSave != null)
                        {
                            fileName = CameraApp.ImageIdToSave + "." + FileFormatEnum.JPEG.ToString();
                            var pathToImage = GetPathToImage(uri);
                            var originalMetadata = new ExifInterface(pathToImage);
                            int orientation = GetRotation(originalMetadata);

                            HandleBitmap(uri, orientation, fileName);
                        }
                    }
                }
            }
        }

        /*
         *  https://stackoverflow.com/questions/26597811/xamarin-choose-image-from-gallery-path-is-null
         */

        private string GetPathToImage(Android.Net.Uri uri)
        {
            ICursor cursor = this.ContentResolver.Query(uri, null, null, null, null);
            cursor.MoveToFirst();
            string document_id = cursor.GetString(0);
            document_id = document_id.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new String[] { document_id }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
        }

        //private string GetPathToImage(Android.Net.Uri uri)
        //{
        //    string doc_id = "";
        //    using (var c1 = ContentResolver.Query(uri, null, null, null, null))
        //    {
        //        c1.MoveToFirst();
        //        String document_id = c1.GetString(0);
        //        doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
        //    }

        //    string path = null;

        //    // The projection contains the columns we want to return in our query.
        //    string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? ";
        //    using (var cursor = ManagedQuery(Android.Provider.MediaStore.Images.Media.ExternalContentUri, null, selection, new string[] { doc_id }, null))
        //    {
        //        if (cursor == null) return path;
        //        var columnIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
        //        cursor.MoveToFirst();
        //        path = cursor.GetString(columnIndex);
        //    }
        //    return path;
        //}

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
                Console.WriteLine(ex);
                return 0;
            }
        }

        public async Task HandleBitmap(Android.Net.Uri uri, int orientation, string imageId)
        {
            try
            {
                Bitmap mBitmap = Android.Provider.MediaStore.Images.Media.GetBitmap(this.ContentResolver, uri);
                Bitmap myBitmap = null;

                if (mBitmap != null)
                {
                    //In order to rotate the image we create a Matrix object, rotate if the image is not already in it's correct orientation
                    Matrix matrix = new Matrix();
                    if (orientation != 0)
                    {
                        matrix.PreRotate(orientation);
                    }

                    Console.WriteLine("About to rotate");
                    myBitmap = Bitmap.CreateBitmap(mBitmap, 0, 0, mBitmap.Width, mBitmap.Height, matrix, true);

                    MemoryStream stream = new MemoryStream();

                    Console.WriteLine("About to compress");
                    //Compressing by 50%, feel free to change if file size is not a factor
                    myBitmap.Compress(Bitmap.CompressFormat.Jpeg, 50, stream);

                    Console.WriteLine("About to convert to byte array");
                    byte[] bitmapData = stream.ToArray();

                    //Send image byte array back to UI
                    Console.WriteLine("About to send Image back to UI");

                    string pngFileName = String.Empty;

                    if (imageId != null && imageId != "")
                    {
                        pngFileName = SavePictureToDisk(myBitmap, imageId);
                    }

                    //MessagingCenter.Send<byte[]>(bitmapData, "ImageSelected");
                    //MessagingCenter.Send<string>(pngFileName, "ImageSelected");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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
                    if (File.Exists(pngFileName))
                    {
                        File.Delete(pngFileName);
                    }

                    using (FileStream fs = new FileStream(pngFileName, FileMode.OpenOrCreate))
                    {
                        source.Compress(Bitmap.CompressFormat.Jpeg, 50, fs);
                        fs.Close();
                    }

                    Console.WriteLine("Saved photo");
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return pngFileName;
        }

        public void DeleteImageFromStorage(List<EOImgData> imageData)
        {
            foreach (EOImgData img in imageData)
            {
                Java.IO.File file = new Java.IO.File(img.fileName);
                file.Delete();
                MediaScannerConnection.ScanFile(MainActivity.ActivityContext.ApplicationContext, new String[] { file.Path }, null, null);
            }

            imageData.Clear();
        }
    }
}