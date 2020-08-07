using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Media;
using Android.Provider;
using Android.Support.V4.Content;
using EOMobile.Droid;
using EOMobile.Interfaces;
using ViewModels.DataModels;
using Xamarin.Forms;

[assembly: Dependency(typeof(XamarinFormsCamera.Droid.CameraAndroid))]
namespace XamarinFormsCamera.Droid
{
    public class CameraAndroid : ICameraInterface
    {
        public Java.IO.File cameraFile;
        Java.IO.File dirFile;
        public static int PICK_PHOTO_CODE = 1046;

        public void LaunchCamera(FileFormatEnum fileType, string imageId)
        {
            /*
             *  TO DO: Permission checking needs to be added, with the latest updates
             *  to Android OS, users can now turn an app's access to the camera off (just like on iOS)
             */

            Intent takePictureIntent = new Intent(MediaStore.ActionImageCapture);

            // Ensure that there's a camera activity to handle the intent
            if (takePictureIntent.ResolveActivity(MainActivity.ActivityContext.PackageManager) != null)
            {
                try
                {                    
                    var documentsDirectry = MainActivity.ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                    cameraFile = new Java.IO.File(documentsDirectry, imageId + "." + fileType.ToString());

                    if (cameraFile != null)
                    {
                        using (var mediaStorageDir = new Java.IO.File(documentsDirectry, string.Empty))
                        {
                            if (!mediaStorageDir.Exists())
                            {
                                if (!mediaStorageDir.Mkdirs())
                                    throw new IOException("Couldn't create directory, have you added the WRITE_EXTERNAL_STORAGE permission?");
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    // Error occurred while creating the File
                    Console.WriteLine(ex);
                }

                try
                {
                    var documentsDirectory = MainActivity.ActivityContext.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
                    cameraFile = new Java.IO.File(documentsDirectory, imageId + "." + fileType.ToString());

                    //NOTE: Make sure the authority matches what you put in the AndroidManifest.xml file
                    Android.Net.Uri photoURI = FileProvider.GetUriForFile(Forms.Context.ApplicationContext, "com.ElegantOrchids.EOMobile.fileprovider", cameraFile);

                    //takePictureIntent.PutExtra(MediaStore.ExtraOutput, photoURI);

                    ((Activity)MainActivity.ActivityContext).StartActivityForResult(takePictureIntent, 0);
                }
                catch(Exception ex)
                {

                }
            }
        }

        public void LaunchGallery(FileFormatEnum fileType, string imageId)
        {
            try
            {
                var documentsDirectory = ((Activity)MainActivity.ActivityContext).GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);

                var files = Directory.EnumerateFiles(documentsDirectory.AbsolutePath, "*.*");

                string a, b = "";
                foreach (string currentFile in files)
                {
                    a = currentFile;
                    b = b + "\n" + a;
                }

                //get thumbnails and display in a grid

                var imageIntent = new Intent();
                imageIntent.SetType("image/*");

                if (imageId != null)
                {
                    imageIntent.PutExtra("fileName", imageId + "." + fileType.ToString());
                }

                imageIntent.SetAction(Intent.ActionGetContent);
                imageIntent.AddCategory(Intent.CategoryOpenable);


                ((Activity)MainActivity.ActivityContext).StartActivityForResult(Intent.CreateChooser(imageIntent, "Select photo"), 1);

                //Android.Net.Uri.Parse(documentsDirectory.AbsolutePath);

                //Intent intent = new Intent(Intent.ActionPick, Android.Net.Uri.Parse(documentsDirectory.AbsolutePath));

                //////////////////////
                //Android.Net.Uri photoURI = FileProvider.GetUriForFile(Forms.Context.ApplicationContext, "com.ElegantOrchids.EOMobile.fileprovider", new Java.IO.File("EOImage_10-5-2019_1.JPEG"));

                //Intent intent = new Intent(Intent.ActionPick, photoURI);

                //Intent intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.InternalContentUri);

                //((Activity)MainActivity.ActivityContext).StartActivityForResult(Intent.CreateChooser(intent, "Select photo"), 1);
            }
            catch(Exception ex)
            {

            }
        }

        public void DeleteImageFromStorage(List<EOImgData> imageData)
        {
            try
            {
                foreach (EOImgData img in imageData)
                {
                    Java.IO.File file = new Java.IO.File(img.fileName);
                    file.Delete();
                    MediaScannerConnection.ScanFile(MainActivity.ActivityContext.ApplicationContext, new String[] { file.Path }, null, null);
                }

                imageData.Clear();
            }
            catch(Exception ex)
            {

            }
        }
    }
}
