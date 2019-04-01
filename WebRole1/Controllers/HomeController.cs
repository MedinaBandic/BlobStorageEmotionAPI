using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using Microsoft.ProjectOxford.Face;

namespace WebRole1.Controllers
{
    public class HomeController : Controller
    {
        public const string _faceApiKey = "ed57bd56251a4ac58f6e06862fac722f";

        //public const string _emotApiKey = "9444c937d213494facbea9a862cedf86";

       // public const string _apiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        BlobStorageServices _blobStorageServices = new BlobStorageServices();

        public ActionResult Upload()
        {
            CloudBlobContainer container = _blobStorageServices.GetCloudBlobContainer();
            List<string> blobs = new List<string>();
            foreach (var blobItem in container.ListBlobs())
            {
                blobs.Add(blobItem.Uri.ToString());
            }
            return View(blobs);
        }

        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            if (file.ContentLength > 0)
            {

                var apiService = new FaceApiService("9444c937d213494facbea9a862cedf86");

                //byte[] resultImage = await apiService.UploadAndDetectFace(file);
                byte[] resultImageBytes;

                //Detect Faces
                using (var imageStream = new MemoryStream())
                {
                    file.InputStream.CopyTo(imageStream);
                    imageStream.Position = 0;
                    //Call DetectFaces method from Face API service file
                    var faces = await apiService.DetectFaces(imageStream);
                    var attr = new List<string>();
                    foreach(var face in faces)
                    {
                        attr.Add(face.FaceAttributes.Age.ToString());
                        attr.Add(face.FaceAttributes.FacialHair.Beard.ToString());
                        attr.Add(face.FaceAttributes.FacialHair.Moustache.ToString());
                        attr.Add(face.FaceAttributes.FacialHair.Sideburns.ToString());
                        attr.Add(face.FaceAttributes.Gender.ToString());
                        attr.Add(face.FaceAttributes.Glasses.ToString());
                        attr.Add(face.FaceAttributes.HeadPose.Pitch.ToString());
                        attr.Add(face.FaceAttributes.HeadPose.Roll.ToString());
                        attr.Add(face.FaceAttributes.HeadPose.Yaw.ToString());
                        attr.Add(face.FaceAttributes.Smile.ToString());
                    }
                    
                    TempData["attributes"] = attr;
                    var facePositions = faces.Select(face => face.FaceRectangle);
                    
                    // Draw rectangles over original image.
                    using (var img = apiService.DrawRectangles(imageStream, facePositions))
                        {
                            using (var ms = new MemoryStream())
                            {
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                resultImageBytes = ms.ToArray();
                            }
                        }
                    }
                CloudBlobContainer container = _blobStorageServices.GetCloudBlobContainer();
                CloudBlockBlob blob = container.GetBlockBlobReference(file.FileName);
                blob.UploadFromByteArray(resultImageBytes, 0, resultImageBytes.Count());

                TempData["resultImageBase64"] = GetImageBase64String(resultImageBytes);
                return RedirectToAction("ViewFaces");

            }
            
            return RedirectToAction("Upload");
        }
            
        private object GetImageBase64String(byte[] resultImage)
        {
            var imageBased64 = Convert.ToBase64String(resultImage);
            return $"data:image/png;base64, {imageBased64}";
        }

        public ActionResult ViewFaces()
        {
            ViewBag.ImageData = TempData["resultImageBase64"];
            ViewBag.Attributes = TempData["attributes"];
            return View();
        }

        [HttpPost]
        public string DeleteImage(string name)
        {
            Uri uri = new Uri(name);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            CloudBlobContainer container = _blobStorageServices.GetCloudBlobContainer();
            CloudBlockBlob blob = container.GetBlockBlobReference(filename);

            blob.Delete();

            return "File Deleted!";
        }
    }

}