using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace WebRole1
{
    public class FaceApiService
    {
        private readonly IFaceServiceClient _faceServiceClient;

        public FaceApiService(string apiKey)
        {
            _faceServiceClient = new FaceServiceClient(apiKey);
        }

        public async Task<byte[]> UploadAndDetectFace(HttpPostedFileBase file)
        {
            byte[] resultImageBytes;

            using (var imageStream = new MemoryStream())
            {
                file.InputStream.CopyTo(imageStream);
                imageStream.Position = 0;

                // Detect faces and get rectangle positions.
                var faces = await DetectFaces(imageStream);
                var facePositions = faces.Select(face => face.FaceRectangle);
                // Draw rectangles over original image.
                using (var img = DrawRectangles(imageStream, facePositions))
                {
                    using (var ms = new MemoryStream())
                    {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        resultImageBytes = ms.ToArray();
                    }
                }
            }

            return resultImageBytes;
        }

        public async Task<IEnumerable<Face>> DetectFaces(Stream imageStream)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    imageStream.CopyTo(stream);
                    stream.Position = 0;

                    var requiredFaceAttributes = new FaceAttributeType[] {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.Smile,
                FaceAttributeType.FacialHair,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Glasses
            };

                    var faces = await _faceServiceClient.DetectAsync(stream, returnFaceLandmarks: true, returnFaceAttributes: requiredFaceAttributes);
                    foreach (var face in faces)
                    {
                        var id = face.FaceId;
                        var attributes = face.FaceAttributes;
                        var age = attributes.Age;
                        var gender = attributes.Gender;
                        var smile = attributes.Smile;
                        var facialHair = attributes.FacialHair;
                        var headPose = attributes.HeadPose;
                        var glasses = attributes.Glasses;
                    }
                    return faces;
                   // return faces.Select(face => face.FaceRectangle);
                }
            }
            catch (Exception)
            {
                return Enumerable.Empty<Face>();
            }
        }

        public Image DrawRectangles(Stream inputStream, IEnumerable<FaceRectangle> facesPosition)
        {
            RectangleF[] rectangles =
                    facesPosition.Select(
                        rectangle => new RectangleF(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height))
                        .ToArray();

            var img = Image.FromStream(inputStream);

            using (var graphics = Graphics.FromImage(img))
            {
                if (rectangles.Any())
                {
                    graphics.DrawRectangles(new Pen(Color.Red, 3), rectangles);
                }
            }

            return img;
        }
    }
}