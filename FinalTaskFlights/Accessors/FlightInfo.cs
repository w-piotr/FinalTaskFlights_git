namespace FinalTaskFlights
{
    /// <summary>
    /// Flight Info class decighned to store the information about the flight reservation.
    /// </summary>
    public class FlightInfo
    {
        public int ReservationId { get; set; }
        public string PassengerName { get; set; }
        public string FromAirport { get; set; }
        public string ToAirport { get; set; }
        public bool OneWayFlight { get; set; }
        public string StartDate { get; set; }
        public string EndDate{ get; set; }
        public string TripClass { get; set; }
        public int FlightCost { get; set; }
        public RentalInfo Rental { get; set; }
    }
}
