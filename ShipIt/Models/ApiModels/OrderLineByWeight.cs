using System;
using System.Collections.Generic;
using System.Data;
using ShipIt.Models.ApiModels;

namespace ShipIt.Models.ApiModels
{


    //CLASS-BASED APPROACH TO CALCULATING WHAT EACH TRUCK SHOULD CARRY 
    //Truck Class - shows us what's inside the truck: i.e. what productGTINS and how much weight of each GTIN 
    //A list or other object that holds our 'fleet' of trucks: all the trucks we need for this OutBoundOrderRequest
    public class OrderLineByWeight
    {
        public string ProductGtin { get; set; }

        public float OrderWeight { get; set; }

        public OrderLineByWeight(string productGtin, float orderWeight)
        {
            ProductGtin = productGtin;
            OrderWeight = orderWeight;

        }
    }
}
