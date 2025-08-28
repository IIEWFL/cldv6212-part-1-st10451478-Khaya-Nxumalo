using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareStorageService _fileShareStorageService;

        public CustomerController(TableStorageService tableStorageService,
                        BlobStorageService blobStorageService,
                        QueueStorageService queueStorageService,
                        FileShareStorageService fileShareStorageService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            _fileShareStorageService = fileShareStorageService;
        }
        //GET: Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }
        // GET: Customer/Create

        public IActionResult Create()
        {
            return View();
        }

        // In the Create action, move the queue message sending code before the return statement to fix CS0162

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, IFormFile photo)
        {
            if (ModelState.IsValid)
            {
                //Upload photo to blob and return the SAS URI
                if (photo != null)
                {
                    using var stream = photo.OpenReadStream();
                    customer.PhotoUrl = await _blobStorageService.UploadPhotoAsync(Guid.NewGuid().ToString(), stream);
                }

                //Add student to table storage
                await _tableStorageService.AddCustomerAsync(customer);

                //Send Message to the queue
                var message = new
                {
                    Action = "New Customer created",
                    Timestamp = DateTime.UtcNow,
                    Details = new
                    {
                        customer.PartitionKey,
                        customer.RowKey,
                        customer.PhoneNumber,
                        customer.Name,
                        customer.Email,
                        customer.Address
                    }
                };
                await _queueStorageService.SendMessagesAsync(System.Text.Json.JsonSerializer.Serialize(message));

                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        //GET: Student/Details/{partitionKey} + {rowKey}
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //GET: Student/Edit/{partitionKey} + {rowKey}
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            return View(customer);
        }

        //POST: Wtih Blob Storage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer, IFormFile photo)
        {
            // Remove the photo error from ModelState if no new photo was uploaded
            if (photo == null || photo.Length == 0)
            {
                ModelState.Remove("photo");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for null partitionKey and rowKey
                    if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
                    {
                        ModelState.AddModelError(string.Empty, "Invalid student data.");
                        return View(customer);
                    }

                    // Retrieve the existing student from Table Storage
                    var existingCustomer = await _tableStorageService.GetCustomerAsync(customer.PartitionKey, customer.RowKey);

                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Check if a new photo was uploaded
                    if (photo != null && photo.Length > 0)
                    {
                        // Delete the old photo if it exists
                        if (!string.IsNullOrEmpty(existingCustomer.PhotoUrl))
                        {
                            var blobName = Path.GetFileName(new Uri(existingCustomer.PhotoUrl).AbsolutePath);
                            await _blobStorageService.DeletePhotoAsync(blobName);
                        }

                        // Upload the new photo
                        using var stream = photo.OpenReadStream();
                        customer.PhotoUrl = await _blobStorageService.UploadPhotoAsync(Guid.NewGuid().ToString(), stream);
                    }
                    else
                    {
                        // Preserve the existing PhotoUrl
                        customer.PhotoUrl = existingCustomer.PhotoUrl;
                    }

                    // Update the student in Table Storage
                    await _tableStorageService.UpdateCustomerAsync(customer);

                    //send message to the queue
                    var message = new
                    {
                        Action = "Customer updated",
                        Timestamp = DateTime.UtcNow,
                        Details = customer

                    };
                    await _queueStorageService.SendMessagesAsync(System.Text.Json.JsonSerializer.Serialize(message));

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception
                    ModelState.AddModelError(string.Empty, $"An error occurred while updating the customer. {ex.Message}");
                }
            }

            // If we got this far, something failed; redisplay form
            return View(customer);
        }

        //GET: Customer/Delete/{partitionKey}/{rowKey}
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null)
            {
                return NotFound();
            }
            return View();
        }

        //POST: Customer/Delete/{partitionKey}/{rowKey}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);

            if (customer != null && !string.IsNullOrEmpty(customer.PhotoUrl))
            {
                var blobName = Path.GetFileName(new Uri(customer.PhotoUrl).AbsolutePath);
                await _blobStorageService.DeletePhotoAsync(blobName);
            }

            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
            //Send message to queue
            var message = new
            {
                Action = "Customer deleted",
                Timestamp = DateTime.UtcNow,
                Details = new
                {
                    customerId = rowKey
                }
            };
           await _queueStorageService.SendMessagesAsync(System.Text.Json.JsonSerializer.Serialize(message));

            return RedirectToAction(nameof(Index));
        }
        //GET: CustomerLogs/Log
        [HttpGet]
        public async Task<IActionResult> Log()
        {
            var logMessages = await _queueStorageService.GetMessagesAsync();
            return View(logMessages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportLog()
        {
            var logMessages = await _queueStorageService.GetMessagesAsync();


            var filename = $"Log_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                // Write the header
                await writer.WriteLineAsync("MessageId,InsertionTime,MessageText");

                // Write each log message
                foreach (var log in logMessages)
                {
                    // Escape any double quotes in the message text
                    var messageText = log.MessageText?.Replace("\"", "\"\"");
                    // Ensure fields are enclosed in double quotes
                    await writer.WriteLineAsync($"\"{log.MessageId}\",\"{log.InsertionTime?.ToString("yyyy/MM/dd HH:mm:ss")}\",\"{messageText}\"");
                }
                await writer.FlushAsync();

                // Reset the stream position to the beginning before uploading
                stream.Position = 0;
                await _fileShareStorageService.UploadFileAsync(filename, stream);
            }

            return RedirectToAction(nameof(Index));
        }
    }

}
//C# Corner, 2018. Azure Storage CRUD Operations In MVC Using C# - Azure Table Storage - Part One. [online] Available at: https://www.c-sharpcorner.com/article/azure-storage-crud-operations-in-mvc-using-c-sharp-azure-table-storage-part-one [Accessed 25 Aug. 2025].
//Code Maze, 2022. Azure Table Storage with ASP.NET Core. [online] Available at: https://code-maze.com/azure-table-storage-aspnetcore [Accessed 25 Aug. 2025].
