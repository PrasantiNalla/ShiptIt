﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    [Route("orders/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        // At the moment there is no way of knowing how many trucks we are going to need to fulfil each order that we receive.
        // Please improve the OutBoundOrder endpoint so that it shows roughly how many trucks we will need in order to fit everything in the order. (each truck can contain a max of 2000kg)
        // We don’t mind too much what the format is for the new data, but please:
        // give it a nice clear name
        // don’t break any backwards compatibility. (ie don’t change any of the existing data)


        [HttpPost("")]
        //It can't be right to change this into a returning int statement
        public int Post([FromBody] OutboundOrderRequestModel request)
        {
            Log.Info(String.Format("Processing outbound order: {0}", request));
            //Log.Info on number of trucks here 

            var gtins = new List<String>();
            foreach (var orderLine in request.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(String.Format("Outbound order request contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var errors = new List<string>();

            //Change this to a dictionary where the key is the orderlinegtin, 
            //and the value is the Total Weight of that orderline
            List<OrderLineByWeight> ourOrderLinesWeights = new List<OrderLineByWeight>();

            float weightGrams = 0;

            foreach (var orderline in request.OrderLines)
            {
                //we want to run GetProductByGtin as above, to get the product model based on that Gtin...
                //then compare that to the quantity from our OrderLine  
                var ourOrderLineProduct = _productRepository.GetProductByGtin(orderline.gtin);
                float ourOrderLineProductWeight = (float)ourOrderLineProduct.Weight;
                var ourOrderLineProductQuantity = orderline.quantity;
                float ourOrderLineTotalWeight = ourOrderLineProductQuantity * ourOrderLineProductWeight;
                ourOrderLinesWeights.Add(new OrderLineByWeight(orderline.gtin, ourOrderLineTotalWeight));
                weightGrams += (ourOrderLineProductQuantity * ourOrderLineProductWeight);
            }

            float weightKilograms = weightGrams / 1000;
            int NoOfTrucks = (int)Math.Ceiling((weightKilograms) / 2000);
            List<Truck> Trucks = new List<Truck>();
            for (int i = 0; i < NoOfTrucks; i++)
            {
                Trucks.Add(new Truck(i));
            }
            List<OrderLineByWeight> SortedOrderLineWeights = ourOrderLinesWeights.OrderBy(o => o.OrderWeight).ToList();

            foreach (var order in SortedOrderLineWeights)
            {
                foreach (Truck truck in Trucks)
                {
                    if (truck.TryAddOrder(order))
                    {
                        break;
                    }
                }
            }
            //what to do if the order couldn't be added to any single truck - it should be broken between multiple trucks 

            //EXAMPLE 
            /* Two trucks
            Five OrderLines:
            #1: 1300kg 
            #2: 800kg 
            #3: 800kg
            #4: 800kg
            } */

            Log.Info(String.Format("Trucks needed for this order: ", NoOfTrucks));

            foreach (var orderLine in request.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(string.Format("Unknown product gtin: {0}", orderLine.gtin));
                }
                else
                {
                    var product = products[orderLine.gtin];
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                    productIds.Add(product.Id);
                }
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add(string.Format("Product: {0}, no stock held", orderLine.gtin));
                    continue;
                }

                var item = stock[lineItem.ProductId];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        string.Format("Product: {0}, stock held: {1}, stock to remove: {2}", orderLine.gtin, item.held,
                            lineItem.Quantity));
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            _stockRepository.RemoveStock(request.WarehouseId, lineItems);
            return NoOfTrucks;
        }
    }
}
