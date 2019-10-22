//---------------------------------------------------------------------------------- 
// Copyright (c) Microsoft Corporation. All rights reserved. 
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,  
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES  
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
//---------------------------------------------------------------------------------- 
// The example companies, organizations, products, domain names, 
// e-mail addresses, logos, people, places, and events depicted 
// herein are fictitious.  No association with any real company, 
// organization, product, domain name, email address, logo, person, 
// places, or events is intended or should be inferred. 
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace WebApp_Storage_DotNet.Controllers
{
    /// <summary> 
    /// Azure Blob Storage Photo Gallery - Demonstrates how to use the Blob Storage service.  
    /// Blob storage stores unstructured data such as text, binary data, documents or media files.  
    /// Blobs can be accessed from anywhere in the world via HTTP or HTTPS. 
    /// </summary> 
    public class HomeController : Controller
    {   
        const string BlobContainerName = "webappstoragedotnet-imagecontainer";
        BlobContainerClient blobContainer;
        static bool containerCreated = false;

        public HomeController()
        {
            if (!containerCreated)
            {
                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"].ToString();

                // Create a client to interact with blob service.
                BlobServiceClient client = new BlobServiceClient(connectionString);

                // Get the container named "webappstoragedotnet-imagecontainer" from this storage account.
                // You need create this container in this storage account first if you want interact with truly Azure service.
                blobContainer = client.GetBlobContainerClient(BlobContainerName);
                blobContainer.CreateIfNotExists();

                // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate  
                // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions  
                // to allow public access to blobs in this container.Then you can view the image  
                blobContainer.SetAccessPolicyAsync(PublicAccessType.Blob);

                containerCreated = true;
            }
        }

        #region controller
        /// <summary> 
        /// ActionResult Index() 
        /// Documentation References:  
        /// - What is a Storage Account: http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/ 
        /// - Create a Storage Account: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#create-an-azure-storage-account
        /// - Create a Storage Container: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#create-a-container
        /// - List all Blobs in a Storage Container: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#list-the-blobs-in-a-container
        /// </summary> 
        public async Task<ActionResult> Index()
        {
            try
            {
                await InitClientAsync();
                Uri prefix = blobContainer.Uri;

                // Gets all Cloud Block Blobs in the BlobContainerName and passes them to teh view
                List<Uri> allBlobs = new List<Uri>();
                foreach (BlobItem blob in blobContainer.GetBlobs())
                {
                    if (blob.Properties.BlobType == BlobType.BlockBlob)
                    {
                        allBlobs.Add(new Uri($"{prefix}/{blob.Name}"));
                    }
                }
                
                return View(allBlobs);
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }       
        }

        /// <summary> 
        /// Task<ActionResult> UploadAsync() 
        /// Documentation References:  
        /// - UploadAsync Method: https://azure.github.io/azure-sdk-for-net/api/Azure.Storage.Blobs/Azure.Storage.Blobs.BlobClient.html#methods
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> UploadAsync()
        {
            try
            {
                await InitClientAsync();

                HttpFileCollectionBase files = Request.Files;
                int fileCount = files.Count;

                if (fileCount > 0)
                {
                    for (int i = 0; i < fileCount; i++)
                    {
                        BlobClient blob = blobContainer.GetBlobClient(GetRandomBlobName(files[i].FileName));
                        await blob.UploadAsync(files[i].InputStream);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }            
        }

        /// <summary> 
        /// Task<ActionResult> DeleteImage(string name) 
        /// Documentation References:  
        /// - Delete Blobs : https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#delete-blobs
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteImage(string name)
        {
            try
            {
                await InitClientAsync();

                Uri uri = new Uri(name);
                string filename = Path.GetFileName(uri.LocalPath);
                var blob = blobContainer.GetBlobClient(filename);
                await blob.DeleteAsync();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        /// <summary> 
        /// Task<ActionResult> DeleteAll(string name) 
        /// Documentation References:  
        /// - Delete Blobs: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#delete-blobs
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await InitClientAsync();

                foreach (BlobItem blob in blobContainer.GetBlobs())
                {
                    if (blob.Properties.BlobType == BlobType.BlockBlob)
                    {
                        await blobContainer.DeleteBlobAsync(blob.Name);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }
        #endregion

        #region helpers
        /// <summary> 
        /// string GetRandomBlobName(string filename): Generates a unique random file name to be uploaded  
        /// </summary> 
        private string GetRandomBlobName(string filename)
        {
            string ext = Path.GetExtension(filename);
            return string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);
        }

        /// <summary>
        /// Initialize a blobContainer client 
        /// </summary>
        private async Task InitClientAsync()
        {
            if (blobContainer == null && containerCreated)
            {
                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"].ToString();

                // Create a client to interact with blob service.
                BlobServiceClient client = new BlobServiceClient(connectionString);

                // Get the container named "webappstoragedotnet-imagecontainer" from this storage account.
                // You need create this container in this storage account first if you want interact with truly Azure service.
                blobContainer = client.GetBlobContainerClient(BlobContainerName);
            }
        }
        #endregion
    }
}
