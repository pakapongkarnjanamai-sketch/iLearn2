using System.Globalization;

namespace iLearn.Application.Interfaces.Services
{
    public interface IDateTime
    {
        DateTime Now { get; }
        CultureInfo CultureInfo { get; }
        DateTime UnixTime { get; }
    }
}
