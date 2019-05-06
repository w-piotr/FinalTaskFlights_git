using System;using System.Collections.Generic;

namespace FinalTaskFlights
{
    /// <summary>
    /// Reservation Storage class designed to store the informaiton about all the reservation made during the dialog.
    /// </summary>
    public class ReservationStorage
    {
        public IList<FlightInfo> Reservations { get; set; }
    }
}
