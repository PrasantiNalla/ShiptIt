﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    [Route("orders/inbound")]
    public class InboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;

        public InboundOrderController(IEmployeeRepository employeeRepository, ICompanyRepository companyRepository, IProductRepository productRepository, IStockRepository stockRepository)
        {
            _employeeRepository = employeeRepository;
            _stockRepository = stockRepository;
            _companyRepository = companyRepository;
            _productRepository = productRepository;
        }

        [HttpGet("{warehouseId}")]
        public InboundOrderResponse Get([FromRoute] int warehouseId)
        {
            Log.Info("orderIn for warehouseId: " + warehouseId);

            //A DB call here
            var operationsManager = new Employee(_employeeRepository.GetOperationsManager(warehouseId));

            Log.Debug(String.Format("Found operations manager: {0}", operationsManager));

            //A DB call here
            //the allStock contains a list of StockDataModels: 
            //Stock Data Models can contain a productID, a Warehouse Id and an int 'held' indicating how many are in the warehouse
            var allStock = _stockRepository.GetStockByWarehouseId(warehouseId);

            //This variable will store all the order lines that have come into this warehouse, sorted by company 
            Dictionary<Company, List<InboundOrderLine>> orderlinesByCompany = new Dictionary<Company, List<InboundOrderLine>>();

            foreach (var stock in allStock)
            {
                //We could get everything outside the forLoop; i.e. get all the product info . Not a great solution
                // 
                //A DB call here
                bool containsCompanyKey = false;
                Product product = new Product(_productRepository.GetProductById(stock.ProductId));
                if (stock.held < product.LowerThreshold && !product.Discontinued)
                {
                    foreach (var companykey in orderlinesByCompany.Keys)
                    {
                        if (companykey.Gcp == product.Gcp)
                        {
                            var ourorderQuantity = Math.Max(product.LowerThreshold * 3 - stock.held, product.MinimumOrderQuantity);
                            orderlinesByCompany[companykey].Add(
                        new InboundOrderLine()
                        {
                            gtin = product.Gtin,
                            name = product.Name,
                            quantity = ourorderQuantity
                        });
                            containsCompanyKey = true;
                            break; // we need to exit from the whole 
                        }
                    }
                    //A DB call here
                    //go into the key of the existing dictionary, and check if product.Gcp == Company.Gcp
                    //if the dictionary's key, which is a Company class instance, contains the gcp attribute which is equal to our product.gcp,
                    // don't bother to make an api call
                    if (!containsCompanyKey)
                    {
                        Company company = new Company(_companyRepository.GetCompany(product.Gcp));

                        var orderQuantity = Math.Max(product.LowerThreshold * 3 - stock.held, product.MinimumOrderQuantity);

                        if (!orderlinesByCompany.ContainsKey(company))
                        {

                            orderlinesByCompany.Add(company, new List<InboundOrderLine>());
                        }
                        orderlinesByCompany[company].Add(
                            new InboundOrderLine()
                            {
                                gtin = product.Gtin,
                                name = product.Name,
                                quantity = orderQuantity
                            });
                    }
                }
            }
            Log.Debug(String.Format("Constructed order lines: {0}", orderlinesByCompany));

            var orderSegments = orderlinesByCompany.Select(ol => new OrderSegment()
            {
                OrderLines = ol.Value,
                Company = ol.Key
            });

            Log.Info("Constructed inbound order");

            return new InboundOrderResponse()
            {
                OperationsManager = operationsManager,
                WarehouseId = warehouseId,
                OrderSegments = orderSegments
            };
        }

        [HttpPost("")]
        public void Post([FromBody] InboundManifestRequestModel requestModel)
        {
            Log.Info("Processing manifest: " + requestModel);

            var gtins = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(String.Format("Manifest contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            IEnumerable<ProductDataModel> productDataModels = _productRepository.GetProductsByGtin(gtins);
            Dictionary<string, Product> products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            Log.Debug(String.Format("Retrieved products to verify manifest: {0}", products));

            var lineItems = new List<StockAlteration>();
            var errors = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(String.Format("Unknown product gtin: {0}", orderLine.gtin));
                    continue;
                }

                Product product = products[orderLine.gtin];
                if (!product.Gcp.Equals(requestModel.Gcp))
                {
                    errors.Add(String.Format("Manifest GCP ({0}) doesn't match Product GCP ({1})",
                        requestModel.Gcp, product.Gcp));
                }
                else
                {
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                }
            }

            if (errors.Count() > 0)
            {
                Log.Debug(String.Format("Found errors with inbound manifest: {0}", errors));
                throw new ValidationException(String.Format("Found inconsistencies in the inbound manifest: {0}", String.Join("; ", errors)));
            }

            Log.Debug(String.Format("Increasing stock levels with manifest: {0}", requestModel));
            _stockRepository.AddStock(requestModel.WarehouseId, lineItems);
            Log.Info("Stock levels increased");
        }
    }
}
