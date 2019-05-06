using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalTaskFlights
{
    /// <summary>
    /// Rental Info class designed to store the information about the car rental reservation.
    /// </summary>
    public class RentalInfo
    {
        public int RentalLength { get; set; }
        public int PassengersNumber { get; set; }
        public string CarClass { get; set; }
        public int ChildSeats { get; set; }
        public int RentalCost { get; set; }
    }
}
