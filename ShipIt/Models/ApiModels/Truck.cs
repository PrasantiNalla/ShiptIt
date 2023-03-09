using System;
using System.Collections.Generic;
using System.Data;
using ShipIt.Models.ApiModels;

namespace ShipIt.Models.ApiModels
{

    //CLASS-BASED APPROACH TO CALCULATING WHAT EACH TRUCK SHOULD CARRY 
    //Truck Class - shows us what's inside the truck: i.e. what productGTINS and how much weight of each GTIN 
    //A list or other object that holds our 'fleet' of trucks: all the trucks we need for this OutBoundOrderRequest


    public class Truck
    {
        //For each OrderRequest, 

        public int TruckId {get;set;}
        public float Capacity { get; set; } = 2000;
        public float Load { get; set; } = 0;
        public List<OrderLineByWeight> Orders { get; set; } = new List<OrderLineByWeight>();

        public Truck (int truckId) {
            TruckId = truckId;
        }
        public bool CanFit(OrderLineByWeight orderLineByWeight)
        {
            return Load + orderLineByWeight.OrderWeight <= Capacity;
        }
        public bool TryAddOrder( OrderLineByWeight ourOrderLineByWeight )
        {

            if (CanFit(ourOrderLineByWeight))
            {
               Orders.Add(ourOrderLineByWeight);
                Load += ourOrderLineByWeight.OrderWeight;
                return true;
            }
            return false;
        }
    }
}