using iLearn.Application.Interfaces.Services;
using System.Globalization;

namespace iLearn.Application.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.UtcNow.AddHours(7);
        public CultureInfo CultureInfo => new("th-TH");
        public DateTime UnixTime => new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
