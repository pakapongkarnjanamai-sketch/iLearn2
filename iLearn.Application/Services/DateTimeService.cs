using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.UtcNow.AddHours(7);
        public CultureInfo CultureInfo => new("th-TH");
        public DateTime UnixTime => new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
    public interface IDateTime
    {
        DateTime Now { get; }
        CultureInfo CultureInfo { get; }
        DateTime UnixTime { get; }
    }
}
