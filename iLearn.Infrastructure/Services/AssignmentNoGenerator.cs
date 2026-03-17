using iLearn.Application.Interfaces.Services;
using iLearn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace iLearn.Infrastructure.Services
{
    /// <summary>
    /// Uses a SQL Server sequence (AssignmentNoSeq) to generate a race-condition-free running number.
    /// Format: AS-yyyyMMdd-NNN
    /// </summary>
    public class AssignmentNoGenerator : IAssignmentNoGenerator
    {
        private readonly AppDbContext _context;
        private readonly IDateTime _dateTime;

        public AssignmentNoGenerator(AppDbContext context, IDateTime dateTime)
        {
            _context = context;
            _dateTime = dateTime;
        }

        public async Task<string> NextAsync()
        {
            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await _context.Database.OpenConnectionAsync();
            }

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT NEXT VALUE FOR AssignmentNoSeq";

                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction is not null)
                {
                    cmd.Transaction = currentTransaction.GetDbTransaction();
                }

                var result = await cmd.ExecuteScalarAsync();
                int seqValue = Convert.ToInt32(result);

                string datePrefix = _dateTime.Now.ToString("yyyyMMdd");
                return $"AS-{datePrefix}-{seqValue:D3}";
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }
    }
}
