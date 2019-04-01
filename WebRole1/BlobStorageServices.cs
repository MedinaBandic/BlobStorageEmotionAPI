using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WebRole1
{
    public class BlobStorageServices
    {
        public CloudBlobContainer GetCloudBlobContainer()
        {
            //Parse the connection string
            CloudStorageAccount account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("ImageStorageAccount"));

            //Create a client
            CloudBlobClient client = account.CreateCloudBlobClient();

            //Create a container
            CloudBlobContainer container = client.GetContainerReference("samples");
            if (container.CreateIfNotExists())
            {
                container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }
            return container;
        }
    }
}