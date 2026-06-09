using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.VSDiagnostics;

namespace PartyClap.Benchmarks
{
    [CPUUsageDiagnoser]
    public class PartyPointsBenchmark
    {
        private List<Booking> bookings;
        [GlobalSetup]
        public void Setup()
        {
            bookings = new List<Booking>();
            var rnd = new Random(42);
            // Create a representative dataset with10k bookings
            for (int i = 0; i < 10000; i++)
            {
                var status = (i % 5 == 0) ? "Confirmed" : "Requested";
                bool balancePaid = (i % 7 == 0);
                decimal cost = 500 + (decimal)(rnd.NextDouble() * 5000);
                bookings.Add(new Booking { Id = Guid.NewGuid().ToString(), CustomerId = "c1", VendorId = "v1", ServiceId = "s1", BookingDate = DateTime.Now.AddDays(-rnd.Next(0, 365)), EventDate = DateTime.Now.AddDays(rnd.Next(1, 365)), VendorCost = cost, CustomerTotalCost = cost * 1.10m, AdvancePaid = cost * 0.20m, BalanceAmount = (cost * 1.10m) - (cost * 0.20m), Status = status, BalancePaidOnApp = balancePaid });
            }
        }

        [Benchmark]
        public int ComputePartyPoints()
        {
            int partyPoints = 0;
            decimal totalSpent = 0;
            if (bookings != null)
            {
                foreach (var b in bookings)
                {
                    if (b.Status == "Confirmed" || b.BalancePaidOnApp == true)
                    {
                        totalSpent += b.CustomerTotalCost;
                        partyPoints += 50; //50 points per booking
                    }
                }

                //1 point for every ₹100 spent
                partyPoints += (int)(totalSpent / 100);
            }

            // Determine Tier (not used for result but included to mirror logic)
            string tier = "Bronze";
            if (partyPoints >= 2000)
                tier = "Gold";
            else if (partyPoints >= 500)
                tier = "Silver";
            return partyPoints;
        }

        // Minimal Booking class copy used for benchmark isolation
        private class Booking
        {
            public string Id { get; set; }
            public string CustomerId { get; set; }
            public string VendorId { get; set; }
            public string ServiceId { get; set; }
            public DateTime BookingDate { get; set; }
            public DateTime EventDate { get; set; }
            public decimal VendorCost { get; set; }
            public decimal CustomerTotalCost { get; set; }
            public decimal AdvancePaid { get; set; }
            public decimal BalanceAmount { get; set; }
            public string Status { get; set; }
            public bool BalancePaidOnApp { get; set; }
        }
    }
}