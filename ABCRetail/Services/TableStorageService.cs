using Azure.Data.Tables;
using ABCRetail.Models;
using System.Collections.Generic;
using Azure;

namespace ABCRetail.Services
{
    public class TableStorageService
    {
        private readonly TableClient _tableClient;

        public TableStorageService(string connectionString, string tableName)
        {
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();

        }

        // Get all customers index.cshtml

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        // Get customer by row key

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // add customer create cshtml

        public async Task AddCustomerAsync(Customer customer)
        {
            // set partition key and row key
            customer.PartitionKey = customer.Address;
            customer.RowKey = Guid.NewGuid().ToString();

            // add customer
            await _tableClient.AddEntityAsync(customer);
        }

        // update customer edit cshtml

        public async Task UpdateCustomerAsync(Customer customer)
        {
            // update customer
            await _tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }

        // delete customer

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        internal Task AddCustomerAsync(Controllers.CustomerController customer)
        {
            throw new NotImplementedException();
        }
        // create product table storage service
        public async Task<List<Customer>> GetProductAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        // Get product by row key

        public async Task<Customer?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // add product create cshtml

        public async Task AddProductAsync(Product product)
        {
            // set partition key and row key
            product.PartitionKey = product.Name;
            product.RowKey = Guid.NewGuid().ToString();

            // add customer
            await _tableClient.AddEntityAsync(product);
        }

        // update product edit cshtml

        public async Task UpdateProductAsync(Product product)
        {
            // update customer
            await _tableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        // delete product

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        


    }
}
