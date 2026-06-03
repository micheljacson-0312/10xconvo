using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;
using TenXConvo.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace TenXConvo.Benchmarks
{
    [MemoryDiagnoser]
    public class ConsultantAndUserServiceBenchmarks
    {
        private AppDbContext _db;
        private UserService _userService;
        private Guid _customerId;

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            _db = new AppDbContext(options);
            _db.Database.OpenConnection();
            _db.Database.EnsureCreated();

            _userService = new UserService(_db);

            _customerId = Guid.NewGuid();
            var customerUser = new AppUser { Id = _customerId, UserName = "customer", LoginId = "customer", Email = "customer@example.com", PasswordHash = "hash" };
            _db.Users.Add(customerUser);
            var customerProfile = new CustomerProfile { UserId = _customerId, User = customerUser };
            _db.CustomerProfiles.Add(customerProfile);

            for (int i = 0; i < 50; i++)
            {
                var consultantId = Guid.NewGuid();
                var consultantUser = new AppUser { Id = consultantId, UserName = $"consultant{i}", LoginId = $"consultant{i}", Email = $"c{i}@example.com", PasswordHash = "hash" };
                _db.Users.Add(consultantUser);
                var consultantProfile = new ConsultantProfile { UserId = consultantId, User = consultantUser, Slug = $"consultant-{i}" };
                _db.ConsultantProfiles.Add(consultantProfile);

                var conversation = new Conversation { CustomerId = customerProfile.Id, ConsultantId = consultantProfile.Id, IsActive = true, CreatedAt = DateTime.UtcNow };
                _db.Conversations.Add(conversation);

                for (int j = 0; j < 10; j++)
                {
                    _db.Messages.Add(new Message { ConversationId = conversation.Id, SenderId = consultantId, Body = $"msg {j}", IsRead = false, SentAt = DateTime.UtcNow.AddMinutes(-j) });
                }
            }

            _db.SaveChanges();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _db.Database.CloseConnection();
            _db.Dispose();
        }

        [Benchmark]
        public async Task GetConversations_Baseline()
        {
            await _userService.GetConversationsAsync(_customerId, 1, 50);
        }
    }
}
