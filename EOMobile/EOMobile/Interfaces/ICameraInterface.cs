using System;
using System.Collections.Generic;
using System.Text;
using ViewModels.DataModels;

namespace EOMobile.Interfaces
{
    public enum FileFormatEnum
    {
        PNG,
        JPEG
    }

    public interface ICameraInterface
    {
        void LaunchCamera(FileFormatEnum imageType, string imageId = null);
        void LaunchGallery(FileFormatEnum imageType, string imageId = null);
        void DeleteImageFromStorage(List<EOImgData> imageData);
    }
}
