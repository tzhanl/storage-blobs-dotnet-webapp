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

namespace WebApp.Storage.DotNet.Controllers
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

                // Create a container named "webappstoragedotnet-imagecontainer" in this storage account first if you want interact with truly Azure service.
                blobContainer = client.GetBlobContainerClient(BlobContainerName);
                blobContainer.CreateIfNotExists();

                // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate  
                // access to the resource. The second approach is to set permissions to allow public access to blobs in this container.Then you can view the image.  
                blobContainer.SetAccessPolicy(PublicAccessType.Blob);

                containerCreated = true;
            }
        }

        #region controller
        /// <summary> 
        /// ActionResult Index() 
        /// Documentation References:  
        /// - What is a Storage Account: https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json&tabs=azure-portal
        /// - Create a Storage Account: https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account?tabs=azure-portal#create-a-storage-account-1
        /// - Create a Storage Container: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#create-a-container
        /// - List all Blobs in a Storage Container: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#list-the-blobs-in-a-container
        /// </summary> 
        public async Task<ActionResult> Index()
        {
            try
            {
                InitClient();
                Uri prefix = blobContainer.Uri;

                // Gets all Block Blobs in the BlobContainerName and passes them to teh view
                List<Uri> allBlobs = new List<Uri>();
                await foreach (BlobItem blob in blobContainer.GetBlobsAsync())
                {
                    if (blob.Properties.BlobType == BlobType.Block)
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
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> UploadAsync()
        {
            try
            {
                InitClient();

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
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteImage(string name)
        {
            try
            {
                InitClient();

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
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                InitClient();

                await foreach (BlobItem blob in blobContainer.GetBlobsAsync())
                {
                    if (blob.Properties.BlobType == BlobType.Block)
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
        /// void InitClient()
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
        private void InitClient()
        {
            if (blobContainer == null && containerCreated)
            {
                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"].ToString();

                // Create a client to interact with blob service.
                BlobServiceClient client = new BlobServiceClient(connectionString);

                // Get the exist container named "webappstoragedotnet-imagecontainer" from this storage account.
                blobContainer = client.GetBlobContainerClient(BlobContainerName);
            }
        }
        #endregion
    }
}