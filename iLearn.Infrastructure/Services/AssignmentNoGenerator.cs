using iLearn.Application.Interfaces.Services;
using iLearn.Application.Interfaces.Services;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iLearn.Infrastructure.Services
{
    /// <summary>
    /// Uses a SQL Server sequence (AssignmentNoSeq) to generate a race-condition-free running number.
    /// Format: AS-yyyyMMdd-NNN
    /// </summary>
    public class AssignmentNoGenerator : IAssignmentNoGenerator
    {
        private readonly AppDbContext _context;

        public AssignmentNoGenerator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> NextAsync()
        {
            // Atomically get next value from the database sequence
            var connection = _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT NEXT VALUE FOR AssignmentNoSeq";
                var result = await cmd.ExecuteScalarAsync();
                int seqValue = Convert.ToInt32(result);

                string datePrefix = DateTime.Now.ToString("yyyyMMdd");
                return $"AS-{datePrefix}-{seqValue:D3}";
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
